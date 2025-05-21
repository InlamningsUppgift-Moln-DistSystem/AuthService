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

// 1. L�gg till Key Vault som configk�lla tidigt
string keyVaultUrl = builder.Configuration["KeyVaultUrl"];
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

// 2. Testa att l�sa ut hemligheter direkt efter Key Vault laddats in
var jwtSecret = builder.Configuration["Jwt-Secret"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];

Console.WriteLine($"Jwt-Secret from config: {jwtSecret ?? "NULL or EMPTY"}");
Console.WriteLine($"Jwt-Issuer from config: {jwtIssuer ?? "NULL or EMPTY"}");
Console.WriteLine($"Jwt-Audience from config: {jwtAudience ?? "NULL or EMPTY"}");

// 3. Konfigurera JwtSettings-objektet explicit
var jwtSettings = new JwtSettings
{
    Secret = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience
};

// 4. L�gg in i DI-container s� andra kan anv�nda IOptions<JwtSettings>
builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSettings.Secret;
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
});

// 5. L�gg till DbContext med connection string fr�n Key Vault
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration["DefaultConnection"]));

// 6. L�gg till Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// 7. L�gg till JWT-autentisering med explicit jwtSettings
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

// 8. L�gg till �vriga tj�nster
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();

// 9. L�gg till controllers
builder.Services.AddControllers();

// 10. Swagger setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });
});

var app = builder.Build();

// 11. Swagger aktiverat i alla milj�er (f�r test)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1");
    c.RoutePrefix = "swagger"; // s� att det ligger p� /swagger
});


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
