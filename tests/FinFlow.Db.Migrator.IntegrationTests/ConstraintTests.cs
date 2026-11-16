using Npgsql;

namespace FinFlow.Db.Migrator.IntegrationTests;

public sealed class ConstraintTests(PostgresFixture postgres)
{
    private const string UniqueViolation = "23505";
    private const string ForeignKeyViolation = "23503";
    private const string CheckViolation = "23514";

    [Fact]
    public async Task Account_UnknownBank_ViolatesForeignKey()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, tenants) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var ex = await Assert.ThrowsAsync<PostgresException>(() => session.ExecuteAsync(
                "INSERT INTO accounts (workspace_id, bank_id, name) VALUES (@ws, 32000, 'Sem banco')",
                ct, ("ws", tenants.WorkspaceA)));

            Assert.Equal(ForeignKeyViolation, ex.SqlState);
        }
    }

    [Fact]
    public async Task StatementImport_SameFileHashInSameWorkspace_ViolatesUnique()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, tenants) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var ex = await Assert.ThrowsAsync<PostgresException>(() => session.ExecuteAsync(
                "INSERT INTO statement_imports (workspace_id, account_id, bank_id, file_hash, rows_total, rows_inserted, rows_duplicated) " +
                "VALUES (@ws, @acc, (SELECT id FROM banks WHERE code = 'c6'), 'hash-A', 1, 1, 0)",
                ct, ("ws", tenants.WorkspaceA), ("acc", tenants.AccountA)));

            Assert.Equal(UniqueViolation, ex.SqlState);
        }
    }

    [Fact]
    public async Task StatementImport_SameFileHashInAnotherWorkspace_IsAllowed()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, tenants) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var inserted = await session.ExecuteAsync(
                "INSERT INTO statement_imports (workspace_id, account_id, bank_id, file_hash, rows_total, rows_inserted, rows_duplicated) " +
                "VALUES (@ws, @acc, (SELECT id FROM banks WHERE code = 'c6'), 'hash-A', 1, 1, 0)",
                ct, ("ws", tenants.WorkspaceB), ("acc", tenants.AccountB));

            Assert.Equal(1, inserted);
        }
    }

    [Fact]
    public async Task Transaction_SameDedupKeyInSameAccount_ViolatesUnique()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, tenants) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var ex = await Assert.ThrowsAsync<PostgresException>(() => session.ExecuteAsync(
                "INSERT INTO transactions (workspace_id, account_id, occurred_on, description, amount, dedup_key) " +
                "VALUES (@ws, @acc, DATE '2026-11-02', 'Duplicada', 1.00, 'key-A')",
                ct, ("ws", tenants.WorkspaceA), ("acc", tenants.AccountA)));

            Assert.Equal(UniqueViolation, ex.SqlState);
        }
    }

    [Fact]
    public async Task Transaction_AccountFromAnotherWorkspace_ViolatesForeignKey()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, tenants) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var ex = await Assert.ThrowsAsync<PostgresException>(() => session.ExecuteAsync(
                "INSERT INTO transactions (workspace_id, account_id, occurred_on, description, amount, dedup_key) " +
                "VALUES (@ws, @acc, DATE '2026-11-02', 'Conta alheia', 1.00, 'key-x')",
                ct, ("ws", tenants.WorkspaceA), ("acc", tenants.AccountB)));

            Assert.Equal(ForeignKeyViolation, ex.SqlState);
        }
    }

    [Fact]
    public async Task Transaction_ImportFromAnotherWorkspace_ViolatesForeignKey()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, tenants) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var ex = await Assert.ThrowsAsync<PostgresException>(() => session.ExecuteAsync(
                "INSERT INTO transactions (workspace_id, account_id, import_id, occurred_on, description, amount, dedup_key) " +
                "VALUES (@ws, @acc, @imp, DATE '2026-11-02', 'Import alheio', 1.00, 'key-y')",
                ct, ("ws", tenants.WorkspaceA), ("acc", tenants.AccountA), ("imp", tenants.ImportB)));

            Assert.Equal(ForeignKeyViolation, ex.SqlState);
        }
    }

    [Fact]
    public async Task Transaction_Category_IsOptionalAndEditable()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, tenants) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var initial = await session.ScalarAsync<bool>(
                "SELECT category_id IS NULL FROM transactions WHERE dedup_key = 'key-A'", ct);
            await session.ExecuteAsync(
                "UPDATE transactions SET category_id = @cat WHERE dedup_key = 'key-A'", ct, ("cat", tenants.CategoryId));
            var assigned = await session.ScalarAsync<short>(
                "SELECT category_id FROM transactions WHERE dedup_key = 'key-A'", ct);
            await session.ExecuteAsync(
                "UPDATE transactions SET category_id = NULL WHERE dedup_key = 'key-A'", ct);
            var cleared = await session.ScalarAsync<bool>(
                "SELECT category_id IS NULL FROM transactions WHERE dedup_key = 'key-A'", ct);

            Assert.Equal((true, tenants.CategoryId, true), (initial, assigned, cleared));
        }
    }

    [Fact]
    public async Task Transaction_UnknownCategory_ViolatesForeignKey()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, _) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var ex = await Assert.ThrowsAsync<PostgresException>(() => session.ExecuteAsync(
                "UPDATE transactions SET category_id = 32000 WHERE dedup_key = 'key-A'", ct));

            Assert.Equal(ForeignKeyViolation, ex.SqlState);
        }
    }

    [Fact]
    public async Task Transaction_Amount_IsNumeric19Scale2()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, _) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var type = await session.ScalarAsync<string>(
                "SELECT data_type || '(' || numeric_precision || ',' || numeric_scale || ')' " +
                "FROM information_schema.columns WHERE table_name = 'transactions' AND column_name = 'amount'",
                ct);

            Assert.Equal("numeric(19,2)", type);
        }
    }

    [Fact]
    public async Task User_DuplicateAuthSubject_ViolatesUnique()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = (await TestDatabase.CreateAsync(postgres, ct)).Migrate();

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            await session.ExecuteAsync("INSERT INTO users (auth_subject, email) VALUES ('sub-1', 'a@example.com')", ct);

            var ex = await Assert.ThrowsAsync<PostgresException>(() => session.ExecuteAsync(
                "INSERT INTO users (auth_subject, email) VALUES ('sub-1', 'b@example.com')", ct));

            Assert.Equal(UniqueViolation, ex.SqlState);
        }
    }

    [Fact]
    public async Task WorkspaceMember_UnknownRole_ViolatesCheck()
    {
        var ct = TestContext.Current.CancellationToken;
        var (database, tenants) = await SeedAsync(ct);

        var session = await DbSession.OpenAsync(database, ct);
        await using (session)
        {
            var user = await session.ScalarAsync<Guid>(
                "INSERT INTO users (auth_subject, email) VALUES ('sub-1', 'a@example.com') RETURNING id", ct);

            var ex = await Assert.ThrowsAsync<PostgresException>(() => session.ExecuteAsync(
                "INSERT INTO workspace_members (workspace_id, user_id, role) VALUES (@ws, @user, 'admin')",
                ct, ("ws", tenants.WorkspaceA), ("user", user)));

            Assert.Equal(CheckViolation, ex.SqlState);
        }
    }

    private async Task<(TestDatabase Database, SeededTenants Tenants)> SeedAsync(CancellationToken ct)
    {
        var database = (await TestDatabase.CreateAsync(postgres, ct)).Migrate();
        return (database, await SeededTenants.CreateAsync(database, ct));
    }
}
