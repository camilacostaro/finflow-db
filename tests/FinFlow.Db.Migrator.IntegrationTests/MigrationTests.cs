namespace FinFlow.Db.Migrator.IntegrationTests;

public sealed class MigrationTests(PostgresFixture postgres)
{
    private static readonly string[] ExpectedTables =
    [
        "accounts", "banks", "categories",
        "statement_imports", "transactions", "users", "workspace_members", "workspaces",
    ];

    [Fact]
    public async Task Run_EmptyDatabase_CreatesAllTables()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = (await TestDatabase.CreateAsync(postgres, ct)).Migrate();

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var tables = await session.ScalarAsync<string[]>(
                "SELECT array_agg(table_name::text ORDER BY table_name) FROM information_schema.tables " +
                "WHERE table_schema = 'public' AND table_name <> 'schemaversions'",
                ct);

            Assert.Equal(ExpectedTables, tables);
        }
    }

    [Fact]
    public async Task Run_SecondTime_SucceedsAndAppliesNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = (await TestDatabase.CreateAsync(postgres, ct)).Migrate();
        var appliedAfterFirstRun = await CountAppliedScriptsAsync(database, ct);

        database.Migrate();

        Assert.Equal(appliedAfterFirstRun, await CountAppliedScriptsAsync(database, ct));
    }

    [Fact]
    public async Task Run_EmptyDatabase_SeedsC6Bank()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = (await TestDatabase.CreateAsync(postgres, ct)).Migrate();

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var name = await session.ScalarAsync<string>("SELECT name FROM banks WHERE code = 'c6'", ct);

            Assert.Equal("C6 Bank", name);
        }
    }

    private static async Task<long> CountAppliedScriptsAsync(TestDatabase database, CancellationToken ct)
    {
        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            return await session.ScalarAsync<long>("SELECT count(*) FROM schemaversions", ct);
        }
    }
}
