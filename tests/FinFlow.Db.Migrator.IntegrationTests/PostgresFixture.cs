using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(FinFlow.Db.Migrator.IntegrationTests.PostgresFixture))]

namespace FinFlow.Db.Migrator.IntegrationTests;

/// <summary>Um Postgres 18 real por execucao de testes; cada teste cria o proprio database.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

    /// <summary>Conexao como superusuario (usada so para criar roles e databases de teste).</summary>
    public string AdminConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
