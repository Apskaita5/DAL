using System;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using A5Soft.DAL.Core;

namespace A5Soft.DAL.SQLite
{
    internal class SQLiteLightDataReader : LightDataReaderBase
    {
        private readonly SQLiteConnection _connection;
        private readonly SQLiteCommand _command;

        public SQLiteLightDataReader(DbDataReader reader, SQLiteConnection connection,
            SQLiteCommand command, bool isTransaction)
            : base(reader, isTransaction)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _command = command ?? throw new ArgumentNullException(nameof(command));
        }

        protected override Task CloseConnectionAsync()
        {
            try { _command.Dispose(); }
            catch (Exception) { }
            _connection.CloseAndDispose();
            return Task.CompletedTask;
        }
    }
}
