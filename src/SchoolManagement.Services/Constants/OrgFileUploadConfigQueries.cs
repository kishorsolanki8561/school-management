namespace SchoolManagement.Services.Constants;

internal static class OrgFileUploadConfigQueries
{
    public const string GetById = @"
        SELECT Id, OrgId, PageId, AllowedExtensions, AllowedMimeTypes,
               MaxFileSizeBytes, AllowMultiple, IsActive, CreatedAt
        FROM   OrgFileUploadConfigs
        WHERE  Id = @Id AND IsDeleted = 0";

    public const string GetByScreen = @"
        SELECT Id, OrgId, PageId, AllowedExtensions, AllowedMimeTypes,
               MaxFileSizeBytes, AllowMultiple, IsActive, CreatedAt
        FROM   OrgFileUploadConfigs
        WHERE  OrgId = @OrgId AND PageId = @PageId
          AND  IsDeleted = 0 AND IsActive = 1";
}
