// Data/SqlitePragmaInterceptor.cs
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SchoolPortal.API.Data
{
    // SQLite's default journal mode locks the whole database file for the
    // brief moment a write happens, and Microsoft.Data.Sqlite's default busy
    // timeout is effectively 0 — so if two requests try to write at almost
    // the same instant (e.g. two staff recording payments on two different
    // devices at once, now that the portal is reachable over the network),
    // the second one used to fail immediately with a raw "database is
    // locked" error instead of just waiting a moment for its turn.
    //
    // WAL (write-ahead logging) lets reads and a write happen without
    // blocking each other, and a busy timeout makes a write that does need
    // to wait simply wait (up to the timeout) instead of failing outright.
    // journal_mode is stored in the database file itself once set, but
    // busy_timeout is a per-connection setting, so both are (re)applied
    // every time a new connection to the database is opened.
    public class SqlitePragmaInterceptor : DbConnectionInterceptor
    {
        private const string PragmaSql = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            using var command = connection.CreateCommand();
            command.CommandText = PragmaSql;
            command.ExecuteNonQuery();
        }

        public override async Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = PragmaSql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
