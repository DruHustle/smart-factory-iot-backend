using System.Text.RegularExpressions;
using SmartFactory.Services.DeviceService.Application.DTOs;

namespace SmartFactory.Services.DeviceService.Application.Services;

public sealed record AasDocuments(Dictionary<string, object?> Shell, List<Dictionary<string, object?>> Submodels);

/// <summary>Builds the IDTA 02006, 02003, and 02018 asset model instances.</summary>
public static class AasDocumentsBuilder
{
    private static Dictionary<string, object?> External(string value) => new()
    {
        ["type"] = "ExternalReference",
        ["keys"] = new[] { new Dictionary<string, object?> { ["type"] = "GlobalReference", ["value"] = value } },
    };

    private static Dictionary<string, object?> Property(string idShort, string value, string valueType = "xs:string", string? semanticId = null, string? supplemental = null)
    {
        var item = new Dictionary<string, object?> { ["modelType"] = "Property", ["idShort"] = idShort, ["valueType"] = valueType, ["value"] = value };
        if (semanticId is not null) item["semanticId"] = External(semanticId);
        if (supplemental is not null) item["supplementalSemanticIds"] = new[] { External(supplemental) };
        return item;
    }

    private static Dictionary<string, object?> MultiLanguage(string idShort, string value, string semanticId, string? supplemental = null)
    {
        var item = new Dictionary<string, object?>
        {
            ["modelType"] = "MultiLanguageProperty", ["idShort"] = idShort,
            ["value"] = new[] { new Dictionary<string, object?> { ["language"] = "en", ["text"] = value } },
            ["semanticId"] = External(semanticId),
        };
        if (supplemental is not null) item["supplementalSemanticIds"] = new[] { External(supplemental) };
        return item;
    }

    private static Dictionary<string, object?> Collection(string idShort, object[] values, string? semanticId = null, object[]? supplemental = null)
    {
        var item = new Dictionary<string, object?> { ["modelType"] = "SubmodelElementCollection", ["idShort"] = idShort, ["value"] = values };
        if (semanticId is not null) item["semanticId"] = External(semanticId);
        if (supplemental is not null) item["supplementalSemanticIds"] = supplemental;
        return item;
    }

    private static Dictionary<string, object?> ElementList(string idShort, object[] values, string semanticId, string elementSemanticId, object[]? supplemental = null)
    {
        var item = new Dictionary<string, object?>
        {
            ["modelType"] = "SubmodelElementList", ["idShort"] = idShort, ["orderRelevant"] = false,
            ["typeValueListElement"] = "SubmodelElementCollection", ["semanticId"] = External(semanticId),
            ["semanticIdListElement"] = External(elementSemanticId), ["value"] = values,
        };
        if (supplemental is not null) item["supplementalSemanticIds"] = supplemental;
        return item;
    }

    private static Dictionary<string, object?> Submodel(string assetId, string idShort, string semantic, string template, string version, object[] elements) => new()
    {
        ["modelType"] = "Submodel", ["id"] = $"{assetId}/submodels/{idShort}", ["idShort"] = idShort,
        ["kind"] = "Instance", ["semanticId"] = External(semantic),
        ["administration"] = new Dictionary<string, object?> { ["version"] = version, ["revision"] = "0", ["templateId"] = template },
        ["submodelElements"] = elements,
    };

    public static AasDocuments Build(AssetProvisionRequest asset)
    {
        if (new[] { asset.AssetId, asset.Name, asset.Manufacturer, asset.Model, asset.ManufacturerStreet, asset.ManufacturerZipcode, asset.ManufacturerCityTown, asset.ManufacturerNationalCode, asset.ManufacturerArticleNumber, asset.OrderCodeOfManufacturer }.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("IDTA template instances require manufacturer identity and postal address fields.");
        if (!Regex.IsMatch(asset.ManufacturerNationalCode, "^[A-Za-z]{2}$"))
            throw new ArgumentException("Manufacturer country code must use two ISO 3166-1 alpha-2 letters.");

        var assetId = asset.AssetId.Trim();
        var cleanedName = Regex.Replace(asset.Name, "[^A-Za-z0-9]", "");
        var idShort = Regex.IsMatch(cleanedName, "^[A-Za-z]") ? cleanedName : $"Asset{cleanedName}";
        var shell = new Dictionary<string, object?>
        {
            ["modelType"] = "AssetAdministrationShell", ["id"] = assetId, ["idShort"] = idShort,
            ["administration"] = new Dictionary<string, object?> { ["version"] = asset.AasVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), ["revision"] = "0" },
            ["assetInformation"] = new Dictionary<string, object?>
            {
                ["assetKind"] = "Instance", ["globalAssetId"] = assetId,
                ["specificAssetIds"] = new[] { new Dictionary<string, object?> { ["name"] = "assetType", ["value"] = asset.AssetType } },
            },
            ["submodels"] = new[] { "Nameplate", "TechnicalData", "MaintenanceInstructions" }.Select(name => new Dictionary<string, object?>
            {
                ["type"] = "ModelReference", ["keys"] = new[] { new Dictionary<string, object?> { ["type"] = "Submodel", ["value"] = $"{assetId}/submodels/{name}" } },
            }).ToArray(),
        };

        var address = Collection("AddressInformation", new object[]
        {
            MultiLanguage("Street", asset.ManufacturerStreet, "0173-1#02-AAO128#002"),
            MultiLanguage("Zipcode", asset.ManufacturerZipcode, "0173-1#02-AAO129#002"),
            MultiLanguage("CityTown", asset.ManufacturerCityTown, "0173-1#02-AAO132#002"),
            MultiLanguage("NationalCode", asset.ManufacturerNationalCode.ToUpperInvariant(), "0173-1#02-AAO134#002"),
        }, "https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/AddressInformation", new object[]
        {
            External("https://admin-shell.io/smt-dropin/smt-dropin-use/1/0"), External("0112/2///61360_7#AAS002#001"),
            External("0173-1#02-AAQ837#008/0173-1#01-ADR448#008"),
        });
        var nameplateElements = new List<object>
        {
            Property("URIOfTheProduct", assetId, "xs:anyURI", "0112/2///61987#ABN590#002", "0173-1#02-ABH173#003"),
            MultiLanguage("ManufacturerName", asset.Manufacturer, "0112/2///61987#ABA565#009", "0173-1#02-AAO677#004"),
            MultiLanguage("ManufacturerProductDesignation", asset.Model, "0112/2///61987#ABA567#009", "0173-1#02-AAW338#003"), address,
            Property("OrderCodeOfManufacturer", asset.OrderCodeOfManufacturer, "xs:string", "0112/2///61987#ABA950#008", "0173-1#02-AAO227#004"),
            Property("ProductArticleNumberOfManufacturer", asset.ManufacturerArticleNumber, "xs:string", "0112/2///61987#ABA581#007", "0173-1#02-AAO676#005"),
        };
        if (!string.IsNullOrWhiteSpace(asset.SerialNumber)) nameplateElements.Add(Property("SerialNumber", asset.SerialNumber, "xs:string", "0112/2///61987#ABA951#009", "0173-1#02-AAM556#004"));
        var nameplate = Submodel(assetId, "Nameplate", "https://admin-shell.io/idta/nameplate/3/0/Nameplate", "https://admin-shell.io/idta-02006-3-0", asset.AasVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), nameplateElements.ToArray());

        var generalInformation = Collection("GeneralInformation", new object[]
        {
            Property("ManufacturerName", asset.Manufacturer, "xs:string", "0173-1#02-AAO677#004"),
            MultiLanguage("ManufacturerProductDesignation", asset.Model, "0173-1#02-AAW338#003", "https://api.eclass-cdp.com/0173-1-02-AAW338-003"),
            Property("ManufacturerArticleNumber", asset.ManufacturerArticleNumber, "xs:string", "0173-1#02-AAO676#005", "https://api.eclass-cdp.com/0173-1-02-AAO676-005"),
            Property("ManufacturerOrderCode", asset.OrderCodeOfManufacturer, "xs:string", "0173-1#02-AAO227#004", "https://api.eclass-cdp.com/0173-1-02-AAO227-004"),
            ElementList("ProductImages", Array.Empty<object>(), "0173-1#02-ABM220#001", "0173-1#02-ABM220#001/0173-1#01-AHY911#001"),
        }, "0173-1#02-ABK161#002/0173-1#01-AHX838#002", new object[] { External("https://api.eclass-cdp.com/0173-1-02-ABK161-002/0173-1-01-AHX838-002") });
        var ratedValues = new List<object>();
        if (!string.IsNullOrWhiteSpace(asset.RatedValue)) ratedValues.Add(Property("RatedValue", asset.RatedValue, "xs:string", "https://admin-shell.io/SMT/General/Arbitrary"));
        if (!string.IsNullOrWhiteSpace(asset.RatedUnit)) ratedValues.Add(Property("RatedUnit", asset.RatedUnit, "xs:string", "https://admin-shell.io/SMT/General/Arbitrary"));
        object[] technicalPropertyValues = ratedValues.Count == 0 ? Array.Empty<object>() : new object[]
        {
            Collection("TechnicalPropertyAreas__00__", new object[]
            {
                Collection("RatedCharacteristics", ratedValues.ToArray(), "https://admin-shell.io/SMT/General/Arbitrary"),
            }, "0173-1#02-ABL358#002/0173-1#01-AHX773#002"),
        };
        var technicalData = Submodel(assetId, "TechnicalData", "0173-1#01-AHX837#002", "https://admin-shell.io/idta-02003-2-0", asset.AasVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), new object[]
        {
            generalInformation,
            ElementList("ProductClassifications", Array.Empty<object>(), "0173-1#02-ABK162#002", "0173-1#02-ABK162#002/0173-1#01-AHX839#002"),
            ElementList("TechnicalPropertyAreas", technicalPropertyValues, "0173-1#02-ABK163#002", "0173-1#02-ABL358#002/0173-1#01-AHX773#002", new object[] { External("https://api.eclass-cdp.com/0173-1-02-ABK163-002") }),
            ElementList("SpecificDescriptions", Array.Empty<object>(), "0173-1#02-ABM221#001", "0173-1#02-ABM221#001/0173-1#01-AHY912#001"),
        });
        var maintenance = Submodel(assetId, "MaintenanceInstructions", "https://admin-shell.io/idta/SubmodelTemplate/MaintenanceInstructions/1/0", "https://admin-shell.io/idta-02018-1-0", asset.AasVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), new object[]
        {
            Property("MaintenanceFreeAsset", "false", "xs:boolean", "https://admin-shell.io/idta/maintenanceinstructions/maintenancefreeasset/1/0"),
        });
        return new AasDocuments(shell, new List<Dictionary<string, object?>> { nameplate, technicalData, maintenance });
    }
}
