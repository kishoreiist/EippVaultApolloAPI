using EVWebApi.Data;
using EVWebApi.DTOs;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Interfaces.Services.MetaDataReaders;
using EVWebApi.Mapping;
using EVWebApi.Middleware;
using EVWebApi.Models;
using EVWebApi.Repositories;
using EVWebApi.Services;
using EVWebApi.Services.MetadataReaders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Reflection.Metadata;
using System.Text;
using Syncfusion.Licensing;



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



builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// 6. Add Controllers
//builder.Services.AddControllers();

builder.Services.AddControllers()
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

    //"AllowAll", policy =>
    {

    //policy.SetIsOriginAllowed(origin => true)
    //  .AllowAnyMethod()
    //  .AllowAnyHeader()
    //  .AllowCredentials();

    policy.SetIsOriginAllowed(origin =>
    {
        if (origin.StartsWith("http://localhost"))
            return true;

        var allowed = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        return allowed!.Contains(origin);
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
var app = builder.Build();

// Middleware
if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseSwagger();
//app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

app.Run();
