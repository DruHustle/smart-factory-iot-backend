using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using AasCore.Aas3.Package;
using SmartFactory.Services.DeviceService.Application.Services;

namespace SmartFactory.Tests;

public sealed class AasxPackageParserTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesAasCoreJsonAndXmlModels(bool xml)
    {
        var model = xml
            ? """<environment xmlns="https://admin-shell.io/aas/3/1"><assetAdministrationShells><assetAdministrationShell><id>urn:test:aas</id><assetInformation><assetKind>Instance</assetKind><globalAssetId>urn:test:asset</globalAssetId></assetInformation></assetAdministrationShell></assetAdministrationShells><submodels/><conceptDescriptions/></environment>"""
            : """{"assetAdministrationShells":[{"modelType":"AssetAdministrationShell","id":"urn:test:aas","assetInformation":{"assetKind":"Instance","globalAssetId":"urn:test:asset"}}],"submodels":[],"conceptDescriptions":[]}""";

        var parsed = AasxPackageParser.Parse(CreatePackage(model, xml ? "data.xml" : "data.json"));

        var shells = Assert.IsType<JsonArray>(parsed["assetAdministrationShells"]);
        Assert.Equal("urn:test:aas", shells[0]?["id"]?.GetValue<string>());
    }

    [Fact]
    public void RejectsPackagePathsThatCouldEscapeTheContainer()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            archive.CreateEntry("../outside.txt");
        Assert.Throws<InvalidDataException>(() => AasxPackageParser.Parse(stream.ToArray()));
    }

    [Fact]
    public void RejectsPackagesWithoutTheRequiredOriginRelationship()
    {
        var package = CreatePackage("{}", "data.json", includeOrigin: false);
        Assert.Throws<InvalidDataException>(() => AasxPackageParser.Parse(package));
    }

    [Fact]
    public void PreservesV32AdministrativeDatesNestedSemanticIdsAndMultipleSpecParts()
    {
        const string shell = """{"assetAdministrationShells":[{"modelType":"AssetAdministrationShell","id":"urn:test:aas","administration":{"createdAt":"2026-01-02T03:04:05Z","updatedAt":"2026-02-03T04:05:06Z"},"assetInformation":{"assetKind":"Instance","globalAssetId":"urn:test:asset"}}],"submodels":[],"conceptDescriptions":[]}""";
        const string submodel = """{"assetAdministrationShells":[],"submodels":[{"modelType":"Submodel","id":"urn:test:aas/submodels/technical","idShort":"TechnicalData","submodelElements":[{"modelType":"SubmodelElementCollection","idShort":"Motor","semanticId":{"type":"ExternalReference","keys":[{"type":"GlobalReference","value":"urn:semantic:motor"}]},"value":[{"modelType":"Property","idShort":"RatedPower","valueType":"xs:decimal","value":"5.5","semanticId":{"type":"ExternalReference","keys":[{"type":"GlobalReference","value":"urn:semantic:rated-power"}]}}]}]}],"conceptDescriptions":[]}""";

        var parsed = AasxPackageParser.Parse(CreatePackage(("shell.json", shell), ("submodel.json", submodel)));

        Assert.Equal("2026-01-02T03:04:05Z", parsed["assetAdministrationShells"]?[0]?["administration"]?["createdAt"]?.GetValue<string>());
        Assert.Equal("urn:semantic:rated-power", parsed["submodels"]?[0]?["submodelElements"]?[0]?["value"]?[0]?["semanticId"]?["keys"]?[0]?["value"]?.GetValue<string>());
    }

    [Fact]
    public void ParsesV32XmlAndPreservesAdministrativeDates()
    {
        const string model = """<environment xmlns="https://admin-shell.io/aas/3/2"><assetAdministrationShells><assetAdministrationShell><id>urn:test:aas:xml32</id><administration><createdAt>2026-01-02T03:04:05Z</createdAt><updatedAt>2026-02-03T04:05:06Z</updatedAt></administration><assetInformation><assetKind>Instance</assetKind><globalAssetId>urn:test:asset:xml32</globalAssetId></assetInformation></assetAdministrationShell></assetAdministrationShells><submodels/><conceptDescriptions/></environment>""";
        var parsed = AasxPackageParser.Parse(CreatePackage(model, "environment.xml"));
        Assert.Equal("2026-01-02T03:04:05Z", parsed["assetAdministrationShells"]?[0]?["administration"]?["createdAt"]?.GetValue<string>());
        Assert.Equal("2026-02-03T04:05:06Z", parsed["assetAdministrationShells"]?[0]?["administration"]?["updatedAt"]?.GetValue<string>());
    }

    [Fact]
    public void ParsesAnExternalVendorCorpusWhenConfigured()
    {
        var corpus = Environment.GetEnvironmentVariable("AASX_TEST_CORPUS_DIR");
        if (string.IsNullOrWhiteSpace(corpus)) return;
        Assert.True(Directory.Exists(corpus), "AASX_TEST_CORPUS_DIR must reference a readable vendor-package directory.");
        var files = Directory.GetFiles(corpus, "*.aasx", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var result = AasxPackageParser.Parse(File.ReadAllBytes(file));
            var shells = Assert.IsType<JsonArray>(result["assetAdministrationShells"]);
            Assert.InRange(shells.Count, 1, 25);

            var submodels = Assert.IsType<JsonArray>(result["submodels"]);
            Assert.All(submodels, submodel => Assert.False(string.IsNullOrWhiteSpace(submodel?["id"]?.GetValue<string>())));

            if (Path.GetFileName(file).Equals("IDTA-01005_Example.aasx", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal(2, shells.Count);
                Assert.Equal(2, submodels.Count);
                Assert.Contains(submodels, submodel => submodel?.ToJsonString().Contains("Additional_file_for_submodel.pdf", StringComparison.Ordinal) == true);
            }
        }
    }

    private static byte[] CreatePackage(string model, string modelPart, bool includeOrigin = true)
    {
        if (includeOrigin) return CreatePackage((modelPart, model));

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("data.json");
        }
        return stream.ToArray();
    }

    private static byte[] CreatePackage(params (string Name, string Content)[] models)
    {
        using var stream = new MemoryStream();
        using (var package = new Packaging().Create(stream))
        {
            foreach (var (name, content) in models)
            {
                var part = package.PutPart(new Uri($"/aasx/{name}", UriKind.Relative), "application/json", Encoding.UTF8.GetBytes(content));
                package.MakeSpec(part);
            }
            package.Flush();
        }
        return stream.ToArray();
    }
}
