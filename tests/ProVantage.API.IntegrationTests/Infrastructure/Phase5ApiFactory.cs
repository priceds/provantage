using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProVantage.API.IntegrationTests.Infrastructure;

public sealed class Phase5ApiFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> _settings;
    private readonly IReadOnlyDictionary<string, string?> _environmentSettings;

    public Phase5ApiFactory(IReadOnlyDictionary<string, string?> settings)
    {
        _settings = settings;
        _environmentSettings = settings.ToDictionary(
            pair => pair.Key.Replace(":", "__", StringComparison.Ordinal),
            pair => pair.Value,
            StringComparer.Ordinal);

        foreach (var setting in _environmentSettings)
        {
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(_settings);
        });
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var setting in _environmentSettings)
        {
            Environment.SetEnvironmentVariable(setting.Key, null);
        }

        base.Dispose(disposing);
    }
}
