using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using AasCore.Aas3.Package;
using AasJsonization = AasCore.Aas3_1.Jsonization;
using AasXmlization = AasCore.Aas3_1.Xmlization;

namespace SmartFactory.Services.DeviceService.Application.Services;

/// <summary>Reads AASX packages without extracting untrusted ZIP paths to disk.</summary>
public static class AasxPackageParser
{
    private const long MaxExpandedBytes = 250L * 1024 * 1024;
    private const long MaxModelBytes = 25L * 1024 * 1024;
    private const string Aas30Namespace = "https://admin-shell.io/aas/3/0";
    private const string Aas31Namespace = "https://admin-shell.io/aas/3/1";
    private const string Aas32Namespace = "https://admin-shell.io/aas/3/2";

    public static JsonObject Parse(byte[] packageBytes)
    {
        if (packageBytes.Length is <= 0 or > 50 * 1024 * 1024) throw new InvalidDataException("AASX package must be between 1 byte and 50 MB.");

        var entries = ValidateArchive(packageBytes);
        using var input = new MemoryStream(packageBytes, writable: false);
        using var packageResult = new Packaging().OpenRead(input);
        PackageRead package;
        try { package = packageResult.Must(); }
        catch (Exception error) when (error is not InvalidDataException)
        { throw new InvalidDataException("AASX package container is malformed or unsupported.", error); }

        var specs = package.Specs().ToArray();
        if (specs.Length == 0) throw new InvalidDataException("AASX package has no AAS model relationship.");

        var environments = new List<JsonObject>();
        foreach (var spec in specs)
        {
            var path = Uri.UnescapeDataString(spec.Uri.OriginalString.TrimStart('/'));
            if (!entries.TryGetValue(path, out var zipEntry)) throw new InvalidDataException("AASX package model part does not match a contained ZIP part.");
            if (zipEntry.Length > MaxModelBytes) throw new InvalidDataException("AAS core model exceeds the 25 MB model limit.");
            using var modelStream = spec.Stream();
            var bytes = ReadBounded(modelStream, MaxModelBytes);
            try { environments.Add(Deserialize(bytes)); }
            catch (Exception error) when (error is JsonException or XmlException or InvalidOperationException or ArgumentException)
            { throw new InvalidDataException("AASX package contains an invalid AAS JSON or XML core model.", error); }
        }

        return MergeEnvironments(environments);
    }

    private static Dictionary<string, ZipArchiveEntry> ValidateArchive(byte[] packageBytes)
    {
        using var memory = new MemoryStream(packageBytes, writable: false);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: false);
        if (archive.Entries.Count is < 1 or > 2_000) throw new InvalidDataException("AASX package has an invalid number of ZIP entries.");
        long expandedBytes = 0;
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            ValidatePartName(entry.FullName);
            expandedBytes = checked(expandedBytes + entry.Length);
            if (entry.Length > MaxExpandedBytes || expandedBytes > MaxExpandedBytes) throw new InvalidDataException("AASX package expands beyond the 250 MB safety limit.");
            if (!entry.FullName.EndsWith('/') && !entries.TryAdd(entry.FullName, entry)) throw new InvalidDataException("AASX package contains duplicate part names.");
        }
        return entries;
    }

    private static JsonObject Deserialize(byte[] bytes)
    {
        // Keep valid JSON model fields byte-for-byte at the tree level. The
        // generated v3.1 model SDK validates known fields, while raw JSON keeps
        // newer v3.2 administration fields and vendor extensions intact.
        var jsonOffset = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3 : 0;
        var firstIndex = jsonOffset;
        while (firstIndex < bytes.Length && char.IsWhiteSpace((char)bytes[firstIndex])) firstIndex++;
        var first = firstIndex < bytes.Length ? bytes[firstIndex] : (byte)0;
        if (first == (byte)'{')
        {
            var raw = JsonNode.Parse(bytes.AsSpan(jsonOffset)) as JsonObject ?? throw new InvalidDataException("AAS JSON environment must be an object.");
            var compatibleView = raw.DeepClone();
            RemoveV32AdministrationDates(compatibleView);
            _ = AasJsonization.Deserialize.EnvironmentFrom(compatibleView);
            return raw;
        }

        var hasUnicodeXmlBom = bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }) || bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }) ||
            bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }) || bytes.AsSpan().StartsWith(new byte[] { 0x00, 0x00, 0xFE, 0xFF });
        if (first != (byte)'<' && !hasUnicodeXmlBom) throw new InvalidDataException("AAS core model must be JSON or XML.");

        using var input = new MemoryStream(bytes, writable: false);
        using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxModelBytes });
        var source = XDocument.Load(reader, LoadOptions.None);
        var administration = ReadAdministrationDates(source);
        RemoveV32AdministrationDates(source);
        NormalizeXmlNamespaceForV31Sdk(source);

        using var normalized = new MemoryStream();
        source.Save(normalized, SaveOptions.DisableFormatting);
        normalized.Position = 0;
        using var normalizedReader = XmlReader.Create(normalized, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxModelBytes });
        normalizedReader.MoveToContent();
        var model = AasJsonization.Serialize.ToJsonObject(AasXmlization.Deserialize.EnvironmentFrom(normalizedReader));
        RestoreAdministrationDates(model, administration);
        return model;
    }

    private static Dictionary<string, Dictionary<string, string>> ReadAdministrationDates(XDocument source)
    {
        var fieldsById = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        var identifiableNames = new HashSet<string>(StringComparer.Ordinal) { "assetAdministrationShell", "submodel", "conceptDescription" };
        foreach (var identifiable in source.Descendants().Where(element => identifiableNames.Contains(element.Name.LocalName)))
        {
            var id = identifiable.Elements().FirstOrDefault(element => element.Name.LocalName == "id")?.Value;
            var admin = identifiable.Elements().FirstOrDefault(element => element.Name.LocalName == "administration");
            if (string.IsNullOrWhiteSpace(id) || admin is null) continue;
            var fields = admin.Elements()
                .Where(element => element.Name.LocalName is "createdAt" or "updatedAt")
                .ToDictionary(element => element.Name.LocalName, element => element.Value, StringComparer.Ordinal);
            if (fields.Count > 0) fieldsById[id] = fields;
        }
        return fieldsById;
    }

    // The official generated C# SDK linked by this project is AAS 3.1. Its
    // strict JSON/XML readers reject the two AdministrativeInformation fields
    // added in metamodel 3.2. Validate a temporary compatible view and retain
    // the original 3.2 values in the imported model.
    private static void RemoveV32AdministrationDates(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj["administration"] is JsonObject administration)
            {
                administration.Remove("createdAt");
                administration.Remove("updatedAt");
            }
            foreach (var child in obj.ToArray()) RemoveV32AdministrationDates(child.Value);
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array) RemoveV32AdministrationDates(child);
        }
    }

    private static void RemoveV32AdministrationDates(XDocument document)
    {
        foreach (var administration in document.Descendants().Where(element => element.Name.LocalName == "administration"))
            administration.Elements().Where(element => element.Name.LocalName is "createdAt" or "updatedAt").Remove();
    }

    private static void NormalizeXmlNamespaceForV31Sdk(XDocument document)
    {
        var namespacesToNormalize = new HashSet<string>(StringComparer.Ordinal) { Aas30Namespace, Aas32Namespace };
        foreach (var declaration in document.Descendants().Attributes().Where(attribute => attribute.IsNamespaceDeclaration && namespacesToNormalize.Contains(attribute.Value)))
            declaration.Value = Aas31Namespace;
        if (document.Root is { } root)
        {
            foreach (var element in root.DescendantsAndSelf().Where(element => namespacesToNormalize.Contains(element.Name.NamespaceName)))
                element.Name = XName.Get(element.Name.LocalName, Aas31Namespace);
        }
    }

    private static void RestoreAdministrationDates(JsonObject environment, IReadOnlyDictionary<string, Dictionary<string, string>> dateFields)
    {
        foreach (var name in new[] { "assetAdministrationShells", "submodels", "conceptDescriptions" })
        {
            if (environment[name] is not JsonArray elements) continue;
            foreach (var element in elements.OfType<JsonObject>())
            {
                var id = element["id"]?.GetValue<string>();
                if (id is null || !dateFields.TryGetValue(id, out var dates)) continue;
                if (element["administration"] is not JsonObject admin)
                {
                    admin = new JsonObject();
                    element["administration"] = admin;
                }
                foreach (var (field, value) in dates) admin[field] = value;
            }
        }
    }

    private static JsonObject MergeEnvironments(IReadOnlyList<JsonObject> environments)
    {
        var merged = (JsonObject)environments[0].DeepClone();
        foreach (var arrayName in new[] { "assetAdministrationShells", "submodels", "conceptDescriptions" })
        {
            var target = merged[arrayName] as JsonArray ?? new JsonArray();
            merged[arrayName] = target;
            foreach (var environment in environments.Skip(1))
                if (environment[arrayName] is JsonArray items)
                    foreach (var item in items) target.Add(item?.DeepClone());
        }
        return merged;
    }

    private static byte[] ReadBounded(Stream stream, long maxBytes)
    {
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > maxBytes) throw new InvalidDataException("AAS core model expands beyond the 25 MB model limit.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private static void ValidatePartName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.StartsWith('/') || name.Contains('\\') || name.Contains(':') || name.Any(char.IsControl))
            throw new InvalidDataException("AASX package contains an unsafe ZIP part name.");
        if (name.Split('/').Any(part => part is ".." or ".")) throw new InvalidDataException("AASX package contains a non-normalized ZIP part name.");
    }
}
