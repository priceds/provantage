using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace ProVantage.API.IntegrationTests.Infrastructure;

public sealed class ContainerizedApiFixture : IAsyncLifetime
{
    private readonly List<string> _sqliteFiles = [];
    private MsSqlContainer? _sqlContainer;
    private RedisContainer? _redisContainer;
    private bool _useDocker;

    public async Task InitializeAsync()
    {
        try
        {
            _sqlContainer = new MsSqlBuilder()
                .WithPassword("Your_strong_Test_Password123!")
                .Build();
            _redisContainer = new RedisBuilder().Build();

            await _sqlContainer.StartAsync();
            await _redisContainer.StartAsync();
            _useDocker = true;
        }
        catch
        {
            _useDocker = false;
        }
    }

    public Phase5ApiFactory CreateFactory()
    {
        if (_useDocker && _sqlContainer is not null && _redisContainer is not null)
        {
            var builder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString())
            {
                InitialCatalog = $"ProVantageTests_{Guid.NewGuid():N}"
            };

            return new Phase5ApiFactory(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = builder.ConnectionString,
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString()
            });
        }

        var sqlitePath = Path.Combine(Path.GetTempPath(), $"provantage-tests-{Guid.NewGuid():N}.db");
        _sqliteFiles.Add(sqlitePath);

        return new Phase5ApiFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={sqlitePath}",
            ["Testing:UseSqlite"] = "true",
            ["Testing:UseInMemoryCache"] = "true",
            ["Testing:DisableBackgroundJobs"] = "true"
        });
    }

    public async Task DisposeAsync()
    {
        if (_sqlContainer is not null)
        {
            await _sqlContainer.DisposeAsync();
        }

        if (_redisContainer is not null)
        {
            await _redisContainer.DisposeAsync();
        }

        foreach (var sqliteFile in _sqliteFiles)
        {
            if (File.Exists(sqliteFile))
            {
                File.Delete(sqliteFile);
            }
        }
    }
}
