# Master Data — Country, State, City, Organization, Menu, Page, Permissions

Master data provides reference tables used throughout the system. Geographic data forms a strict hierarchy: **Country → State → City**. Organizations are independently managed tenants.

---

## Entity Relationships

```
Country (1) ──────── (N) State (1) ──────── (N) City
   │                        │                      │
   Id, Name, Code           Id, Name, Code          Id, Name
   IsActive                 CountryId (FK)          StateId (FK)
   IsDeleted                IsActive                IsActive
                            IsDeleted               IsDeleted
```

All three entities extend `BaseEntity` (audit fields + soft delete). See [ARCHITECTURE.md](ARCHITECTURE.md#baseentity).

---

## Entities

### Country
**File:** `src/SchoolManagement.Models/Entities/Country.cs`

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK, auto-increment |
| `Name` | string | Required, max 100 |
| `Code` | string | ISO alpha-3, e.g. `"IND"`, max 10 |
| `IsActive` | bool | Default `true` |
| `States` | `ICollection<State>` | Navigation — not serialized |
| *(BaseEntity fields)* | | CreatedAt, CreatedBy, IsDeleted, … |

---

### State
**File:** `src/SchoolManagement.Models/Entities/State.cs`

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK |
| `Name` | string | Required, max 100 |
| `Code` | string | Max 10 |
| `CountryId` | int | FK → Country.Id |
| `Country` | Country | Navigation |
| `IsActive` | bool | Default `true` |
| `Cities` | `ICollection<City>` | Navigation |
| *(BaseEntity fields)* | | |

---

### City
**File:** `src/SchoolManagement.Models/Entities/City.cs`

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK |
| `Name` | string | Required, max 100 |
| `StateId` | int | FK → State.Id |
| `State` | State | Navigation |
| `IsActive` | bool | Default `true` |
| *(BaseEntity fields)* | | |

---

### Organization
**File:** `src/SchoolManagement.Models/Entities/Organization.cs`

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK, auto-increment |
| `Name` | string | Required, max 200, unique |
| `Address` | string? | Optional, max 500 |
| `IsActive` | bool | Default `true` |
| `UserOrganizationMappings` | `ICollection<UserOrganizationMapping>` | Navigation |
| *(BaseEntity fields)* | | CreatedAt, CreatedBy, IsDeleted, … |

---

### UserOrganizationMapping
**File:** `src/SchoolManagement.Models/Entities/UserOrganizationMapping.cs`

Many-to-many join between `User` and `Organization`. A user can belong to multiple organisations.

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK, auto-increment |
| `UserId` | int | FK → User.Id (cascade delete) |
| `OrgId` | int | FK → Organization.Id (restrict delete) |
| `User` | User | Navigation |
| `Organization` | Organization | Navigation |
| *(BaseEntity fields)* | | |

Unique index on `(UserId, OrgId)` — a user cannot be mapped to the same org twice.

---

## DTOs

### Country DTOs
**File:** `src/SchoolManagement.Models/DTOs/Master/CountryDtos.cs`

**`CreateCountryRequest`**
```csharp
string Name   // required
string Code   // required
```

**`UpdateCountryRequest`**
```csharp
string Name
string Code
bool IsActive
```

**`CountryResponse`**
```csharp
int Id
string Name
string Code
bool IsActive
DateTime CreatedAt
```

---

### State DTOs
**File:** `src/SchoolManagement.Models/DTOs/Master/StateDtos.cs`

**`CreateStateRequest`**
```csharp
string Name
string Code
int CountryId
```

**`UpdateStateRequest`**
```csharp
string Name
string Code
bool IsActive
```

**`StateResponse`**
```csharp
int Id
string Name
string Code
int CountryId
string CountryName   // resolved via navigation
bool IsActive
DateTime CreatedAt
```

---

### City DTOs
**File:** `src/SchoolManagement.Models/DTOs/Master/CityDtos.cs`

**`CreateCityRequest`**
```csharp
string Name
int StateId
```

**`UpdateCityRequest`**
```csharp
string Name
bool IsActive
```

**`CityResponse`**
```csharp
int Id
string Name
int StateId
string StateName     // resolved via navigation
int CountryId        // resolved via State.CountryId
string CountryName   // resolved via State.Country.Name
bool IsActive
DateTime CreatedAt
```

---

### Organization DTOs
**File:** `src/SchoolManagement.Models/DTOs/Master/OrganizationDtos.cs`

**`CreateOrganizationRequest`**
```csharp
string Name    // required
string? Address
```

**`UpdateOrganizationRequest`**
```csharp
string Name
string? Address
bool IsActive
```

**`OrganizationResponse`**
```csharp
int Id
string Name
string? Address
bool IsActive
DateTime CreatedAt
```

---

## AutoMapper Mappings

**File:** `src/SchoolManagement.Models/Mappings/AutoMapperProfile.cs`

```csharp
// Country — direct property-to-property
CreateMap<Country, CountryResponse>();

// State — CountryName resolved from navigation property
CreateMap<State, StateResponse>()
    .ForMember(d => d.CountryName, o => o.MapFrom(s => s.Country.Name));

// City — StateName, CountryId, CountryName from nested navigation
CreateMap<City, CityResponse>()
    .ForMember(d => d.StateName,   o => o.MapFrom(s => s.State.Name))
    .ForMember(d => d.CountryId,   o => o.MapFrom(s => s.State.CountryId))
    .ForMember(d => d.CountryName, o => o.MapFrom(s => s.State.Country.Name));

// Organization — direct property-to-property
CreateMap<Organization, OrganizationResponse>();
```

> Navigation properties must be loaded (via `.Include()` or Dapper join) before mapping, otherwise nested fields will be `null`.

---

## Service Layer

### ICountryService / CountryService
**Files:**
- `src/SchoolManagement.Services/Interfaces/ICountryService.cs`
- `src/SchoolManagement.Services/Implementations/CountryService.cs`

**Methods**

| Method | Description |
|---|---|
| `CreateAsync(request)` | Validates no duplicate name/code, inserts, returns mapped DTO |
| `UpdateAsync(id, request)` | Loads entity, updates fields, saves |
| `DeleteAsync(id)` | Sets `IsDeleted = true` (soft delete) |
| `GetByIdAsync(id)` | Dapper read with `CountryQueries.GetById` |
| `GetAllAsync(pagination)` | Dapper paged read with optional search |

---

### IStateService / StateService
**Files:**
- `src/SchoolManagement.Services/Interfaces/IStateService.cs`
- `src/SchoolManagement.Services/Implementations/StateService.cs`

**Methods**

| Method | Description |
|---|---|
| `CreateAsync(request)` | Validates CountryId exists, inserts |
| `UpdateAsync(id, request)` | Updates Name, Code, IsActive |
| `DeleteAsync(id)` | Soft delete |
| `GetByIdAsync(id)` | Dapper read with navigation join |
| `GetAllAsync(pagination)` | Paged list |
| `GetByCountryAsync(countryId)` | All active states for a country |

---

### ICityService / CityService
**Files:**
- `src/SchoolManagement.Services/Interfaces/ICityService.cs`
- `src/SchoolManagement.Services/Implementations/CityService.cs`

**Methods**

| Method | Description |
|---|---|
| `CreateAsync(request)` | Validates StateId exists, inserts |
| `UpdateAsync(id, request)` | Updates Name, IsActive |
| `DeleteAsync(id)` | Soft delete |
| `GetByIdAsync(id)` | Dapper read (joins State + Country) |
| `GetAllAsync(pagination)` | Paged list |
| `GetByStateAsync(stateId)` | All active cities for a state |

---

### IOrganizationService / OrganizationService
**Files:**
- `src/SchoolManagement.Services/Interfaces/IOrganizationService.cs`
- `src/SchoolManagement.Services/Implementations/OrganizationService.cs`

**Methods**

| Method | Description |
|---|---|
| `CreateAsync(request)` | Validates no duplicate name, inserts, returns mapped DTO |
| `UpdateAsync(id, request)` | Updates Name, Address, IsActive |
| `DeleteAsync(id)` | Soft delete |
| `GetByIdAsync(id)` | Dapper read with `OrganizationQueries.GetById` |
| `GetAllAsync(pagination)` | Dapper paged read with optional search on Name/Address |

---

## Soft Delete

No records are physically removed. Deleting sets `IsDeleted = true`. EF Core global query filters exclude deleted records automatically:

```csharp
// Defined in each entity's IEntityTypeConfiguration:
builder.HasQueryFilter(c => !c.IsDeleted);  // Country, State, City, Organization, UserOrganizationMapping
```

Dapper queries include `WHERE IsDeleted = 0` explicitly in their SQL constants.

---

## Database Seeding

**Files:**
- `src/SchoolManagement.Seeding/Seeding/RoleSeeder.cs`
- `src/SchoolManagement.Seeding/Seeding/UserSeeder.cs`
- `src/SchoolManagement.Seeding/Seeding/CountrySeeder.cs`
- `src/SchoolManagement.Seeding/Seeding/DatabaseSeeder.cs`

Seeders implement `ISeeder`:

```csharp
public interface ISeeder
{
    Task<bool> IsSeededAsync(CancellationToken cancellationToken = default);
    Task SeedAsync(CancellationToken cancellationToken = default);
}
```

`DatabaseSeeder` runs all registered `ISeeder` implementations at startup in this order:

| Seeder | Seeds | Check |
|---|---|---|
| `RoleSeeder` | 29 roles (3 system + 26 school roles, Ids 1–29) — see list below | `Roles` table not empty |
| `UserSeeder` | Default admin user (`superadmin` / `phalodi@123`, `IsAdmin=true`) + `UserRoleMapping` linking to Super Admin | Any user with `IsAdmin = true` |
| `CountrySeeder` | Common countries (India, USA, UK, Australia, etc.) | `Countries` table not empty |

Each seeder checks `IsSeededAsync()` first — if data already exists, it skips. This makes seeding idempotent and safe to run on every startup.

**Seeded roles (RoleSeeder):**

| Id | Name | Category | IsOrgRole |
|---|---|---|---|
| 1 | Owner Admin | System | `false` |
| 2 | Super Admin | System | `false` |
| 3 | Admin | System | `false` |
| 4 | Student | Academic | `true` |
| 5 | Teacher | Academic | `true` |
| 6 | Head Teacher | Academic | `true` |
| 7 | Principal | Academic | `true` |
| 8 | Vice Principal | Academic | `true` |
| 9 | Coordinator | Academic | `true` |
| 10 | Parent | Parent / Guardian | `true` |
| 11 | Guardian | Parent / Guardian | `true` |
| 12 | School Administrator | Administrative | `true` |
| 13 | Office Staff | Administrative | `true` |
| 14 | Clerk | Administrative | `true` |
| 15 | Accountant | Administrative | `true` |
| 16 | Librarian | Administrative | `true` |
| 17 | Lab Assistant | Administrative | `true` |
| 18 | IT Staff | Administrative | `true` |
| 19 | Receptionist | Administrative | `true` |
| 20 | Counselor | Administrative | `true` |
| 21 | Special Educator | Administrative | `true` |
| 22 | Nurse | Health | `true` |
| 23 | Medical Staff | Health | `true` |
| 24 | Driver | Support / Operations | `true` |
| 25 | Conductor | Support / Operations | `true` |
| 26 | Attendant | Support / Operations | `true` |
| 27 | Security Guard | Support / Operations | `true` |
| 28 | Cleaner | Support / Operations | `true` |
| 29 | Maintenance Staff | Support / Operations | `true` |

---

---

## Menu & Page Hierarchy

```
MenuMaster (1) ────────── (N) PageMaster (1) ────── (N) PageMasterModule (1) ── (N) PageMasterModuleActionMapping
                                   │                           │
                                   └── IsUsePageForOwnerAdmin  └── PageMasterModuleActionMapping.ActionId (ActionType enum)
     │
     └── IsUseMenuForOwnerAdmin

MenuAndPagePermissions: MenuId + PageId + PageModuleId + ActionId + RoleId + IsAllowed
```

### MenuMaster
**File:** `src/SchoolManagement.Models/Entities/MenuMaster.cs`

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK, auto-increment |
| `Name` | string | Required |
| `HasChild` | bool | True = this menu has sub-menus |
| `ParentMenuId` | int? | FK → MenuMaster.Id (self-referencing) |
| `Position` | int | Display order |
| `IconClass` | string? | CSS icon class |
| `IsActive` | bool | Default `true` |
| `IsUseMenuForOwnerAdmin` | bool | Default `false` — reserved for OwnerAdmin-specific logic |
| *(BaseEntity fields)* | | |

### PageMaster
**File:** `src/SchoolManagement.Models/Entities/PageMaster.cs`

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK |
| `Name` | string | Required |
| `IconClass` | string? | |
| `PageUrl` | string | Route path |
| `MenuId` | int | FK → MenuMaster.Id |
| `IsActive` | bool | Default `true` |
| `IsUsePageForOwnerAdmin` | bool | Default `false` |
| *(BaseEntity fields)* | | |

> **HasChild rule**: If `MenuMaster.HasChild = false`, only one `PageMaster` is allowed per menu. `PageMasterService.CreatePageAsync` enforces this.

### PageMasterModule
**File:** `src/SchoolManagement.Models/Entities/PageMasterModule.cs`

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK |
| `Name` | string | Module name (e.g. "Student List") |
| `PageId` | int | FK → PageMaster.Id |
| `IsActive` | bool | Default `true` |

### PageMasterModuleActionMapping
**File:** `src/SchoolManagement.Models/Entities/PageMasterModuleActionMapping.cs`

Maps each module to a set of allowed `ActionType` enum values.

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK |
| `PageId` | int | FK → PageMaster.Id |
| `PageModuleId` | int | FK → PageMasterModule.Id |
| `ActionId` | ActionType | `ADD=1, EDIT=2, DELETE=3, VIEW_LIST=4, VIEW_DETAILS=5, UPDATE_PROGRESS=6, STATUS_CHANGE=7` |

### MenuAndPagePermission
**File:** `src/SchoolManagement.Models/Entities/MenuAndPagePermission.cs`

Role-based permission record: `(MenuId, PageId, PageModuleId, ActionId, RoleId) → IsAllowed`.

| Property | Type | Notes |
|---|---|---|
| `Id` | int | PK |
| `MenuId` | int | FK → MenuMaster.Id |
| `PageId` | int | FK → PageMaster.Id |
| `PageModuleId` | int | FK → PageMasterModule.Id |
| `ActionId` | ActionType | |
| `RoleId` | int | FK → Role.Id |
| `IsAllowed` | bool | Default `false` — deny-by-default |

Permissions are **auto-seeded** when a page with modules is created (`PageMasterService.CreatePageAsync`) — one row per `(module, action, role)` combination, `IsAllowed = false`. They are also seeded for newly added roles in `RoleSeeder.SeedPermissionsForNewRolesAsync`.

---

## EF Core Configurations

| Entity | Configuration File |
|---|---|
| Country | `src/SchoolManagement.DbInfrastructure/Configurations/CountryConfiguration.cs` |
| State | `src/SchoolManagement.DbInfrastructure/Configurations/StateConfiguration.cs` |
| City | `src/SchoolManagement.DbInfrastructure/Configurations/CityConfiguration.cs` |
| Organization | `src/SchoolManagement.DbInfrastructure/Configurations/OrganizationConfiguration.cs` |
| UserOrganizationMapping | `src/SchoolManagement.DbInfrastructure/Configurations/UserOrganizationMappingConfiguration.cs` |

Configurations define:
- Column types and max lengths
- Required fields
- FK relationships and cascade behavior
- Indexes on `Name`, `Code`, `IsDeleted`
