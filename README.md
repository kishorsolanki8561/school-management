# School Management System

A modular monolithic ASP.NET Core 6 Web API for managing school operations — authentication, master data (countries, states, cities), user management, and audit logging.

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 6 Web API |
| ORM | Entity Framework Core 6 (writes) + Dapper (reads) |
| Database | SQL Server |
| Auth | JWT Bearer + Refresh Tokens |
| Mapping | AutoMapper |
| Testing | XUnit + Moq + FluentAssertions + In-Memory DB |
| Docs | Swagger / OpenAPI |

## Solution Structure

```
school-management/
├── src/
│   ├── SchoolManagement.API/             # Controllers, middleware pipeline, DI wiring
│   ├── SchoolManagement.Models/          # Entities, DTOs, AutoMapper profiles, enums
│   ├── SchoolManagement.Services/        # Business logic (interfaces + implementations)
│   ├── SchoolManagement.DbInfrastructure/# EF Core DbContext, repositories, configurations
│   ├── SchoolManagement.Common/          # Encryption, email, request context, helpers
│   └── SchoolManagement.Seeding/         # Database seed data (roles, users, countries)
├── tests/
│   └── SchoolManagement.Tests/           # XUnit unit/integration tests
└── docs/
    ├── ARCHITECTURE.md
    ├── API.md
    ├── MASTERS.md
    ├── TESTING.md
    └── AUTOMAPPER.md
```

## Quick Start

### 1. Prerequisites
- .NET 6 SDK
- SQL Server (local or remote)

### 2. Configure
Update the connection string in `src/SchoolManagement.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SchoolManagement;Trusted_Connection=True;"
  }
}
```

> `JwtSettings.SecretKey` and `EncryptionSettings.AesKey` are already set to real cryptographic values — no changes needed for development.

### 3. Apply Migrations
```bash
dotnet ef database update \
  --project src/SchoolManagement.DbInfrastructure \
  --startup-project src/SchoolManagement.API
```

### 4. Run
```bash
dotnet run --project src/SchoolManagement.API
```

Swagger UI opens automatically at `https://localhost:{port}/swagger` (browser auto-launches on F5 / `dotnet run`).

**Default SuperAdmin credentials (seeded on first run):**
| Field | Value |
|---|---|
| Username | `superadmin` |
| Password | `phalodi@123` |

### 5. Run Tests
```bash
dotnet test tests/SchoolManagement.Tests
```

## Documentation

| Doc | Description |
|---|---|
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Modular monolithic layers and request lifecycle |
| [API.md](docs/API.md) | All REST endpoints with request/response formats |
| [MASTERS.md](docs/MASTERS.md) | Country, State, City master data design |
| [TESTING.md](docs/TESTING.md) | Test strategy, coverage, and patterns |
| [AUTOMAPPER.md](docs/AUTOMAPPER.md) | Mapping profiles and conventions |

## Key Features

- JWT authentication with refresh token rotation
- Soft delete on all master entities (`IsDeleted` flag)
- Audit trail on every write (CreatedBy, ModifiedBy, IpAddress, Location, ScreenName, TableName, BatchId, ParentAuditLogId)
- Parent–child audit linking — child entities automatically linked to their parent's audit row via EF FK metadata
- API versioning (`/api/v1/...`)
- Paginated list responses with search
- E2E encryption support (RSA-2048 + AES-256-GCM, configurable)
- Database seeding on startup (idempotent)
