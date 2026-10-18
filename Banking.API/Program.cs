using System.Reflection;
using System.Text.Json.Serialization;
using Banking.API;
using Banking.API.Auth;
using Banking.API.Localization;
using Banking.API.Middleware;
using Banking.API.RateLimiting;
using Banking.Application;
using Banking.Application.Interfaces;
using Banking.Infrastructure;
using Banking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC + JSON: enums travel as their names ("Deposit", "M"), never as integers.
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ValidationProblemResponse.Create);

// Every error (validation, business rule, not found, unexpected) is an RFC 7807 problem response
// that also carries the request's correlation id.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = CorrelationIdMiddleware.AddToProblem);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Error texts come from Resources/ErrorMessages(.es).resx in the culture chosen from Accept-Language:
// en-US (default) or es-NI, with plain "en"/"es" and other regional variants mapped to those two.
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(SupportedCultures.Default);
    options.SupportedCultures = SupportedCultures.All.ToList();
    options.SupportedUICultures = SupportedCultures.All.ToList();
    options.RequestCultureProviders = [new AcceptLanguageCultureProvider()];
    options.ApplyCurrentCultureToResponseHeaders = true;
});

// JWT bearer authentication. The signing key comes from configuration (a development-only one in
// appsettings.Development.json; the Jwt__SigningKey environment variable elsewhere) and is checked
// at start-up, so a deployment without a real secret fails immediately instead of issuing tokens.
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentCustomer, HttpContextCurrentCustomer>();
builder.Services.AddSingleton<IAccessTokenIssuer, JwtTokenIssuer>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
    {
        // Keep the raw claim names ("sub", "name") instead of the legacy SOAP-style ones.
        bearer.MapInboundClaims = false;
        bearer.Events = AuthenticationProblemResponses.Events;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Value.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Value.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwt.Value.SigningKeyBytes),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Name,
        };
    });

// Secure by default: every endpoint needs an authenticated customer unless it opts out with
// [AllowAnonymous] (registration, login, health). Ownership is enforced by the use cases.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Banking API",
        Version = ApiVersion.Current,
        Description = "Customers, bank accounts and ledger operations (deposits, withdrawals, interest). "
            + "Register with POST /api/Customer, log in with POST /api/Auth/login and press Authorize to use the token: "
            + "every other operation is restricted to the authenticated customer's own data. "
            + "Error messages are returned in English or Spanish according to the Accept-Language header; "
            + "every problem response also carries a stable `code` so clients need not parse the text.",
    });
    options.AddSecurityDefinition(BearerSecurityOperationFilter.SchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by POST /api/Auth/login (without the 'Bearer ' prefix).",
    });
    options.OperationFilter<BearerSecurityOperationFilter>();
    options.OperationFilter<AcceptLanguageOperationFilter>();

    foreach (var xml in new[] { Assembly.GetExecutingAssembly().GetName().Name, typeof(Banking.Application.DependencyInjection).Assembly.GetName().Name })
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"{xml}.xml");
        if (File.Exists(path))
        {
            options.IncludeXmlComments(path, includeControllerXmlComments: true);
        }
    }
});

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddHealthChecks();

// Fixed-window limits: login/registration per IP address, money operations per customer (see appsettings.json).
builder.Services.AddBankingRateLimiting(builder.Configuration);

var app = builder.Build();

// The correlation id comes first so every log line, header and problem response of the request carries it.
app.UseCorrelationId();

// Localization runs before the exception handler so the culture it picks is still the current one
// when the handler translates an exception thrown further down the pipeline.
app.UseRequestLocalization();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// After authentication, so the money-operations policy can partition by customer.
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

// Create/upgrade the SQLite schema on start; sample data only when "Database:SeedSampleData" is true.
await app.Services.InitializeDatabaseAsync(seedSampleData: app.Configuration.GetValue<bool>("Database:SeedSampleData"));

await app.RunAsync();

/// <summary>Entry point marker so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program
{
}
