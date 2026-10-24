using CoreUp.Api.Administration;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Security.Claims;
using CoreUp.Application;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using CoreUp.Api;
using CoreUp.Infrastructure;
using CoreUp.Infrastructure.Authentication;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

EnvFile.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CoreUp API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            []
        }
    });
});

builder.Services.AddOptions<OtpOptions>().Bind(builder.Configuration.GetSection("Otp"))
    .Validate(o => Encoding.UTF8.GetByteCount(o.Key) >= 32 && !o.Key.StartsWith("replace_", StringComparison.OrdinalIgnoreCase), "Otp:Key must contain at least 32 bytes from an environment variable or user secret.")
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { context.Fail("Invalid account."); return; }
                var db = context.HttpContext.RequestServices.GetRequiredService<CoreUpDbContext>();
                var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, context.HttpContext.RequestAborted);
                var versionClaim = principal.FindFirstValue("token_version");
                var version = 0; // Legacy Phase 1 tokens remain valid until a password change.
                if (versionClaim is not null && !int.TryParse(versionClaim, out version)) { context.Fail("Invalid session."); return; }
                if (user is null || user.TokenVersion != version) { context.Fail("Session revoked."); return; }
                var path = context.Request.Path;
                if (user.VerificationRequired && !user.EmailVerified &&
                    !path.StartsWithSegments("/api/auth/email-verification") && path != "/api/auth/me")
                    context.Fail("Email verification required.");
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
builder.AddAdmin();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("admin-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/admin")) {
        var remote = context.Connection.RemoteIpAddress;
        if (remote == null || !System.Net.IPAddress.IsLoopback(remote)) {
            context.Response.StatusCode = 403; return;
        }
        if (!context.Request.IsHttps) {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Admin requires HTTPS. Use https://localhost:7031/admin with the https launch profile."); return;
        }
        // Administration must not run using an operator/superuser connection.
        var accessDb = context.RequestServices.GetRequiredService<CoreUpDbContext>();
        var elevated = await accessDb.Database.SqlQueryRaw<bool>("""
            SELECT (r.rolsuper OR r.rolcreatedb OR r.rolcreaterole
                OR EXISTS (SELECT 1 FROM pg_database d WHERE d.datname = current_database() AND d.datdba = r.oid)
                OR EXISTS (SELECT 1 FROM pg_tables t WHERE t.schemaname = current_schema() AND t.tableowner = current_user)) AS "Value"
            FROM pg_roles r WHERE r.rolname = current_user
            """).SingleAsync(context.RequestAborted);
        if (elevated) {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync("Admin requires a dedicated least-privilege application database connection. See tools/admin/README.md."); return;
        }
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'; form-action 'self'; base-uri 'none'";
    }
    await next(context);
});
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.MapGet("/health", async (CoreUpDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "Healthy", database = "Connected" })
        : Results.Json(new { status = "Unhealthy", database = "Disconnected" }, statusCode: 503));

app.Run();

public partial class Program;
