using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence;

/// <summary>Helpers to recognise provider-specific failures and translate them into domain exceptions.</summary>
internal static class SqliteErrors
{
    // https://www.sqlite.org/rescode.html#constraint_unique
    private const int SqliteConstraintUnique = 2067;

    // https://www.sqlite.org/rescode.html#constraint_primarykey (a duplicate of a non-rowid primary key)
    private const int SqliteConstraintPrimaryKey = 1555;

    /// <summary>A duplicate in a unique index or in a composite/natural primary key.</summary>
    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
        => exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique or SqliteConstraintPrimaryKey };
}
