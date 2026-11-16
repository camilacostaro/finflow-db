using FinFlow.Db.Migrator;

const string ConnectionStringVariable = "ConnectionStrings__Default";

var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
if (string.IsNullOrWhiteSpace(connectionString))
{
    await Console.Error.WriteLineAsync($"{ConnectionStringVariable} is missing: provide an Npgsql connection string.");
    return 1;
}

var result = MigrationRunner.Run(connectionString);
if (!result.Successful)
{
    await Console.Error.WriteLineAsync($"Migration failed: {result.Error.Message}");
    return 2;
}

Console.WriteLine("Migrations applied successfully.");
return 0;
