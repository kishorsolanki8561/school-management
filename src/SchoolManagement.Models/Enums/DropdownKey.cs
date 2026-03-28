using System.Text.Json.Serialization;

namespace SchoolManagement.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DropdownKey
{
    CountryDDL      = 1,
    StateDDL        = 2,
    CityDDL         = 3,
    OrganizationDDL = 4,
    RolesDDL        = 5,
    ParentMenuDDL   = 6,
    MenuDDL         = 7,
    PageDDL         = 8,
}
