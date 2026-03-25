# AutoMapper

## Overview

AutoMapper is used to map EF Core entities to response DTOs. This keeps controllers and services free of manual property assignments and ensures consistent, testable mappings in one place.

**Profile file:** `src/SchoolManagement.Models/Mappings/AutoMapperProfile.cs`

**Registration (Program.cs):**
```csharp
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
```

---

## All Mappings

### Country → CountryResponse

```csharp
CreateMap<Country, CountryResponse>();
```

All properties map by convention (same name, compatible type). No custom configuration needed.

| Source (`Country`) | Destination (`CountryResponse`) |
|---|---|
| `Id` | `Id` |
| `Name` | `Name` |
| `Code` | `Code` |
| `IsActive` | `IsActive` |
| `CreatedAt` | `CreatedAt` |

---

### State → StateResponse

```csharp
CreateMap<State, StateResponse>()
    .ForMember(d => d.CountryName,
               o => o.MapFrom(s => s.Country.Name));
```

`CountryName` does not exist on `State` directly — it is resolved from the `Country` navigation property.

| Source | Destination | Note |
|---|---|---|
| `Id` | `Id` | Convention |
| `Name` | `Name` | Convention |
| `Code` | `Code` | Convention |
| `CountryId` | `CountryId` | Convention |
| `Country.Name` | `CountryName` | Custom `ForMember` |
| `IsActive` | `IsActive` | Convention |
| `CreatedAt` | `CreatedAt` | Convention |

> **Prerequisite:** `State.Country` must be loaded before mapping. The service uses `.Include(s => s.Country)` or a Dapper join.

---

### City → CityResponse

```csharp
CreateMap<City, CityResponse>()
    .ForMember(d => d.StateName,
               o => o.MapFrom(s => s.State.Name))
    .ForMember(d => d.CountryId,
               o => o.MapFrom(s => s.State.CountryId))
    .ForMember(d => d.CountryName,
               o => o.MapFrom(s => s.State.Country.Name));
```

`City` only has `StateId`; `StateName`, `CountryId`, and `CountryName` are all resolved via nested navigation.

| Source | Destination | Note |
|---|---|---|
| `Id` | `Id` | Convention |
| `Name` | `Name` | Convention |
| `StateId` | `StateId` | Convention |
| `State.Name` | `StateName` | Custom `ForMember` |
| `State.CountryId` | `CountryId` | Custom `ForMember` |
| `State.Country.Name` | `CountryName` | Custom `ForMember` (2-level deep) |
| `IsActive` | `IsActive` | Convention |
| `CreatedAt` | `CreatedAt` | Convention |

> **Prerequisite:** `City.State` and `City.State.Country` must both be loaded. The service uses `.Include(c => c.State).ThenInclude(s => s.Country)`.

---

### Organization → OrganizationResponse

```csharp
CreateMap<Organization, OrganizationResponse>();
```

All properties map by convention (same name, compatible type). No custom configuration needed.

| Source (`Organization`) | Destination (`OrganizationResponse`) |
|---|---|
| `Id` | `Id` |
| `Name` | `Name` |
| `Address` | `Address` |
| `IsActive` | `IsActive` |
| `CreatedAt` | `CreatedAt` |

---

### MenuMaster → MenuResponse

```csharp
CreateMap<MenuMaster, MenuResponse>();
```

Direct convention mapping — no custom `ForMember` needed.

---

### PageMaster → PageResponse

```csharp
CreateMap<PageMaster, PageResponse>();
```

Direct convention mapping.

---

### MenuAndPagePermission → MenuAndPagePermissionResponse

```csharp
CreateMap<MenuAndPagePermission, MenuAndPagePermissionResponse>();
```

Direct convention mapping.

---

### CreatePageRequest → PageMaster (inbound)

```csharp
CreateMap<CreatePageRequest, PageMaster>()
    .ForMember(dest => dest.Menu,        opt => opt.Ignore())
    .ForMember(dest => dest.Modules,     opt => opt.Ignore())
    .ForMember(dest => dest.Permissions, opt => opt.Ignore());
```

Navigation properties are excluded — they are populated by `PageMasterService` separately during hierarchical seeding.

---

### UpdatePageRequest → PageMaster (inbound)

```csharp
CreateMap<UpdatePageRequest, PageMaster>()
    .ForMember(dest => dest.Menu,        opt => opt.Ignore())
    .ForMember(dest => dest.Modules,     opt => opt.Ignore())
    .ForMember(dest => dest.Permissions, opt => opt.Ignore());
```

Used with `_mapper.Map(request, existingPage)` to apply only scalar field changes in `UpdatePageAsync`.

---

## Services That Use AutoMapper

| Service | File | Usage |
|---|---|---|
| `CountryService` | `Services/Implementations/CountryService.cs` | `_mapper.Map<CountryResponse>(country)` |
| `StateService` | `Services/Implementations/StateService.cs` | `_mapper.Map<StateResponse>(state)` |
| `CityService` | `Services/Implementations/CityService.cs` | `_mapper.Map<CityResponse>(city)` |
| `OrganizationService` | `Services/Implementations/OrganizationService.cs` | `_mapper.Map<OrganizationResponse>(org)` |
| `MenuMasterService` | `Services/Implementations/MenuMasterService.cs` | `_mapper.Map<MenuResponse>(menu)` |
| `PageMasterService` | `Services/Implementations/PageMasterService.cs` | `_mapper.Map<PageMaster>(request)` (create); `_mapper.Map(request, page)` (update); `_mapper.Map<PageResponse>(page)` (response) |

All services inject `IMapper` via constructor.

---

## Services That Do NOT Use AutoMapper

### AuthService — Manual Mapping (Intentional)

**File:** `src/SchoolManagement.Services/Implementations/AuthService.cs`

`AuthService` builds `LoginResponse` from **two independent sources**:

```
User entity        →  Username, Role
Computed at runtime →  AccessToken (JWT), RefreshToken (random bytes), AccessTokenExpiry
```

```csharp
return new LoginResponse
{
    Username          = user.Username,
    Role              = roleNames.FirstOrDefault() ?? string.Empty,  // from UserRoleMapping
    AccessToken       = GenerateJwt(user, roleNames),   // computed
    RefreshToken      = GenerateRefreshToken(),          // computed
    AccessTokenExpiry = DateTime.UtcNow.AddMinutes(expiry)           // computed
};
```

`Role` is resolved by querying `UserRoleMappings` at token-generation time — it is not a stored property on `User`. Three of the five fields (`AccessToken`, `RefreshToken`, `AccessTokenExpiry`) are **generated values that do not exist on the `User` entity**. AutoMapper is designed for entity→DTO projection from a single source object. Using it here would require `IMappingAction<User, LoginResponse>` or `AfterMap` hooks injecting the token service — which adds complexity without benefit.

Manual construction is the right choice here.

---

### AuditLogService — No Mapping Needed

**File:** `src/SchoolManagement.Services/Implementations/AuditLogService.cs`

Returns the `AuditLog` entity directly via a Dapper query that selects exactly the columns needed. There is no separate `AuditLogResponse` DTO, so no mapping is required.

---

### Infrastructure / Common Services

`EmailService`, `EncryptionService`, `RequestContext`, `ErrorLogService`, repositories — none of these deal with entity→DTO projection. They handle encryption, I/O, request metadata, and data access respectively, so AutoMapper is not applicable.

---

## Convention Rules

AutoMapper maps properties **by name** (case-insensitive). Any property on the destination that has the same name as a source property is mapped automatically. `ForMember` is only used when:

1. The destination property name differs from the source.
2. The value comes from a nested navigation property.
3. Transformation logic is needed (e.g., enum to string).

Avoid using AutoMapper for computed values that require injected services — use manual construction instead.
