using Banking.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Banking.Tests.Infrastructure;

public class MigrationsTests
{
    [Test]
    public void Model_HasNoPendingChanges_ComparedToLastMigration()
    {
        using var database = new SqliteTestDatabase();
        using var context = database.CreateContext();

        Assert.That(context.Database.HasPendingModelChanges(), Is.False,
            "the entity model differs from the migrations snapshot; run 'dotnet ef migrations add <Name>'");
    }
}
