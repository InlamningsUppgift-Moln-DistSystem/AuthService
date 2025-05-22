using AuthService.Configuration;
using AuthService.Data;
using AuthService.Helpers;
using AuthService.Repositories;
using AuthService.Services;
using AuthService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

// 1. Lägg till Key Vault
string keyVaultUrl = builder.Configuration["KeyVaultUrl"];
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

// 2. Läs ut JwtSettings
var jwtSecret = builder.Configuration["Jwt-Secret"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];

Console.WriteLine($"Jwt-Secret: {(string.IsNullOrEmpty(jwtSecret) ? "NULL" : "LOADED")}");
Console.WriteLine($"Jwt-Issuer: {jwtIssuer}");
Console.WriteLine($"Jwt-Audience: {jwtAudience}");

// 3. Setup JwtSettings
var jwtSettings = new JwtSettings
{
    Secret = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience
};

builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSettings.Secret;
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
});

// 4. CORS – tillåt frontend från Azure Static Web App
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://jolly-river-05ee55f03.6.azurestaticapps.net",
            "http://localhost:5174" // 👈 för lokal utveckling
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});


// 5. DbContext
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration["DefaultConnection"]));

// 6. Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// 7. JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };
});

// 8. Dependency Injection
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();
builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();

// 9. Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });
});

var app = builder.Build();

// 10. Middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseCors("AllowFrontend"); // 🟢 AKTIVERA CORS här före authentication

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
