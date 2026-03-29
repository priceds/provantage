using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ProVantage.API.IntegrationTests.Infrastructure;

namespace ProVantage.API.IntegrationTests.Endpoints;

public class Phase5EndpointsTests : IClassFixture<ContainerizedApiFixture>
{
    private static readonly Guid ApprovedVendorId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private readonly ContainerizedApiFixture _fixture;

    public Phase5EndpointsTests(ContainerizedApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_returns_access_token_for_seeded_admin()
    {
        using var factory = _fixture.CreateFactory();
        using var client = CreateHttpsClient(factory);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@acme.com",
            password = "Admin123!"
        });

        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.False(string.IsNullOrWhiteSpace(payload.RootElement.GetProperty("accessToken").GetString()));
        Assert.Equal("admin@acme.com", payload.RootElement.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Get_contracts_returns_seeded_contracts_for_authenticated_user()
    {
        using var factory = _fixture.CreateFactory();
        using var client = CreateHttpsClient(factory);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginAsAsync(client, "admin@acme.com"));

        var response = await client.GetAsync("/api/contracts?page=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(payload.RootElement.GetProperty("totalCount").GetInt32() >= 5);
        Assert.True(payload.RootElement.GetProperty("items").GetArrayLength() >= 5);
    }

    [Fact]
    public async Task Create_contract_persists_and_can_be_retrieved()
    {
        using var factory = _fixture.CreateFactory();
        using var client = CreateHttpsClient(factory);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginAsAsync(client, "admin@acme.com"));

        var createResponse = await client.PostAsJsonAsync("/api/contracts", new
        {
            vendorId = ApprovedVendorId,
            title = "Security Appliance Support",
            startDate = DateTime.UtcNow.Date.ToString("O"),
            endDate = DateTime.UtcNow.Date.AddDays(45).ToString("O"),
            value = 25000m,
            currency = "usd"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var contractId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var getResponse = await client.GetAsync($"/api/contracts/{contractId}");

        getResponse.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal("Security Appliance Support", payload.RootElement.GetProperty("title").GetString());
        Assert.Equal("USD", payload.RootElement.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task Get_audit_logs_returns_data_for_admin()
    {
        using var factory = _fixture.CreateFactory();
        using var client = CreateHttpsClient(factory);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginAsAsync(client, "admin@acme.com"));

        var response = await client.GetAsync("/api/audit-logs?page=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(payload.RootElement.GetProperty("items").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Get_audit_logs_returns_forbidden_for_non_admin_user()
    {
        using var factory = _fixture.CreateFactory();
        using var client = CreateHttpsClient(factory);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginAsAsync(client, "buyer@acme.com"));

        var response = await client.GetAsync("/api/audit-logs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpClient CreateHttpsClient(Phase5ApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    private static async Task<string> LoginAsAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Admin123!"
        });

        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token was not returned.");
    }
}
