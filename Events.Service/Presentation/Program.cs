using System.Reflection;
using System.Text;
using CSCourse.Contracts.Models;
using Events.Service.Application;
using Events.Service.Infrastructure;
using Events.Service.Infrastructure.DataAccess;
using Events.Service.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var bootstrapServers = builder.Configuration.GetConnectionString("BootstrapServers") 
    ?? throw new InvalidOperationException("Connection string 'BootstrapServers' not found.");

var cacheServers = builder.Configuration.GetConnectionString("CacheServers") 
    ?? throw new InvalidOperationException("Connection string 'CacheServers' not found.");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

        if(jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.Secret))
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
        new OpenApiSecurityScheme {
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
builder.Services.AddOpenApi();


// --------- INIT KAFKA --------- //
await KafkaTopicInitializer.EnsureTopicsAsync(bootstrapServers);
// ---------           --------- //
var app = builder.Build();


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
