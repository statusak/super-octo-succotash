using System.Reflection;
using System.Text;
using CSCourse.Contracts.Models;
using Events.Service.Application;
using Events.Service.Infrastructure;
using Events.Service.Infrastructure.Config;
using Events.Service.Infrastructure.DataAccess;
using Events.Service.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var bootstrapServers = builder.Configuration.GetConnectionString("BootstrapServers")
    ?? throw new InvalidOperationException("Connection string 'BootstrapServers' not found.");

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

var otlpEndpoint = builder.Configuration.GetValue<string>("Otlp:Endpoint");

if (string.IsNullOrWhiteSpace(otlpEndpoint))
    throw new InvalidOperationException("Configuration 'Otlp:Endpoint' is missing or empty");

Uri otlpUri;
try
{
    otlpUri = new Uri(otlpEndpoint);
}
catch (UriFormatException ex)
{
    throw new InvalidOperationException($"Invalid Otlp:Endpoint value '{otlpEndpoint}'. It must be a valid URI.", ex);
}

const string serviceName = "events-service-api";
const string serviceVersion = "1.0.0";

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

        if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.Secret))
        {
            throw new InvalidOperationException("JwtSettings are not configured.");
        }

        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)),

            ClockSkew = TimeSpan.Zero,

            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddInfrastructure(connectionString, bootstrapServers);
builder.Services.AddApplication();

builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Description = @"Введите JWT токен авторизации.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services
    .AddOpenApi()
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: serviceName,
            serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))    
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = httpContext => 
                {
                    var path = httpContext.Request.Path;
                    return !path.StartsWithSegments("/metrics");
                };
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(options =>
            {
                options.Endpoint = otlpUri;
                options.Protocol = OtlpExportProtocol.Grpc;
                options.BatchExportProcessorOptions.ScheduledDelayMilliseconds = 10000;
                options.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = 15000;
            }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console(new CompactJsonFormatter()));


// --------- INIT KAFKA --------- //
await KafkaTopicInitializer.EnsureTopicsAsync(bootstrapServers);
// ---------           --------- //
var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}


if (app.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
