# Testing

## Framework & Libraries

| Library | Version | Purpose |
|---|---|---|
| XUnit | Latest | Test runner |
| Moq | Latest | Mocking dependencies |
| FluentAssertions | Latest | Readable assertions |
| Microsoft.EntityFrameworkCore.InMemory | Latest | In-memory DB for EF Core (non-transaction tests) |
| Microsoft.EntityFrameworkCore.Sqlite | 6.0.0 | SQLite in-memory DB for transaction-aware tests |

**Test project:** `tests/SchoolManagement.Tests/SchoolManagement.Tests.csproj`

---

## Run Tests

```bash
# All tests
dotnet test tests/SchoolManagement.Tests

# With verbosity
dotnet test tests/SchoolManagement.Tests --verbosity normal

# Specific test class
dotnet test --filter "FullyQualifiedName~CountryServiceTests"

# With coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

---

## Test Coverage

| Area | Test File | Key Scenarios |
|---|---|---|
| CountryService | `Services/CountryServiceTests.cs` | Create, Update, Delete, GetById, GetAll, duplicate name/code validation |
| StateService | `Services/StateServiceTests.cs` | CRUD, country-state relationship, invalid countryId |
| CityService | `Services/CityServiceTests.cs` | CRUD, state-city relationship, invalid stateId |
| OrganizationService | `Services/OrganizationServiceTests.cs` | Create, Update, Delete, GetById, GetAll, duplicate name validation |
| AuthService | `Services/AuthServiceTests.cs` | Login, register, refresh token, logout, forgot/reset password, multi-role + org assignment |
| AuditLogService | `Services/AuditLogServiceTests.cs` | GetByEntity, GetByUser, GetByScreen, GetByTable, GetByEntityHierarchy (empty-batch cases) |
| DapperAuditExecutor | `Services/DapperAuditExecutorTests.cs` | Skip on null context / zero rows / unconfigured table; Created saves NewData; Updated saves only changed columns; no-op when nothing changed; bool → Yes/No; null columns excluded; request context stamped |
| QueryBuilder | `Services/QueryBuilderTests.cs` | Default sort, defaultSortDescending, valid column, sortDescending, invalid/injection fallback, unknown column, case-insensitive match, base SQL preserved, pagination clause appended |
| MenuMasterService | `Services/MenuMasterServiceTests.cs` | Create, Update, Delete, GetById, GetAll, cascade soft-delete (pages/modules/actions/permissions) |
| PageMasterService | `Services/PageMasterServiceTests.cs` | Hierarchical create (modules+actions+permissions), default all-action-types, multi-module, HasChild rules, duplicate skip, scalar update, new module upsert, cascade delete, GetAll paged |
| MenuAndPagePermission | `Services/MenuAndPagePermissionServiceTests.cs` | GetById, GetAll with filters, UpdateAsync flip IsAllowed, NotFound |
| OrgFileUploadConfigService | `Services/OrgFileUploadConfigServiceTests.cs` | Create, duplicate check, Update, UpdateNotFound, GetById (delegated), GetByScreen (delegated) |
| DropdownService | `Services/DropdownServiceTests.cs` | Unknown key throws, invalid extra column throws, invalid filter key throws, happy path name+value, extra columns camelCased, filter passed to repo, empty result not null, multiple extra cols, string-key filter (IsOrgRole), SQL contains correct table name |
| FileUploadService | `Services/FileUploadServiceTests.cs` | OwnerAdmin uses defaults, org config resolves, fallback to defaults, AllowMultiple=false blocks 2 files, invalid extension/size throw, valid upload returns response, folder resolution (OrgName/PageName / PageName / OrgName / AllAttachment) |
| EncryptionService | `Common/EncryptionServiceTests.cs` | AES-256-GCM encrypt/decrypt, RSA key operations |
| FilesValidator | `Common/FilesValidatorTests.cs` | File type, size, and extension validation |
| HashingUtility | `Common/HashingUtilityTests.cs` | Password hashing and verification |
| MultiQueryResultSet | `Common/MultipleQueryResultSetTests.cs` | Generic multi-result Dapper response parsing |
| ExceptionMiddleware | `Middleware/ExceptionHandlingMiddlewareTests.cs` | Unhandled exception → structured error response |

---

## Test Patterns

### 1. Arrange-Act-Assert (AAA)

Every test is structured in three clear sections:

```csharp
[Fact]
public async Task CreateAsync_ShouldReturnCountryResponse_WhenRequestIsValid()
{
    // Arrange
    var request = new CreateCountryRequest { Name = "India", Code = "IND" };

    // Act
    var result = await _countryService.CreateAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be("India");
    result.Code.Should().Be("IND");
}
```

---

### 2. In-Memory Database Isolation

Service tests that exercise EF Core use an in-memory database created per test class, ensuring complete isolation:

```csharp
public class CountryServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext _context;
    private readonly CountryService _service;

    public CountryServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())  // unique DB per test run
            .Options;

        _context = new SchoolManagementDbContext(options);
        _service = new CountryService(_context, _mapper);
    }

    public void Dispose() => _context.Dispose();
}
```

### 3. SQLite In-Memory for Transaction Tests

The EF Core `InMemory` provider does **not** support `IDbContextTransaction`. Services that use `BeginTransactionAsync` (e.g. `PageMasterService`, `MenuMasterService`) must use the `SqliteServiceTestBase`:

**File:** `tests/SchoolManagement.Tests/Infrastructure/SqliteServiceTestBase.cs`

```csharp
public abstract class SqliteServiceTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly SchoolManagementDbContext _context;

    protected SqliteServiceTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();   // keep open — SQLite in-memory DB lives as long as the connection

        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseSqlite(_connection).Options;

        _context = new SchoolManagementDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose() { _context.Dispose(); _connection.Close(); _connection.Dispose(); }
}
```

Test classes extend `SqliteServiceTestBase` instead of `IDisposable`:

```csharp
public sealed class PageMasterServiceTests : SqliteServiceTestBase
{
    public PageMasterServiceTests()
    {
        _sut = new PageMasterService(_context, _readRepoMock.Object, _mapper);
    }
}
```

---

### 3. Mocking with Moq

Read-heavy services that use `IReadRepository` (Dapper) are tested with mocked repositories:

```csharp
var mockRepo = new Mock<IReadRepository>();
mockRepo
    .Setup(r => r.QueryAsync<CountryResponse>(CountryQueries.GetAll, It.IsAny<object>()))
    .ReturnsAsync(new List<CountryResponse> { new() { Id = 1, Name = "India" } });

var service = new CountryService(mockRepo.Object, _mapper);
```

---

### 4. FluentAssertions

All assertions use FluentAssertions for readable failure messages:

```csharp
result.Should().NotBeNull();
result.Items.Should().HaveCount(3);
result.Should().BeEquivalentTo(expected);
await act.Should().ThrowAsync<NotFoundException>()
         .WithMessage("Country not found");
```

---

### 5. Soft Delete Verification

When testing soft delete, `IgnoreQueryFilters()` is used to confirm the record still exists in the DB:

```csharp
await _service.DeleteAsync(country.Id);

var deleted = await _context.Countries
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(c => c.Id == country.Id);

deleted.Should().NotBeNull();
deleted!.IsDeleted.Should().BeTrue();
```

---

### 6. Navigation Property Loading

Tests for `StateResponse` and `CityResponse` that rely on nested navigation properties include `.Include()` calls or seed related entities to simulate realistic EF Core behavior:

```csharp
// Seed country first so State.Country navigation resolves
_context.Countries.Add(new Country { Id = 1, Name = "India", Code = "IND" });
_context.States.Add(new State { Id = 1, Name = "Maharashtra", CountryId = 1 });
await _context.SaveChangesAsync();
```

---

## Test Scenarios by Service

### CountryService
- Create country — returns mapped `CountryResponse`
- Create country — throws when name already exists
- Create country — throws when code already exists
- Update country — updates fields correctly
- Update country — throws when ID not found
- Delete country — sets `IsDeleted = true`
- Delete country — throws when ID not found
- GetById — returns correct country
- GetById — throws when not found
- GetAll — returns paginated list
- GetAll — respects search filter

### StateService
- Create state — validates `CountryId` exists
- Create state — throws when country not found
- CRUD operations (same pattern as Country)
- GetByCountry — returns only states for given country
- GetByCountry — returns empty list when no states exist

### CityService
- Create city — validates `StateId` exists
- Create city — throws when state not found
- CRUD operations (same pattern as Country)
- GetByState — returns only cities for given state

### MenuMasterService
- Create — persists and returns response
- Create — same name under different parents is allowed (no global unique constraint)
- Update — updates fields correctly
- Update — throws when ID not found
- Delete — sets `IsDeleted = true` on menu
- Delete — cascade soft-deletes all child `PageMasters`, `PageMasterModules`, `PageMasterModuleActionMappings`, `MenuAndPagePermissions`
- Delete — throws when ID not found
- GetById — delegates to read repository

### PageMasterService
- Create — explicit actions persisted for each module
- Create — null/empty actions defaults to all 7 `ActionType` values
- Create — multiple modules all persisted
- Create — `HasChild = false` with no existing page: allowed
- Create — `HasChild = false` with existing page: throws `InvalidOperationException`
- Create — menu not found: throws `KeyNotFoundException`
- Create — duplicate action for same module: skipped (idempotent)
- Update — scalar field changes applied
- Update — new module name: inserted with actions + permissions
- Update — duplicate module name: skipped (idempotent)
- Update — ID not found: throws `KeyNotFoundException`
- Delete — cascade soft-deletes modules, action mappings, and permissions
- GetAll — returns paged result from read repository

### MenuAndPagePermissionService
- GetById — delegates to read repository
- GetAll — returns paged result
- GetAll — passes filter params (roleId, menuId, pageId) to repository
- Update — toggles `IsAllowed` false → true
- Update — toggles `IsAllowed` true → false
- Update — wrong `roleId` for existing record: throws `KeyNotFoundException`
- Update — record `id` not found: throws `KeyNotFoundException`

### OrgFileUploadConfigService
- Create — persists config and returns `OrgFileUploadConfigResponse`
- Create — throws `InvalidOperationException` when `(OrgId, PageId)` already exists
- Update — updates all config fields correctly
- Update — throws `KeyNotFoundException` when ID not found
- GetById — delegates to `IReadRepository`; returns `null` when not found
- GetByScreen — delegates to `IReadRepository` by `(OrgId, PageId)`; returns `null` when not found

### DropdownService
- `GetDropdownAsync` — unknown `DropdownKey` value → throws `ArgumentException` containing the key name
- `GetDropdownAsync` — `ExtraColumns` contains non-whitelisted column → throws `ArgumentException` containing the column name
- `GetDropdownAsync` — `Filters` contains non-whitelisted key → throws `ArgumentException` containing the key name
- `GetDropdownAsync` — valid `CountryDDL` with no extras → returns list with `name` and `value` keys
- `GetDropdownAsync` — extra column `"Code"` → response key is `"code"` (camelCased)
- `GetDropdownAsync` — `StateDDL` with `CountryId` filter → `QueryDynamicAsync` called with SQL containing `@CountryId`
- `GetDropdownAsync` — empty rows from repo → returns empty (non-null) enumerable
- `GetDropdownAsync` — multiple extra columns (`ParentMenuId`, `HasChild`) → all appear camelCased in response
- `GetDropdownAsync` — `RolesDDL` with `IsOrgRole` filter (whitelisted) → does not throw
- `GetDropdownAsync` — `PageDDL` → SQL sent to repo contains `[PageMasters]`
- `GetDropdownAsync` — `SchoolDDL` → SQL sent to repo contains `[Organizations]` and `IsApproved = 1`

### FileUploadService
- OwnerAdmin — always uses `appsettings.json` defaults even when org config exists
- Non-admin with org config — uses `OrgFileUploadConfig` for `(orgId, pageId)`
- Non-admin without org config — falls back to `appsettings.json` defaults
- `AllowMultiple = false` + 2 files — throws `InvalidOperationException`
- OwnerAdmin default `AllowMultiple = false` + 2 files — throws `InvalidOperationException`
- Invalid file extension — throws `ArgumentException` matching `*extension*`
- File too large — throws `ArgumentException` matching `*size*`
- Valid single file — returns `FileUploadResponse` with correct `FileName`, `SizeBytes`, `ContentType`, `FilePath`
- `AllowMultiple = true` — all files returned in response list
- Folder = `{OrgName}/{PageName}` when both orgId and pageId are provided
- Folder = `{PageName}` when orgId is null
- Folder = `{OrgName}` when pageId is null
- Folder = `AllAttachment` when both orgId and pageId are null

### QueryBuilder
- `AppendPaging` — null `sortBy` → uses `defaultColumn ASC`
- `AppendPaging` — null `sortBy` + `defaultSortDescending: true` → uses `defaultColumn DESC`
- `AppendPaging` — valid `sortBy` → uses that column with specified direction
- `AppendPaging` — valid `sortBy` + `sortDescending: true` → appends `DESC`
- `AppendPaging` — SQL-injection attempt in `sortBy` → falls back to default (injection string not present in output)
- `AppendPaging` — unknown column name → falls back to default column
- `AppendPaging` — `sortBy` matching allowed column case-insensitively → uses the whitelisted value
- `AppendPaging` — base SQL is preserved unchanged at the start of the result
- `AppendPaging` — always appends `OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY`

### AuditLogService
- GetByEntity — returns paged result for entity name + id
- GetByUser — returns paged result for user id
- GetByScreen — returns paged result for screen name
- GetByTable — returns paged result for table name
- GetByEntityHierarchyAsync — entityId only (entityName=null, screenName=null), no batches → returns empty paged result
- GetByEntityHierarchyAsync — entityId + entityName filter, no batches → returns empty paged result
- GetByEntityHierarchyAsync — entityId + screenName filter, no batches → returns empty paged result
- GetByEntityHierarchyAsync — all three filters provided, no batches → returns empty paged result
- GetByEntityHierarchyAsync — blank/whitespace entityName or screenName normalised to null before SQL params are built

### OrganizationService
- Create — returns mapped `OrganizationResponse`
- Create — throws when name already exists
- Update — updates Name, Address, IsActive
- Update — throws when ID not found
- Delete — sets `IsDeleted = true`
- Delete — throws when ID not found
- GetById — delegates to read repository
- GetAll — returns paged result from read repository

### AuthService
- Register — creates user with hashed password; `roleIds` (list) creates one `UserRoleMapping` per role; `orgIds` (list) creates one `UserOrganizationMapping` per org
- Register — throws on duplicate username/email
- Login — returns access + refresh token on valid credentials; `role` field in response is the first assigned role name (empty string if none); `OrgId`/`OrgName` populated for non-OwnerAdmin users
- Login — throws on wrong password
- Refresh — rotates tokens on valid refresh token
- Refresh — throws on expired/revoked token
- Logout — revokes refresh token
- ForgotPassword — sends reset email
- ResetPassword — changes password and marks token used
- ResetPassword — throws on expired token
- SwitchSchool — returns new token pair with updated OrgId claim
- SwitchSchool — throws Unauthorized when user is not a member of target org

### MenuAndPagePermissionService (Org-scoped)
- UpdateOrgPermissionAsync — toggles IsAllowed on org-specific permission row
- UpdateOrgPermissionAsync — throws when permission not found for given orgId
- GetOrgPermissionsAsync — returns only permissions for the specified orgId
- CopySystemRolesAndPermissions (via SchoolService.ApproveAsync) — all 29 system roles copied with OrgId; idempotent (skips already-copied roles)
- Login after approval — menus/pages resolved from org-specific permissions, not system-level

### SchoolService
- RegisterAsync — creates Organization + SchoolApprovalRequest (Pending)
- RegisterAsync — throws on duplicate school name
- ApproveAsync — sets IsApproved=true, stamps ApprovedAt/ApprovedBy, updates approval request to Approved
- ApproveAsync — throws when school is already approved
- RejectAsync — sets approval request to Rejected with reason
- RejectAsync — throws when no pending request exists
- GetPendingApprovalsAsync — returns only Pending requests paginated
- GetApprovalHistoryAsync — returns all requests for a specific school

### UserManagementService
- CreateAsync — creates user with hashed password, assigns roles scoped to caller's OrgId
- CreateAsync — throws on duplicate username/email
- CreateAsync — throws when a supplied roleId does not exist
- UpdateAsync — updates Username, Email, IsActive
- UpdateAsync — throws when user not found
- DeleteAsync — sets IsDeleted=true
- AssignRoleAsync — adds UserRoleMapping scoped to caller's OrgId
- AssignRoleAsync — throws when role already assigned in org
- RemoveRoleAsync — soft-deletes the UserRoleMapping
- RemoveRoleAsync — throws when assignment not found
- ChangeRoleLevelAsync — swaps system-level role between SuperAdmin and Admin (OrgId=null)
- ChangeRoleLevelAsync — throws when target role is not SuperAdmin or Admin
- ChangeRoleLevelAsync — throws when target user is OwnerAdmin

---

## Test Assembly Config

**File:** `tests/SchoolManagement.Tests/TestAssemblyConfig.cs`

Handles test assembly initialization — sets up any shared infrastructure needed across all test classes (e.g., AutoMapper configuration, test-scoped logging).
