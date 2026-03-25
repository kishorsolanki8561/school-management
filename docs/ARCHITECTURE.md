# Architecture

## Pattern: Modular Monolithic

The system is a **modular monolith** — a single deployable unit divided into well-defined modules with strict dependency rules. Each module has its own responsibility and communicates only through defined interfaces, making it easy to extract into microservices later if needed.

```
┌─────────────────────────────────────────────┐
│              SchoolManagement.API            │  ← Entry point, controllers, middleware
├─────────────────────────────────────────────┤
│           SchoolManagement.Services          │  ← Business logic (interfaces + impls)
├─────────────────────────────────────────────┤
│       SchoolManagement.DbInfrastructure      │  ← EF Core, Dapper, repositories
├──────────────────────┬──────────────────────┤
│  SchoolManagement    │  SchoolManagement     │
│       .Models        │      .Common          │  ← Shared by all layers
└──────────────────────┴──────────────────────┘
```

Dependency direction: API → Services → DbInfrastructure → Models
`Common` and `Models` are referenced by all layers but reference nothing above them.

---

## Projects

### SchoolManagement.API
- **Controllers** — thin HTTP adapters; delegate all logic to services
- **Middleware** — `RequestContextMiddleware`, `ExceptionHandlingMiddleware`, `EncryptionMiddleware`
- **Extensions** — `ServiceCollectionExtensions` (JWT, Swagger, versioning)
- **Program.cs** — DI composition root; wires all modules via extension methods

### SchoolManagement.Services
- **Interfaces/** — service contracts (`IAuthService`, `ICountryService`, `IStateService`, `ICityService`, `IAuditLogService`, `IOrganizationService`, `IMenuMasterService`, `IPageMasterService`, `IMenuAndPagePermissionService`)
- **Implementations/** — business logic classes
- **Constants/** — raw Dapper SQL queries per domain (`CountryQueries`, `StateQueries`, `CityQueries`, `AuditLogQueries`, `AuthQueries`, `OrganizationQueries`, `MenuMasterQueries`, `PageMasterQueries`, `MenuAndPagePermissionQueries`)
- **Extensions/ServicesExtensions.cs** — registers all services as `Scoped`

### SchoolManagement.DbInfrastructure
- **Context/SchoolManagementDbContext** — EF Core context; auto-stamps audit fields in `SaveChangesAsync`
- **Repositories/** — generic `IWriteRepository<T>` (EF Core), `IReadRepository` (Dapper), `IDapperAuditExecutor` (Dapper writes with audit)
- **Configurations/** — EF Core `IEntityTypeConfiguration<T>` per entity
- **Audit/** — static audit sub-system:
  - `AuditConfiguration` — registry of audited tables + columns; if a table is absent, it is silently skipped
  - `AuditTableConfig` / `AuditColumnConfig` / `AuditLookup` — per-table and per-column config (display name, FK lookup, bool display override)
  - `AuditValueHelper` — shared helpers used by both the EF interceptor and the Dapper executor: `GetEffectiveColumns`, `FormatValue` (bool → "Yes"/"No"), `ShouldSkip` (null / IsDeleted=false), `ResolveLookupAsync` (FK id → display value), `BuildFromEntityAsync`
  - `DapperAuditContext` — carries table name, entity id, action, and old/new entity snapshots for Dapper writes
- **Interceptors/AuditInterceptor** — EF Core `SaveChangesInterceptor`; column-wise old/new capture using `AuditConfiguration`; FK values resolved to display names; booleans formatted as "Yes"/"No"; null and IsDeleted=false values skipped. Parent-child audit linking works generically via EF FK metadata (`FindParentEntity`) — no entity types are hardcoded. `BatchId` is scoped to the active DB transaction (all `SaveChangesAsync` calls inside one transaction share the same batch). `ParentAuditLogId` correctly stores the **parent `AuditLog` row's Id** (not the entity PK) via a two-pass save: parent audit rows are saved first so their Ids are known, then child rows are saved with `ParentAuditLogId` set. A `_savedAuditIds` dictionary enables cross-`SaveChangesAsync` parent linking within the same request (e.g. page saved in one call, modules saved in a subsequent call inside the same transaction)
- **Extensions/DbInfrastructureExtensions.cs** — registers DbContext, repositories, and `IDapperAuditExecutor`

### SchoolManagement.Models
- **Entities/** — EF Core entity classes; all business entities extend `BaseEntity`
- **DTOs/** — request and response records per module (`Auth/`, `Master/`)
- **Mappings/AutoMapperProfile.cs** — single profile with all entity→DTO maps
- **Common/** — shared types (`ApiResponse<T>`, `PagedResult<T>`, `PaginationRequest`)
- **Enums/** — `UserRole` (29 values, Ids 1–29 — used only by `RoleSeeder` for fixed seeded IDs)

### SchoolManagement.Common
- **Services/** — `IEncryptionService` (AES-256-GCM + RSA-2048), `IEmailService` (SMTP), `IRequestContext`
- **Middleware/** — request pipeline components
- **Helpers/** — `FilesValidator`, `FilePathHelper`
- **Utilities/** — `IpLocationResolver`
- **Extensions/CommonExtensions.cs** — registers all common services

### SchoolManagement.Seeding
- **ISeeder** — contract for idempotent seed classes
- **RoleSeeder** — upserts 29 roles (with `IsOrgRole` flag) on every startup: inserts missing roles, updates changed rows only — no-op when nothing has changed
- **UserSeeder** — seeds default admin user (`superadmin`) + assigns Super Admin role via `UserRoleMapping`
- **CountrySeeder** — seeds country reference data
- **DatabaseSeeder** — orchestrates all seeders in order; called from `app.SeedDatabaseAsync()` at startup

---

## Dependency Injection Composition

All DI is done in `Program.cs` via extension methods — one call per module:

```csharp
builder.Services.AddCommonServices();         // encryption, email, request context
builder.Services.AddDbInfrastructure(config); // DbContext, repositories, error logging
builder.Services.AddApplicationServices();    // business services
builder.Services.AddSeeding();                // seed orchestrator + seeders
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddJwtAuthentication();
builder.Services.AddApiVersioningConfig();
```

All business and infrastructure services are registered as **Scoped** (per HTTP request).

---

## Request Lifecycle

```
HTTP Request
     │
     ▼
RequestContextMiddleware       ← captures IpAddress, UserId, X-Screen-Name header → populates IRequestContext
     │
     ▼
EncryptionMiddleware           ← decrypts body if E2E encryption enabled
     │
     ▼
JWT Authentication             ← validates Bearer token
     │
     ▼
Controller Action              ← validates input, calls service interface
     │
     ▼
Service Implementation         ← business logic; uses IMapper for entity→DTO
     │
     ├──► IWriteRepository<T>  ← EF Core writes (auto-stamps audit via AuditInterceptor)
     │
     └──► IReadRepository      ← Dapper reads (raw SQL from *Queries constants)
          │
          ▼
     SQL Server
          │
     ◄────┘
     │
     ▼
ApiResponse<T>                 ← uniform response wrapper
     │
     ▼
ExceptionHandlingMiddleware    ← catches unhandled exceptions → ErrorLog + structured error response
     │
     ▼
HTTP Response
```

---

## BaseEntity

Every business entity extends `BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public int Id            { get; set; }
    public DateTime CreatedAt   { get; init; }
    public string CreatedBy     { get; init; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy   { get; set; }
    public bool IsDeleted       { get; set; }   // soft delete
    public string? IpAddress    { get; set; }
    public string? Location     { get; set; }
}
```

Soft delete is applied via a global query filter: `IsDeleted == false` is appended automatically to all EF Core queries.

`Role` additionally carries `IsOrgRole` (bool) — `true` for school-scoped roles, `false` for system-level roles (OwnerAdmin, SuperAdmin, Admin).

---

## API Versioning

URL segment versioning: `/api/v{version}/resource`
Current version: `v1`
Default version: `1.0`

Controllers declare their version with `[ApiVersion("1.0")]` and route with `[Route("api/v{version:apiVersion}/[controller]")]`.

---

## Database Migrations

Migrations live in `src/SchoolManagement.DbInfrastructure/Migrations/`.

**Add a new migration** (after changing entities or configurations):
```bash
dotnet ef migrations add <MigrationName> \
  --project src/SchoolManagement.DbInfrastructure \
  --startup-project src/SchoolManagement.API \
  --output-dir Migrations
```

**Apply migrations to the database:**
```bash
dotnet ef database update \
  --project src/SchoolManagement.DbInfrastructure \
  --startup-project src/SchoolManagement.API
```

**Remove the last migration** (if not yet applied):
```bash
dotnet ef migrations remove \
  --project src/SchoolManagement.DbInfrastructure \
  --startup-project src/SchoolManagement.API
```

**Current migrations:**

| Migration | Tables Covered |
|---|---|
| `InitialCreate` | Users, Roles, RefreshTokens, PasswordResetTokens, AuditLogs, ErrorLogs, Countries, States, Cities |
| `AddScreenNameAndTableNameToAuditLog` | Adds `ScreenName` (nullable) and `TableName` columns + indexes to AuditLogs |
| `AddBatchIdAndParentAuditLogIdToAuditLog` | Adds `BatchId`, `ParentAuditLogId` columns + indexes to AuditLogs |
| `AddPayloadAndContextToErrorLog` | Adds `IpAddress`, `Location`, `HttpMethod`, `StatusCode`, `RequestPayload`, `ResponsePayload` to ErrorLogs |
| `AddOrgIdUserRoleMappingAndIsAdmin` | Adds `OrgId` to all BaseEntity tables; drops `Role` from Users; adds `IsAdmin` to Users; adds all BaseEntity columns to Roles; creates `UserRoleMappings` table |
| `AddOrganizationAndUserOrgMapping` | Removes `OrgId` from BaseEntity tables (including Roles at this stage); creates `Organizations` table; creates `UserOrganizationMappings` table |
| `RemoveOrgIdFromRole` | Removes `OrgId` column from `Roles` table |
| `AddIsOrgRoleToRole` | Adds `IsOrgRole` (bool, default false) column to `Roles` table |
| *(Menu/Page migrations)* | Creates `MenuMasters`, `PageMasters`, `PageMasterModules`, `PageMasterModuleActionMappings`, `MenuAndPagePermissions` tables |
| `AddIsUseForOwnerAdminToMenuAndPage` | Adds `IsUseMenuForOwnerAdmin` (bit, default 0) to `MenuMasters`; adds `IsUsePageForOwnerAdmin` (bit, default 0) to `PageMasters` |

> The `Microsoft.EntityFrameworkCore.Design` package is in `SchoolManagement.API.csproj` (as a private dev dependency) to support `dotnet ef` CLI tooling.

---

## Response Envelope

All endpoints return a consistent `ApiResponse<T>`:

```json
{
  "success": true,
  "message": "Operation successful",
  "data": { ... },
  "errors": null
}
```

Paginated lists use `PagedResult<T>` as the `data` value:

```json
{
  "items": [ ... ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5
}
```
