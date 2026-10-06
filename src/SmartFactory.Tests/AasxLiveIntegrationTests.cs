using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AasCore.Aas3.Package;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SmartFactory.Services.DeviceService.API.Controllers;
using SmartFactory.Services.DeviceService.Application.Services;

namespace SmartFactory.Tests;

public sealed class AasxLiveIntegrationTests
{
    [Fact]
    public async Task ImportsAndRetrievesAasxAndEmbeddedFileFromLiveBaSyxWhenEnabled()
    {
        if (!bool.TryParse(Environment.GetEnvironmentVariable("AAS_LIVE_TESTS"), out var enabled) || !enabled) return;

        var repository = Setting("AAS_REPOSITORY_URL", "http://127.0.0.1:8081");
        var submodelRepository = Setting("AAS_SUBMODEL_REPOSITORY_URL", "http://127.0.0.1:8083");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AAS_REPOSITORY_URL"] = repository,
            ["AAS_SUBMODEL_REPOSITORY_URL"] = submodelRepository,
            ["AAS_CONCEPT_DESCRIPTION_REPOSITORY_URL"] = Setting("AAS_CONCEPT_DESCRIPTION_REPOSITORY_URL", "http://127.0.0.1:8085"),
            ["AAS_REGISTRY_URL"] = Setting("AAS_REGISTRY_URL", "http://127.0.0.1:8082"),
            ["AAS_SUBMODEL_REGISTRY_URL"] = Setting("AAS_SUBMODEL_REGISTRY_URL", "http://127.0.0.1:8084"),
            ["AASX_FILE_SERVER_URL"] = Setting("AASX_FILE_SERVER_URL", "http://127.0.0.1:8086"),
            ["AAS_ALLOW_UNAUTHENTICATED_LOCAL"] = "true",
            ["AAS_REPOSITORY_REGISTRY_INTEGRATION"] = "true",
        }).Build();
        var environment = new TestEnvironment { EnvironmentName = Environments.Development };
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var service = new AasProvisioningService(http, configuration, environment, NullLogger<AasProvisioningService>.Instance);
        var assetId = $"urn:smart-factory:aasx-live-test:{Guid.NewGuid():N}";
        var submodelId = assetId + "/submodels/TechnicalData";
        var package = CreatePackage(assetId, submodelId);
        Dictionary<string, object?>? imported = null;

        try
        {
            imported = await service.ImportAasxAsync(package, "live-v32-package.aasx", CancellationToken.None);
            var packageId = Assert.IsType<string>(imported["packageId"]);
            using var download = await http.GetAsync(Join(configuration["AASX_FILE_SERVER_URL"]!, $"packages/{packageId}"));
            Assert.Equal(System.Net.HttpStatusCode.OK, download.StatusCode);
            var contentType = download.Content.Headers.ContentType?.MediaType;
            var savedPackage = await download.Content.ReadAsByteArrayAsync();
            Assert.True(savedPackage.AsSpan().StartsWith("PK"u8), $"The AASX File Server should return the uploaded OPC package (media type: {contentType}, first bytes: {Convert.ToHexString(savedPackage.AsSpan(0, Math.Min(16, savedPackage.Length)))}).");
            Assert.Contains(contentType, new[] { "application/asset-administration-shell-package", "application/aasx+json" });
            using var archive = new ZipArchive(new MemoryStream(savedPackage), ZipArchiveMode.Read);
            var attachment = archive.Entries.Single(entry => entry.FullName == "aasx/files/datasheet.bin");
            using var attachmentStream = attachment.Open();
            using var attachmentBytes = new MemoryStream();
            await attachmentStream.CopyToAsync(attachmentBytes);
            Assert.Equal("live attachment fixture", Encoding.UTF8.GetString(attachmentBytes.ToArray()));

            var shellId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(assetId));
            using var shellResponse = await http.GetAsync(Join(repository, $"shells/{shellId}"));
            Assert.Equal(System.Net.HttpStatusCode.OK, shellResponse.StatusCode);
            using var shellJson = JsonDocument.Parse(await shellResponse.Content.ReadAsStreamAsync());
            Assert.Equal("2026-01-02T03:04:05Z", shellJson.RootElement.GetProperty("administration").GetProperty("createdAt").GetString());

            var encodedSubmodel = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(submodelId));
            using var submodelResponse = await http.GetAsync(Join(submodelRepository, $"submodels/{encodedSubmodel}"));
            Assert.Equal(System.Net.HttpStatusCode.OK, submodelResponse.StatusCode);
            using var submodelJson = JsonDocument.Parse(await submodelResponse.Content.ReadAsStreamAsync());
            Assert.Equal("urn:semantic:live-rated-power", submodelJson.RootElement.GetProperty("submodelElements")[0].GetProperty("semanticId").GetProperty("keys")[0].GetProperty("value").GetString());
        }
        finally
        {
            if (imported is not null)
            {
                var shellIds = ((System.Text.Json.Nodes.JsonArray)imported["assetAdministrationShells"]!).Select(item => item!["id"]!.GetValue<string>()).ToArray();
                var submodelIds = ((System.Text.Json.Nodes.JsonArray)imported["submodels"]!).Select(item => item!["id"]!.GetValue<string>()).ToArray();
                var conceptIds = ((System.Text.Json.Nodes.JsonArray)imported["conceptDescriptions"]!).Select(item => item!["id"]!.GetValue<string>()).ToArray();
                await service.RollbackAasxAsync(new AasxImportReceipt((string)imported["packageId"]!, shellIds, submodelIds, conceptIds), CancellationToken.None);
            }
        }
    }

    private static string Setting(string name, string fallback) => Environment.GetEnvironmentVariable(name) ?? fallback;
    private static string Join(string root, string path) => root.TrimEnd('/') + "/" + path;

    private static byte[] CreatePackage(string assetId, string submodelId)
    {
        var shell = JsonSerializer.Serialize(new
        {
            modelType = "AssetAdministrationShell", id = assetId, idShort = "LiveVendorAsset",
            administration = new { createdAt = "2026-01-02T03:04:05Z", updatedAt = "2026-02-03T04:05:06Z" },
            assetInformation = new { assetKind = "Instance", globalAssetId = assetId },
            submodels = new[] { new { type = "ModelReference", keys = new[] { new { type = "Submodel", value = submodelId } } } },
        });
        var submodel = JsonSerializer.Serialize(new
        {
            modelType = "Submodel", id = submodelId, idShort = "TechnicalData", kind = "Instance",
            administration = new { version = "1", revision = "0", createdAt = "2026-01-02T03:04:05Z" },
            submodelElements = new[] { new
            {
                modelType = "Property", idShort = "RatedPower", valueType = "xs:decimal", value = "5.5",
                semanticId = new { type = "ExternalReference", keys = new[] { new { type = "GlobalReference", value = "urn:semantic:live-rated-power" } } },
            } },
        });
        var concepts = "{\"conceptDescriptions\":[{\"modelType\":\"ConceptDescription\",\"id\":\"urn:semantic:live-rated-power\",\"idShort\":\"RatedPower\"}],\"assetAdministrationShells\":[],\"submodels\":[]}";
        var shellEnvironment = JsonSerializer.Serialize(new { assetAdministrationShells = new[] { JsonDocument.Parse(shell).RootElement }, submodels = Array.Empty<object>(), conceptDescriptions = Array.Empty<object>() });
        var submodelEnvironment = JsonSerializer.Serialize(new { assetAdministrationShells = Array.Empty<object>(), submodels = new[] { JsonDocument.Parse(submodel).RootElement }, conceptDescriptions = Array.Empty<object>() });
        using var stream = new MemoryStream();
        using (var package = new Packaging().Create(stream))
        {
            var shellPart = package.PutPart(new Uri("/aasx/shell.json", UriKind.Relative), "application/json", Encoding.UTF8.GetBytes(shellEnvironment));
            package.MakeSpec(shellPart);
            var submodelPart = package.PutPart(new Uri("/aasx/submodel.json", UriKind.Relative), "application/json", Encoding.UTF8.GetBytes(submodelEnvironment));
            package.MakeSpec(submodelPart);
            var conceptPart = package.PutPart(new Uri("/aasx/concepts.json", UriKind.Relative), "application/json", Encoding.UTF8.GetBytes(concepts));
            package.MakeSpec(conceptPart);
            package.PutPart(new Uri("/aasx/files/datasheet.bin", UriKind.Relative), "application/octet-stream", Encoding.UTF8.GetBytes("live attachment fixture"));
            package.Flush();
        }
        return stream.ToArray();
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Live AASX Integration Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
