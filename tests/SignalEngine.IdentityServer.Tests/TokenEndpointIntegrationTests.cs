using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace SignalEngine.IdentityServer.Tests;

/// <summary>
/// Integration tests for the OAuth 2.0 Token Endpoint.
/// These tests require the IdentityServer to be running at https://localhost:7220
/// Run the IdentityServer first: dotnet run --project src/SignalEngine.IdentityServer
/// Then run these tests with: dotnet test --filter "FullyQualifiedName~TokenEndpointIntegrationTests"
/// </summary>
[Trait("Category", "Integration")]
public class TokenEndpointIntegrationTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5041";
    
    // Test credentials matching DataSeeder
    private const string TestUsername = "admin@signalengine.local";
    private const string TestPassword = "P@ssword123";
    private const string RopcClientId = "signalengine-ropc";
    private const string RopcClientSecret = "RopcSecret123!";

    public TokenEndpointIntegrationTests()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    [Fact]
    public async Task TokenEndpoint_WithValidROPCCredentials_ReturnsAccessToken()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret,
            ["username"] = TestUsername,
            ["password"] = TestPassword,
            ["scope"] = "openid profile system-api"
        });

        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.TokenType.Should().Be("Bearer");
        tokenResponse.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TokenEndpoint_WithOfflineAccessScope_ReturnsRefreshToken()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret,
            ["username"] = TestUsername,
            ["password"] = TestPassword,
            ["scope"] = "openid profile system-api offline_access"
        });

        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

        tokenResponse.Should().NotBeNull();
        tokenResponse!.RefreshToken.Should().NotBeNullOrEmpty("offline_access scope should return a refresh token");
    }

    [Fact]
    public async Task TokenEndpoint_WithOpenIdScope_ReturnsIdToken()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret,
            ["username"] = TestUsername,
            ["password"] = TestPassword,
            ["scope"] = "openid profile"
        });

        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

        tokenResponse.Should().NotBeNull();
        tokenResponse!.IdToken.Should().NotBeNullOrEmpty("openid scope should return an id_token");
    }

    [Fact]
    public async Task TokenEndpoint_RefreshToken_ReturnsNewAccessToken()
    {
        // First, get a refresh token
        var initialRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret,
            ["username"] = TestUsername,
            ["password"] = TestPassword,
            ["scope"] = "openid profile system-api offline_access"
        });

        var initialResponse = await _client.PostAsync("/connect/token", initialRequest);
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var initialContent = await initialResponse.Content.ReadAsStringAsync();
        var initialTokenResponse = JsonSerializer.Deserialize<TokenResponse>(initialContent);
        initialTokenResponse!.RefreshToken.Should().NotBeNullOrEmpty();

        // Now use the refresh token to get a new access token
        var refreshRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret,
            ["refresh_token"] = initialTokenResponse.RefreshToken!
        });

        // Act
        var refreshResponse = await _client.PostAsync("/connect/token", refreshRequest);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        var newTokenResponse = JsonSerializer.Deserialize<TokenResponse>(refreshContent);

        newTokenResponse.Should().NotBeNull();
        newTokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        newTokenResponse.RefreshToken.Should().NotBeNullOrEmpty("refresh token flow should return a new refresh token");
    }

    [Fact]
    public async Task TokenEndpoint_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret,
            ["username"] = TestUsername,
            ["password"] = "WrongPassword123!",
            ["scope"] = "openid profile system-api"
        });

        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("invalid_grant");
    }

    [Fact]
    public async Task TokenEndpoint_WithInvalidUsername_ReturnsUnauthorized()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret,
            ["username"] = "nonexistent@signalengine.local",
            ["password"] = TestPassword,
            ["scope"] = "openid profile system-api"
        });

        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("invalid_grant");
    }

    [Fact]
    public async Task TokenEndpoint_WithInvalidClientSecret_ReturnsUnauthorized()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = "WrongSecret123!",
            ["username"] = TestUsername,
            ["password"] = TestPassword,
            ["scope"] = "openid profile system-api"
        });

        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("invalid_client");
    }

    [Fact]
    public async Task TokenEndpoint_WithInvalidClientId_ReturnsUnauthorized()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "nonexistent-client",
            ["client_secret"] = RopcClientSecret,
            ["username"] = TestUsername,
            ["password"] = TestPassword,
            ["scope"] = "openid profile system-api"
        });

        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("invalid_client");
    }

    [Fact]
    public async Task TokenEndpoint_WithInvalidRefreshToken_ReturnsError()
    {
        // Arrange
        var refreshRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret,
            ["refresh_token"] = "invalid_refresh_token"
        });

        // Act
        var response = await _client.PostAsync("/connect/token", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("invalid_grant");
    }

    [Fact]
    public async Task TokenEndpoint_WithUnsupportedGrantType_ReturnsError()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "unsupported_grant",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret
        });

        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("unsupported_grant_type");
    }

    [Fact]
    public async Task TokenEndpoint_AccessTokenExpiresIn_IsReasonable()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = RopcClientSecret,
            ["username"] = TestUsername,
            ["password"] = TestPassword,
            ["scope"] = "openid profile system-api"
        });

        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

        tokenResponse.Should().NotBeNull();
        // Access token should expire in at least 5 minutes but no more than 24 hours
        tokenResponse!.ExpiresIn.Should().BeGreaterThanOrEqualTo(300);
        tokenResponse.ExpiresIn.Should().BeLessThanOrEqualTo(86400);
    }

    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}
