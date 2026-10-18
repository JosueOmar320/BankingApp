using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Banking.Infrastructure.Persistence.Configurations;

/// <summary>
/// SQLite stores timestamps as text without a time-zone marker, so values come back with
/// <see cref="DateTimeKind.Unspecified"/>. Every timestamp in this system is UTC; this converter
/// restores that fact on read so JSON responses carry the trailing "Z".
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
