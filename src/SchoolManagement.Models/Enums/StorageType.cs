using System.Text.Json.Serialization;

namespace SchoolManagement.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StorageType
{
    HostingServer = 1,
    AWSS3         = 2,
    AzureBlob     = 3,
}
