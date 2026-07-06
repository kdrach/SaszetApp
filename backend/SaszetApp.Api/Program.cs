using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = $"{builder.Configuration["Jwt:Authority"]}/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false; // Internal Docker network
        options.Audience = "saszetapp-api"; // Custom Keycloak audience
        
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[] { builder.Configuration["Jwt:ValidIssuer"] ?? builder.Configuration["Jwt:Authority"] },
            ValidateAudience = true,
            ValidAudiences = new[] { "saszetapp-api" },
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
        
        options.Events = new JwtBearerEvents
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
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
