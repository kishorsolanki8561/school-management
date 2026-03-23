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
│   └── SchoolManagement.Seeding/         # Database seed data (roles, countries)
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
Update `src/SchoolManagement.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SchoolManagement;Trusted_Connection=True;"
  },
  "JwtSettings": {
    "SecretKey": "your-minimum-32-character-secret-key",
    "Issuer": "SchoolManagement",
    "Audience": "SchoolManagementClient",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 3. Apply Migrations
```bash
cd src/SchoolManagement.DbInfrastructure
dotnet ef database update --startup-project ../SchoolManagement.API
```

### 4. Run
```bash
cd src/SchoolManagement.API
dotnet run
```

Swagger UI: `https://localhost:{port}/swagger`

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
- Audit trail on every write (CreatedBy, ModifiedBy, IpAddress, Location, ScreenName, TableName)
- API versioning (`/api/v1/...`)
- Paginated list responses with search
- E2E encryption support (RSA-2048 + AES-256-GCM, configurable)
- Database seeding on startup (idempotent)
