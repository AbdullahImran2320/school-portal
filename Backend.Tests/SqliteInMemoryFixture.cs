// SqliteInMemoryFixture.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;

namespace Backend.Tests
{
    // EF Core's InMemory provider doesn't support transactions at all — and
    // PromotionService wraps its whole run in one (so a mid-run failure
    // doesn't half-promote a class). A ":memory:" SQLite connection gives us
    // a real relational database, transactions included, without touching
    // disk or requiring the actual migration history — EnsureCreated()
    // builds the schema straight from the current model instead.
    //
    // The connection must stay open for the whole test: a ":memory:"
    // database is scoped to its connection and vanishes the instant that
    // connection closes, which IDisposable here handles automatically.
    public class SqliteInMemoryFixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        public SchoolPortalDbContext Context { get; }

        public SqliteInMemoryFixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SchoolPortalDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new SchoolPortalDbContext(options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
