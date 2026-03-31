using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs;

public sealed record SaveOrgStorageConfigRequest(
    StorageType StorageType,

    // HostingServer
    string?     BasePath,

    // AWS S3
    string?     BucketName,
    string?     Region,
    string?     AccessKey,
    string?     SecretKey,

    // Azure Blob
    string?     ContainerName,
    string?     ConnectionString
);

public sealed record OrgStorageConfigResponse(
    int         Id,
    int         OrgId,
    StorageType StorageType,
    bool        IsActive,

    string?     BasePath,

    string?     BucketName,
    string?     Region,
    string?     AccessKey,

    string?     ContainerName
    // SecretKey and ConnectionString intentionally omitted from response
);
