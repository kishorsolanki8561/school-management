using SchoolManagement.Models.Enums;

namespace SchoolManagement.Services.Constants;

/// <summary>Describes a single dropdown source: which table, which columns, which filters are allowed.</summary>
internal sealed record DropdownDefinition(
    string   TableName,
    string   NameColumn,
    string   ValueColumn,
    string   BaseCondition,
    string[] AllowedExtraColumns,
    string[] AllowedFilterColumns);

/// <summary>
/// Central whitelist of every dropdown key → its SQL definition.
/// Column names that appear here (AllowedExtraColumns / AllowedFilterColumns) are the ONLY
/// values ever interpolated into SQL strings, preventing SQL-injection from client input.
/// </summary>
internal static class DropdownRegistry
{
    public static readonly IReadOnlyDictionary<DropdownKey, DropdownDefinition> Definitions =
        new Dictionary<DropdownKey, DropdownDefinition>
        {
            [DropdownKey.CountryDDL] = new(
                TableName:            "Countries",
                NameColumn:           "Name",
                ValueColumn:          "Id",
                BaseCondition:        "IsDeleted = 0 AND IsActive = 1",
                AllowedExtraColumns:  new[] { "Code" },
                AllowedFilterColumns: new[] { "IsActive" }),

            [DropdownKey.StateDDL] = new(
                TableName:            "States",
                NameColumn:           "Name",
                ValueColumn:          "Id",
                BaseCondition:        "IsDeleted = 0 AND IsActive = 1",
                AllowedExtraColumns:  new[] { "Code", "CountryId" },
                AllowedFilterColumns: new[] { "CountryId", "IsActive" }),

            [DropdownKey.CityDDL] = new(
                TableName:            "Cities",
                NameColumn:           "Name",
                ValueColumn:          "Id",
                BaseCondition:        "IsDeleted = 0 AND IsActive = 1",
                AllowedExtraColumns:  new[] { "StateId" },
                AllowedFilterColumns: new[] { "StateId", "IsActive" }),

            [DropdownKey.OrganizationDDL] = new(
                TableName:            "Organizations",
                NameColumn:           "Name",
                ValueColumn:          "Id",
                BaseCondition:        "IsDeleted = 0 AND IsActive = 1",
                AllowedExtraColumns:  new[] { "Address" },
                AllowedFilterColumns: new[] { "IsActive" }),

            [DropdownKey.RolesDDL] = new(
                TableName:            "Roles",
                NameColumn:           "Name",
                ValueColumn:          "Id",
                BaseCondition:        "IsDeleted = 0",
                AllowedExtraColumns:  new[] { "Description", "IsOrgRole" },
                AllowedFilterColumns: new[] { "IsOrgRole" }),

            [DropdownKey.ParentMenuDDL] = new(
                TableName:            "MenuMasters",
                NameColumn:           "Name",
                ValueColumn:          "Id",
                BaseCondition:        "IsDeleted = 0 AND IsActive = 1 AND HasChild = 1",
                AllowedExtraColumns:  new[] { "Position", "IconClass" },
                AllowedFilterColumns: new[] { "IsActive" }),

            [DropdownKey.MenuDDL] = new(
                TableName:            "MenuMasters",
                NameColumn:           "Name",
                ValueColumn:          "Id",
                BaseCondition:        "IsDeleted = 0 AND IsActive = 1",
                AllowedExtraColumns:  new[] { "ParentMenuId", "Position", "IconClass", "HasChild" },
                AllowedFilterColumns: new[] { "ParentMenuId", "IsActive", "HasChild" }),

            [DropdownKey.PageDDL] = new(
                TableName:            "PageMasters",
                NameColumn:           "Name",
                ValueColumn:          "Id",
                BaseCondition:        "IsDeleted = 0 AND IsActive = 1",
                AllowedExtraColumns:  new[] { "MenuId", "PageUrl", "IconClass" },
                AllowedFilterColumns: new[] { "MenuId", "IsActive" }),

            [DropdownKey.SchoolDDL] = new(
                TableName:            "Organizations",
                NameColumn:           "Name",
                ValueColumn:          "Id",
                BaseCondition:        "IsDeleted = 0 AND IsActive = 1 AND IsApproved = 1",
                AllowedExtraColumns:  new[] { "SchoolCode", "Address" },
                AllowedFilterColumns: new[] { "IsActive", "IsApproved" }),
        };
}
