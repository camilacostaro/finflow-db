using Npgsql;

namespace FinFlow.Db.Migrator.IntegrationTests;

/// <summary>
/// Database novo e vazio dentro do container compartilhado, acessado com o usuario administrador
/// (superusuario), como a aplicacao conecta no banco real.
/// </summary>
public sealed class TestDatabase
{
    private TestDatabase(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>Connection string do database de teste, sem pool (cada conexao tem sessao propria).</summary>
    public string ConnectionString { get; }

    public static async Task<TestDatabase> CreateAsync(PostgresFixture postgres, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(postgres);

        // Nome gerado de Guid hexadecimal: sem entrada externa no comando DDL.
        var database = "db_" + Guid.NewGuid().ToString("N")[..12];

        var admin = new NpgsqlConnection(postgres.AdminConnectionString);
        await using (admin.ConfigureAwait(false))
        {
            await admin.OpenAsync(cancellationToken);
            var command = new NpgsqlCommand($"CREATE DATABASE {database}", admin);
            await using (command.ConfigureAwait(false))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        var builder = new NpgsqlConnectionStringBuilder(postgres.AdminConnectionString)
        {
            Database = database,
            Pooling = false,
        };
        return new TestDatabase(builder.ConnectionString);
    }

    /// <summary>Aplica as migrations reais.</summary>
    public TestDatabase Migrate()
    {
        var result = MigrationRunner.Run(ConnectionString);
        Assert.True(result.Successful, result.Error?.ToString());
        return this;
    }
}
