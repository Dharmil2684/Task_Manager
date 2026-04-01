using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Serilog;
using TaskManager.API.Middleware;
using TaskManager.Infrastructure;
using TaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog Configuration ──────────────────────────────────────────
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// ── Clean Architecture DI ──────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// ── Health Checks ──────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Authentication ─────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:SecretKey"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("Jwt:SecretKey configuration is missing in appsettings.json.");
}
if (string.IsNullOrEmpty(jwtIssuer))
{
    throw new InvalidOperationException("Jwt:Issuer configuration is missing in appsettings.json.");
}
if (string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("Jwt:Audience configuration is missing in appsettings.json.");
}

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ── Swagger / OpenAPI with JWT Bearer ──────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskManager API",
        Version = "v1",
        Description = "Multi-tenant Task Manager SaaS API"
    });

    // JWT Bearer security scheme — enables the "Authorize" button in Swagger UI
    const string schemeId = "bearer";

    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme. Paste only the token — Swagger adds the 'Bearer' prefix automatically.",
        Name = "Authorization",
        In = ParameterLocation.Header
    });

    // Swashbuckle 10.x delegate pattern — OpenApiSecurityRequirement uses List<string>
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(schemeId, document),
            new List<string>()
        }
    });
});

// ── Rate Limiting — throttle anonymous auth endpoints ──────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;                       // max 10 requests …
        limiter.Window = TimeSpan.FromMinutes(1);       // … per minute
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;                         // reject immediately when the limit is hit
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please try again later." },
            cancellationToken);
    };
});

var app = builder.Build();

// ── Cosmos DB initialisation ───────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<CosmosDbInitializer>();
    await dbInitializer.InitializeAsync();
}

// ── Middleware pipeline ────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

// Enable Swagger UI across all environments for testing.
// In the future, you may want to restrict this to only Development or specific IPs.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskManager API V1");
    // Leave RoutePrefix as string.Empty if you want it to load immediately at the root (/) instead of /swagger
    // But since the user visits /swagger, we'll let it be default just in case. 
    // Wait, the original code had c.RoutePrefix = string.Empty; 
    c.RoutePrefix = "swagger"; 
});

// app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
