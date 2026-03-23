# Testing

## Framework & Libraries

| Library | Version | Purpose |
|---|---|---|
| XUnit | Latest | Test runner |
| Moq | Latest | Mocking dependencies |
| FluentAssertions | Latest | Readable assertions |
| Microsoft.EntityFrameworkCore.InMemory | Latest | In-memory DB for EF Core isolation |

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
| AuthService | `Services/AuthServiceTests.cs` | Login, register, refresh token, logout, forgot/reset password, token expiry |
| AuditLogService | `Services/AuditLogServiceTests.cs` | GetByEntity, GetByUser, GetByScreen, GetByTable |
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

### AuditLogService
- GetByEntity — returns paged result for entity name + id
- GetByUser — returns paged result for user id
- GetByScreen — returns paged result for screen name
- GetByTable — returns paged result for table name

### AuthService
- Register — creates user with hashed password
- Register — throws on duplicate username/email
- Login — returns access + refresh token on valid credentials
- Login — throws on wrong password
- Refresh — rotates tokens on valid refresh token
- Refresh — throws on expired/revoked token
- Logout — revokes refresh token
- ForgotPassword — sends reset email
- ResetPassword — changes password and marks token used
- ResetPassword — throws on expired token

---

## Test Assembly Config

**File:** `tests/SchoolManagement.Tests/TestAssemblyConfig.cs`

Handles test assembly initialization — sets up any shared infrastructure needed across all test classes (e.g., AutoMapper configuration, test-scoped logging).
