using System;
using FreelanceManager.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Tests;

/// <summary>
/// A disposable SQLite database backed by a shared in-memory connection.
/// FK enforcement is on, so DeleteBehavior.Restrict is actually tested.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public DbContextOptions<AppDbContext> Options { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Cache=Shared");
        _connection.Open();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = NewContext();
        ctx.Database.EnsureCreated();
    }

    public AppDbContext NewContext() => new(Options);

    public void Dispose() => _connection.Dispose();
}
