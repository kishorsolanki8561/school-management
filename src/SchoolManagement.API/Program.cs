using Microsoft.AspNetCore.Mvc.ApiExplorer;
using SchoolManagement.API.Extensions;
using SchoolManagement.API.Hubs;
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
builder.Services.AddDbInfrastructure();

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

// ── SignalR (real-time in-app notifications) ──────────────────────────────────
builder.Services.AddSignalR();

// ── HttpClient (used by SMS channel handlers) ─────────────────────────────────
builder.Services.AddHttpClient();

// ── Swagger ────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocs();

// ── CORS ──────────────────────────────────────────────────────────────────────
// Origins are configured in appsettings.json → CorsSettings:AllowedOrigins.
// Add your UI origin (e.g. http://localhost:4200 or https://app.yourdomain.com).
// AllowCredentials() is required so the browser forwards the Authorization header.
// Note: AllowCredentials() cannot be combined with AllowAnyOrigin(); specific
// origins must always be listed in CorsSettings:AllowedOrigins.
const string CorsPolicy = "SchoolManagementCors";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var origins = InitializeConfiguration.CorsSettings.AllowedOrigins;

        if (origins is { Length: > 0 })
        {
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .SetIsOriginAllowedToAllowWildcardSubdomains();
        }
        else
        {
            // Fallback: allow all origins without credentials (dev convenience only)
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// ── Seed default data (roles etc.) — idempotent, safe every startup ───────────
await app.SeedDatabaseAsync();

// ── Middleware pipeline ────────────────────────────────────────────────────────

// 1. Exception handling — must be first to catch all downstream exceptions
app.UseExceptionHandling();

// 2. Swagger UI (available in all environments)
var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwaggerDocs(apiVersionProvider);

// 3. HTTPS redirect
app.UseHttpsRedirection();

// 4. CORS
app.UseCors(CorsPolicy);

// 5. Authentication & Authorization — must run before RequestContextMiddleware
//    so context.User is populated from the JWT before we read its claims
app.UseAuthentication();
app.UseAuthorization();

// 6. Request context — runs after auth so context.User.Identity.IsAuthenticated = true
//    and UserId / Username / Role are correctly extracted into IRequestContext
app.UseRequestContextMiddleware();

// 7. E2E Encryption middleware (decrypts requests, encrypts responses)
// app.UseEncryption(); // Uncomment to enable E2E encryption for all non-bypass routes

// 8. Root redirect → Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// 9. Controllers
app.MapControllers();

// 10. SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

// Expose for integration testing
public partial class Program { }
