using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;
using SignalEngine.Application;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Infrastructure;
using SignalEngine.SystemApi.Services;

var builder = WebApplication.CreateBuilder(args);
// Allow CORS for Angular dev server
var allowedOrigins = new[] { "https://localhost:4200" };


// Add Application and Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, includeDataSeeder: false);

// Add HTTP context accessor for current user service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Configure OpenIddict validation
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Configure the validation handler to use introspection
        options.SetIssuer(builder.Configuration["OpenIddict:Issuer"] ?? "https://localhost:7001/");
        
        // Register the System.Net.Http integration
        options.UseSystemNetHttp();
        
        // Register the ASP.NET Core integration
        options.UseAspNetCore();
        
        // Configure the audience
        options.AddAudiences("signalengine-api");
    });

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

// Configure authorization - just require authentication for now
builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers();
// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SignalEngine System API",
        Version = "v1",
        Description = "API for managing signals, rules, metrics, and assets in SignalEngine"
    });

    // Configure OAuth2 security for Swagger
    var issuer = builder.Configuration["OpenIddict:Issuer"] ?? "https://localhost:7001/";
    
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{issuer}connect/authorize"),
                TokenUrl = new Uri($"{issuer}connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID" },
                    { "profile", "Profile" },
                    { "system-api", "System API Access" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "openid", "profile", "system-api" }
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SignalEngine System API v1");
        options.OAuthClientId("signalengine-spa");
        options.OAuthUsePkce();
    });
}

// Use CORS policy
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
