using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace FinFlow.Db.Migrator;

/// <summary>Aplica os scripts SQL embutidos no assembly, em ordem de nome, uma transação por script.</summary>
public static class MigrationRunner
{
    public static DatabaseUpgradeResult Run(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        return upgrader.PerformUpgrade();
    }
}
