using Microsoft.AspNetCore.Mvc.ApiExplorer;
using SchoolManagement.API.Extensions;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Extensions;
using SchoolManagement.DbInfrastructure.Extensions;
using SchoolManagement.Models.Mappings;
using SchoolManagement.Seeding.Extensions;
using SchoolManagement.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── Settings initialization (populates InitializeConfiguration + AppConfigFactory) ─
InitializeConfiguration.Initialize(builder.Configuration);

// ── Common services (encryption, request context, file helpers) ───────────────
builder.Services.AddCommonServices();

// ── Database + Repositories (EF + Dapper) ─────────────────────────────────────
builder.Services.AddDbInfrastructure(builder.Configuration);

// ── Application services (scoped business logic) ──────────────────────────────
builder.Services.AddApplicationServices();

// ── Data seeding (idempotent — runs on every startup) ─────────────────────────
builder.Services.AddSeeding();

// ── AutoMapper ────────────────────────────────────────────────────────────────
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services.AddJwtAuthentication();
builder.Services.AddAuthorization();

// ── API versioning (URL segment: /api/v1/...) ─────────────────────────────────
builder.Services.AddApiVersioningConfig();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger ────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocs();

// ── CORS (configure per environment) ─────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ── Seed default data (roles etc.) — idempotent, safe every startup ───────────
await app.SeedDatabaseAsync();

// ── Middleware pipeline ────────────────────────────────────────────────────────

// 1. Exception handling — must be first to catch all downstream exceptions
app.UseExceptionHandling();

// 2. Swagger UI (available in all environments — restrict to Development only in production if required)
var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwaggerDocs(apiVersionProvider);

// 3. HTTPS redirect
app.UseHttpsRedirection();

// 4. CORS
app.UseCors();

// 5. Request context (populates IRequestContext from JWT + IP)
app.UseRequestContextMiddleware();

// 6. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 7. E2E Encryption middleware (decrypts requests, encrypts responses)
// app.UseEncryption(); // Uncomment to enable E2E encryption for all non-bypass routes

// 8. Controllers
app.MapControllers();

app.Run();

// Expose for integration testing
public partial class Program { }
