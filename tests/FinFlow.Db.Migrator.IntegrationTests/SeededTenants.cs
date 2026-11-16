namespace FinFlow.Db.Migrator.IntegrationTests;

/// <summary>Dois workspaces com dados proprios, mais uma categoria global.</summary>
public sealed record SeededTenants(Guid WorkspaceA, Guid WorkspaceB, Guid AccountA, Guid AccountB, Guid ImportB, short CategoryId)
{
    public static async Task<SeededTenants> CreateAsync(TestDatabase database, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        var category = await SeedCategoryAsync(database, cancellationToken);
        var a = await SeedWorkspaceAsync(database, "A", cancellationToken);
        var b = await SeedWorkspaceAsync(database, "B", cancellationToken);
        return new SeededTenants(a.Workspace, b.Workspace, a.Account, b.Account, b.Import, category);
    }

    private static async Task<(Guid Workspace, Guid Account, Guid Import)> SeedWorkspaceAsync(
        TestDatabase database, string label, CancellationToken cancellationToken)
    {
        var workspace = Guid.NewGuid();
        var account = Guid.NewGuid();
        var import = Guid.NewGuid();

        var session = await DbSession.OpenAsync(database, cancellationToken);
        await using (session.ConfigureAwait(false))
        {
            await session.ExecuteAsync(
                "INSERT INTO workspaces (id, name) VALUES (@id, @name)",
                cancellationToken, ("id", workspace), ("name", "Workspace " + label));
            await session.ExecuteAsync(
                "INSERT INTO accounts (id, workspace_id, bank_id, name) " +
                "VALUES (@id, @ws, (SELECT id FROM banks WHERE code = 'c6'), @name)",
                cancellationToken, ("id", account), ("ws", workspace), ("name", "Conta " + label));
            await session.ExecuteAsync(
                "INSERT INTO statement_imports (id, workspace_id, account_id, bank_id, file_hash, rows_total, rows_inserted, rows_duplicated) " +
                "VALUES (@id, @ws, @acc, (SELECT id FROM banks WHERE code = 'c6'), @hash, 1, 1, 0)",
                cancellationToken, ("id", import), ("ws", workspace), ("acc", account), ("hash", "hash-" + label));
            await session.ExecuteAsync(
                "INSERT INTO transactions (workspace_id, account_id, import_id, occurred_on, description, amount, dedup_key) " +
                "VALUES (@ws, @acc, @imp, DATE '2026-11-01', @desc, 10.50, @key)",
                cancellationToken, ("ws", workspace), ("acc", account), ("imp", import), ("desc", "Compra " + label), ("key", "key-" + label));
            await session.CommitAsync(cancellationToken);
        }

        return (workspace, account, import);
    }

    private static async Task<short> SeedCategoryAsync(TestDatabase database, CancellationToken cancellationToken)
    {
        var session = await DbSession.OpenAsync(database, cancellationToken);
        await using (session.ConfigureAwait(false))
        {
            var id = await session.ScalarAsync<short>(
                "INSERT INTO categories (code, name) VALUES ('alimentacao', 'Alimentacao') RETURNING id", cancellationToken);
            await session.CommitAsync(cancellationToken);
            return id;
        }
    }
}
