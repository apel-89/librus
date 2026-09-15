using Librus.Api.Data;
using Librus.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Librus.Api.Tests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DefaultConnection =
        "Host=localhost;Port=5533;Database=librus_test;Username=librus;Password=librus";

    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("LIBRUS_TEST_DB") ?? DefaultConnection;

public async Task InitializeAsync()
{
    await using var db = CreateContext();
    await db.Database.MigrateAsync();
}

public Task DisposeAsync() => Task.CompletedTask;

    public LibrusDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LibrusDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LibrusDbContext(options);
    }

public async Task ResetAsync()
{
    await using var db = CreateContext();

    var tables = await db.Database
        .SqlQuery<string>($"""
            SELECT tablename
            FROM pg_tables
            WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'
            """)
        .ToListAsync();

    if (tables.Count == 0) return;

    var list = string.Join(", ", tables.Select(t => $"\"{t.Replace("\"", "\"\"")}\""));

#pragma warning disable EF1002
    await db.Database.ExecuteSqlRawAsync(
        $"TRUNCATE {list} RESTART IDENTITY CASCADE");
#pragma warning restore EF1002
}
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "database";
}

public sealed class TestClock(DateTime now) : IClock
{
    public DateTime UtcNow { get; set; } = now;

    public static TestClock At(int year, int month, int day) =>
        new(new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc));

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}