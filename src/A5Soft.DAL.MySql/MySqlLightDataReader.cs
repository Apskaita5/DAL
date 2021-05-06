using System;
using System.Threading.Tasks;
using A5Soft.DAL.Core;
using MySqlConnector;

namespace A5Soft.DAL.MySql
{
    internal class MySqlLightDataReader : LightDataReaderBase
    {
        private readonly MySqlConnection _connection;
        private readonly MySqlCommand _command;

        public MySqlLightDataReader(MySqlDataReader reader, MySqlConnection connection, 
            MySqlCommand command, bool isTransaction)
            : base(reader, isTransaction)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _command = command ?? throw new ArgumentNullException(nameof(command));
        }

        protected override async Task CloseConnectionAsync()
        {
            try { _command.Dispose(); }
            catch (Exception) { }
            await _connection.CloseAndDisposeAsync();
        }

    }
}
