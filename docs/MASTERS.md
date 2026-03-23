# Master Data — Country, State, City

Master data provides reference tables used throughout the system. The three master entities form a strict hierarchy: **Country → State → City**.

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

## Soft Delete

No records are physically removed. Deleting sets `IsDeleted = true`. EF Core global query filters exclude deleted records automatically:

```csharp
// In DbContext.OnModelCreating:
modelBuilder.Entity<Country>().HasQueryFilter(c => !c.IsDeleted);
modelBuilder.Entity<State>().HasQueryFilter(s => !s.IsDeleted);
modelBuilder.Entity<City>().HasQueryFilter(c => !c.IsDeleted);
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
| `RoleSeeder` | Default roles (SuperAdmin, SchoolAdmin, Supervisor, Teacher, Student) | `Roles` table not empty |
| `UserSeeder` | Default SuperAdmin user (`superadmin` / `phalodi@123`) | Any SuperAdmin user exists |
| `CountrySeeder` | Common countries (India, USA, UK, Australia, etc.) | `Countries` table not empty |

Each seeder checks `IsSeededAsync()` first — if data already exists, it skips. This makes seeding idempotent and safe to run on every startup.

---

## EF Core Configurations

| Entity | Configuration File |
|---|---|
| Country | `src/SchoolManagement.DbInfrastructure/Configurations/CountryConfiguration.cs` |
| State | `src/SchoolManagement.DbInfrastructure/Configurations/StateConfiguration.cs` |
| City | `src/SchoolManagement.DbInfrastructure/Configurations/CityConfiguration.cs` |

Configurations define:
- Column types and max lengths
- Required fields
- FK relationships and cascade behavior
- Indexes on `Name`, `Code`, `IsDeleted`
