using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Read allowed CORS origins from configuration (Cors:AllowedOrigins)
// In production, set via env vars: Cors__AllowedOrigins__0=https://saszet.app etc.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[] { "http://localhost:3010", "http://localhost:3011" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<SaszetApp.Api.Services.Mappers.IPetFoodModelMapper, SaszetApp.Api.Services.Mappers.PetFoodModelMapper>();
builder.Services.AddScoped<SaszetApp.Api.Services.Mappers.ILlmProviderModelMapper, SaszetApp.Api.Services.Mappers.LlmProviderModelMapper>();
builder.Services.AddSingleton<SaszetApp.Api.Services.IEncryptionService, SaszetApp.Api.Services.EncryptionService>();
builder.Services.AddScoped<SaszetApp.Api.Services.IVlmService, SaszetApp.Api.Services.VlmService>();
builder.Services.AddHttpClient(); // Add HttpClient factory for VLM service
builder.Services.AddHealthChecks();

var jwtEvents = new JwtBearerEvents
{
    OnTokenValidated = context =>
    {
        if (context.Principal?.Identity is ClaimsIdentity identity)
        {
            var realmAccessClaim = identity.FindFirst("realm_access");
            if (realmAccessClaim != null)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);
                    if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                    {
                        foreach (var role in rolesElement.EnumerateArray())
                        {
                            var roleStr = role.GetString();
                            if (!string.IsNullOrEmpty(roleStr))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, roleStr));
                            }
                        }
                    }
                }
                catch { /* Ignore parsing errors */ }
            }
        }
        return Task.CompletedTask;
    }
};

builder.Services.AddAuthentication()
.AddJwtBearer("AdminAuth", options =>
{
    options.MetadataAddress = $"{builder.Configuration["Jwt:AdminAuthority"]}/.well-known/openid-configuration";
    options.RequireHttpsMetadata = false; 
    
    options.IncludeErrorDetails = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuers = new[] { builder.Configuration["Jwt:ValidIssuerAdmin"] ?? builder.Configuration["Jwt:AdminAuthority"] },
        ValidateAudience = true,
        ValidAudiences = new[] { "account", "saszetapp-admin" },
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
    
    options.Events = jwtEvents;
})
.AddJwtBearer("CustomerAuth", options =>
{
    options.MetadataAddress = $"{builder.Configuration["Jwt:CustomerAuthority"]}/.well-known/openid-configuration";
    options.RequireHttpsMetadata = false; 
    
    options.IncludeErrorDetails = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuers = new[] { builder.Configuration["Jwt:ValidIssuerCustomer"] ?? builder.Configuration["Jwt:CustomerAuthority"] },
        ValidateAudience = true,
        ValidAudiences = new[] { "account", "saszetapp-pwa" },
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
    
    options.Events = jwtEvents;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("AdminAuth");
        policy.RequireRole("admin");
    });
    options.AddPolicy("CustomerPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("CustomerAuth");
        policy.RequireAuthenticatedUser();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
