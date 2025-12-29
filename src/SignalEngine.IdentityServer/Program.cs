using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.IdentityServer.Services;
using SignalEngine.Infrastructure;
using SignalEngine.Infrastructure.Identity;
using SignalEngine.Infrastructure.Persistence;
using SignalEngine.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register system-level current user service for IdentityServer
// This returns null TenantId, which disables tenant filtering for data seeding
builder.Services.AddScoped<ICurrentUserService, SystemCurrentUserService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("https://localhost:4200", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        // Enable the authorization, token, end-session, and userinfo endpoints
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetEndSessionEndpointUris("/connect/endsession")
               .SetUserInfoEndpointUris("/connect/userinfo");

        // Enable the authorization code flow with PKCE
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        // Enable the client credentials flow
        options.AllowClientCredentialsFlow();

        // Enable the resource owner password credentials flow
        options.AllowPasswordFlow();

        // Enable refresh tokens
        options.AllowRefreshTokenFlow();

        // Register scopes
        options.RegisterScopes(
            "openid",
            "profile",
            "email",
            "roles",
            "system-api",
            "signal.read",
            "signal.write",
            "rule.read",
            "rule.write");

        // Use development encryption and signing certificates
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Let resource servers validate JWTs without introspection.
        options.DisableAccessTokenEncryption();

        // Disable HTTPS requirement for development
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableStatusCodePagesIntegration();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Add controllers and views
builder.Services.AddControllersWithViews();

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Identity.Application";
    options.DefaultChallengeScheme = "Identity.Application";
});

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
        }
        else if (app.Environment.EnvironmentName == "Testing")
        {
            // For testing with in-memory database, use EnsureCreated instead of Migrate
            logger.LogInformation("Creating test database schema...");
            await context.Database.EnsureCreatedAsync();
        }

        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();

// Map OpenIddict and MVC endpoints
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
