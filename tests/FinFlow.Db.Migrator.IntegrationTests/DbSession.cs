using Npgsql;

namespace FinFlow.Db.Migrator.IntegrationTests;

/// <summary>Conexao com uma transacao aberta. Descartar sem Commit desfaz tudo.</summary>
public sealed class DbSession : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    private DbSession(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public static async Task<DbSession> OpenAsync(TestDatabase database, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        var transaction = await connection.BeginTransactionAsync(cancellationToken);
        return new DbSession(connection, transaction);
    }

    public async Task<int> ExecuteAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var command = CreateCommand(sql, parameters);
        await using (command.ConfigureAwait(false))
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<T> ScalarAsync<T>(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var command = CreateCommand(sql, parameters);
        await using (command.ConfigureAwait(false))
        {
            return (T)(await command.ExecuteScalarAsync(cancellationToken))!;
        }
    }

    public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private NpgsqlCommand CreateCommand(string sql, (string Name, object? Value)[] parameters)
    {
        var command = new NpgsqlCommand(sql, _connection, _transaction);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }
}
