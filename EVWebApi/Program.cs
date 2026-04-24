using EVWebApi.BackgroundServices;
using EVWebApi.Data;
using EVWebApi.DTOs;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Interfaces.Services.MetaDataReaders;
using EVWebApi.Mapping;
using EVWebApi.Middleware;
using EVWebApi.Models;
using EVWebApi.Repositories;
using EVWebApi.Services;
using EVWebApi.Services.MetadataReaders;
using EVWebApi.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Prometheus;
using Serilog;
using Serilog.Formatting.Json;
using Syncfusion.Licensing;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc.Filters;
using EVWebApi.Helpers.Security.Filters;

var builder = WebApplication.CreateBuilder(args);

SyncfusionLicenseProvider.RegisterLicense(
    builder.Configuration["Syncfusion:LicenseKey"]
);
// 1. Add DB Context (PostgreSQL)
var connString = builder.Configuration.GetConnectionString("DefaultConnection");

// enabling to map dictionary to postgresql jsonb

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddSingleton(dataSource);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));


builder.Configuration.AddJsonFile("AuditMessages.json", optional: false, reloadOnChange: true);


// 2. Add AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// 3. Add Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IMfaRepository, MfaRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IMetadataRepository, MetadataRepository>();
builder.Services.AddScoped<ICabinetRepository, CabinetRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IDocVersionRepository, DocVersionRepository>();
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();

// 4. Add Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 5. Add Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICabinetService, CabinetService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentGroupingService, DocumentGroupingService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISecurityAdminService, SecurityAdminService>();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserAuthenticatorRepository, UserAuthenticatorRepository>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<MfaSettings>(builder.Configuration.GetSection("Mfa"));

builder.Services.AddScoped<IMetadataReaderService, CsvMetadataReaderService>();
builder.Services.AddScoped<IMetadataReaderService, ExcelMetadataReaderService>();
builder.Services.AddScoped<IMetadataReaderService, XmlMetadataReaderService>();
builder.Services.AddScoped<IMetadataReaderService, TxtMetadataReaderService>();
builder.Services.AddScoped<IMetadataReaderFactoryService, MetadataReaderFactoryService>();

builder.Services.AddScoped<ISecurityFailureService, SecurityFailureService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddHostedService<StorageMonitorService>();
builder.Services.AddScoped<IStorageQuotaService, StorageQuotaService>();
builder.Services.AddScoped<BuildCookieOptionHelper>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICloudFareTurnstileService, CloudFareTurnstileService>();
builder.Services.AddScoped<IDocVersionService, DocVersionService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

builder.Services.AddMemoryCache();

builder.Services.Configure<AllowedIpSettings>(
    builder.Configuration.GetSection("Allowed"));



builder.Services.Configure<StorageAlertEmailSettings>(
    builder.Configuration.GetSection("StorageAlertEmail"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var publicKeyPath = builder.Configuration["Jwt:PublicKeyPath"];
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(publicKeyPath));

        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,

            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT FAILED: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },

            OnChallenge =async context =>
            {
                Log.Warning("JWT CHALLENGE: Unauthorized request. Error: {Error}, Desc: {Desc}",
                    context.Error,
                    context.ErrorDescription);

                context.HandleResponse();
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync($"Token invalid. {context.ErrorDescription}");
            },
          
            OnMessageReceived = context =>
            {
                // Try header first 
                var token = context.Request.Headers["Authorization"]
                    .FirstOrDefault()?.Split(" ").Last();

                // If no header, try cookie
                if (string.IsNullOrEmpty(token))
                {
                    token = context.Request.Cookies["access_token"];
                }

                context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// 6. Add Controllers
//builder.Services.AddControllers();

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ApiActionLoggingFilter>();
        options.Filters.Add<XssSanitizationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(
                null, // keep original names
                false // do not allow integers
            )
        );
    }); 

// 7. Enable CORS (allow frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCors", policy =>
    {
    policy.SetIsOriginAllowed(origin =>
    {
        if (string.IsNullOrWhiteSpace(origin))
            return false;

        if (origin.StartsWith("http://localhost"))
            return true;

        var allowed = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        return allowed.Any(o =>
                string.Equals(o, origin, StringComparison.OrdinalIgnoreCase));
    })
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials();

    });
});

// 8. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DMS API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200 MB
});



builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365); // 1 year
    options.IncludeSubDomains = true;
    options.Preload = true;
});
// Rate Limiting for refresh endpoint

builder.Services.AddScoped<RefreshRateLimitPolicy>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy<string, RefreshRateLimitPolicy>("RefreshPolicy");
});


//serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(
        new JsonFormatter(),
        "logs/log-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 15)//on 16th day, the first log file will be deleted
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// swagger conditional enable
if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

//serilog

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        var userId = httpContext.User?.FindFirst("userId")?.Value;
        var username = httpContext.User?.FindFirst("username")?.Value;
        var role = httpContext.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(userId))
            diagnosticContext.Set("UserId", userId);

        if (!string.IsNullOrEmpty(username))
            diagnosticContext.Set("Username", username);

        if (!string.IsNullOrEmpty(role))
            diagnosticContext.Set("Role", role);

    };
});

app.UseHttpMetrics();

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseCors("RestrictedCors");

//middlewares

app.UseMiddleware<BotDetectionMiddleware>();

app.UseMiddleware<SecurityFailureTrackingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseAuthentication();
app.UseMiddleware<MetricsAuthorizationMiddleware>();
app.UseMiddleware<ClientValidationMiddleware>();
app.UseMiddleware<SessionValidationMiddleware>();
app.UseAuthorization();


app.MapMetrics();

app.MapControllers();

app.Run();
