using A5Soft.DAL.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using A5Soft.DAL.Core.DbSchema;
using A5Soft.DAL.Core.MicroOrm;
using MySqlConnector;

namespace A5Soft.DAL.MySql
{
    /// <summary>
    /// Represents an abstraction over the native MySql data access and schema methods.
    /// </summary>
    /// <remarks>Should be stored in ApplicationContext.Local context (in thread for client,
    /// in http context on server).</remarks>
    public class MySqlAgent : SqlAgentBase
    {
        #region Constants

        private const string AgentName = "MySql connector";
        private const bool AgentIsFileBased = false;
        private const string AgentRootName = "root";
        private const string AgentWildcart = "%";

        #endregion

        #region Properties

        /// <inheritdoc cref="ISqlAgent.IsTransactionInProgress"/>
        public override bool IsTransactionInProgress => CurrentTransaction != null;

        /// <summary>
        /// Gets a name of the SQL implementation behind the SqlAgent, i. e. MySql connector.
        /// </summary>
        public override string Name => AgentName;

        /// <summary>
        /// Gets an id of the concrete SQL implementation, i.e. mysql.
        /// The id is used to select an appropriate SQL token dictionary.
        /// </summary>
        public override string SqlImplementationId => Extensions.MySqlImplementationId;

        /// <summary>
        /// Gets a value indicating whether the SQL engine is file based, i.e. false.
        /// </summary>
        public override bool IsFileBased => AgentIsFileBased;

        /// <summary>
        /// Gets a name of the root user as defined in the SQL implementation behind the SqlAgent, i.e. root.
        /// </summary>
        public override string RootName => AgentRootName;

        /// <summary>
        /// Gets a symbol used as a wildcard for the SQL implementation behind the SqlAgent, i.e. %.
        /// </summary>
        public override string Wildcart => AgentWildcart;

        #endregion

        #region Costructors

        /// <summary>
        /// Initializes a new MySqlAgent instance.
        /// </summary>
        /// <param name="baseConnectionString">a connection string to use to connect to
        /// a database (should not include database parameter that is added by the
        /// SqlAgent implementation depending on the database chosen, should include password
        /// (if any), password replacement (if needed) should be handled by the user class)</param>
        /// <param name="databaseName">a name of the database to use (if any)</param>
        /// <param name="sqlDictionary">an implementation of SQL dictionary to use (if any)</param>
        public MySqlAgent(string baseConnectionString, string databaseName, ISqlDictionary sqlDictionary)
            : base(baseConnectionString, false, databaseName, sqlDictionary) { }

        /// <summary>
        /// Clones a MySqlAgent instance.
        /// </summary>
        /// <param name="agentToClone">a MySqlAgent instance to clone</param>
        private MySqlAgent(MySqlAgent agentToClone) : base(agentToClone) { }

        #endregion

        #region Common ISqlAgent Methods

        /// <inheritdoc cref="ISqlAgent.TestConnectionAsync"/>
        public override async Task TestConnectionAsync()
        {
            using (var result = await OpenConnectionAsync().ConfigureAwait(false))
            {
                await result.CloseAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="ISqlAgent.DatabaseExistsAsync"/>
        public override async Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentDatabase.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                Properties.Resources.CurrentDatabaseNullException);

            var conn = await OpenConnectionAsync(true).ConfigureAwait(false);

            bool result;

            MySqlDataReader reader = null;

            try
            {
                using (var command = new MySqlCommand())
                {
                    command.Connection = conn;
                    command.CommandTimeout = QueryTimeOut;
                    command.CommandText = $"SHOW DATABASES LIKE '{CurrentDatabase.Trim()}';";

                    reader = await command.ExecuteReaderAsync(cancellationToken)
                        .ConfigureAwait(false);
                    var table = await LightDataTable.CreateAsync(reader, cancellationToken)
                        .ConfigureAwait(false);

                    result = table.Rows.Count > 0;
                }
            }
            catch (Exception ex)
            {
                throw ex.WrapSqlException($"SHOW DATABASES LIKE '{CurrentDatabase.Trim()}';");
            }
            finally
            {
                reader?.Close();
                await conn.CloseAndDisposeAsync().ConfigureAwait(false);
            }

            return result;
        }

        /// <inheritdoc cref="ISqlAgent.DatabaseEmptyAsync"/>
        public override async Task<bool> DatabaseEmptyAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentDatabase.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                Properties.Resources.CurrentDatabaseNullException);

            var table = await ExecuteAsync<LightDataTable>(
                    $"SHOW TABLES FROM `{CurrentDatabase.Trim()}`;",
                    null, cancellationToken, ignoreTransaction: true)
                .ConfigureAwait(false);

            return table.Rows.Count < 1;
        }

        /// <inheritdoc cref="ISqlAgent.FetchDatabasesAsync"/>
        public override async Task<List<string>> FetchDatabasesAsync(string pattern = null,
            CancellationToken cancellationToken = default)
        {
            var query = pattern.IsNullOrWhiteSpace() ? "SHOW DATABASES;" : "SHOW DATABASES LIKE ?CD ;";

            var conn = await OpenConnectionAsync(true).ConfigureAwait(false);

            MySqlDataReader reader = null;
            List<string> result = null;

            try
            {
                using (var command = new MySqlCommand())
                {
                    command.Connection = conn;
                    command.CommandTimeout = QueryTimeOut;
                    command.CommandText = query;
                    if (!pattern.IsNullOrWhiteSpace())
                        command.Parameters.AddWithValue("?CD", pattern.Trim());

                    reader = await command.ExecuteReaderAsync(cancellationToken)
                        .ConfigureAwait(false);
                    var table = await LightDataTable.CreateAsync(reader, cancellationToken)
                        .ConfigureAwait(false);

                    result = table.Rows.Select(r => r.GetString(0)).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex.WrapSqlException(query);
            }
            finally
            {
                reader?.Close();
                await conn.CloseAndDisposeAsync().ConfigureAwait(false);
            }

            return result;
        }

        /// <inheritdoc cref="ISqlAgent.GetDefaultSchemaManager"/>
        public override ISchemaManager GetDefaultSchemaManager() => new MySqlSchemaManager(this);

        /// <inheritdoc cref="ISqlAgent.GetDefaultOrmService"/>
        public override IOrmService GetDefaultOrmService(Dictionary<Type, Type> customPocoMaps)
            => new MySqlOrmService(this, customPocoMaps);

        /// <inheritdoc cref="ISqlAgent.GetCopy"/>
        public override SqlAgentBase GetCopy() => new MySqlAgent(this);

        #endregion

        #region Transactions

        private static readonly AsyncLocal<MySqlTransaction> asyncTransaction = new AsyncLocal<MySqlTransaction>();
        private MySqlTransaction instanceTransaction = null;


        private MySqlTransaction CurrentTransaction
        {
            get
            {
                if (UseTransactionPerInstance) return instanceTransaction;
                return asyncTransaction.Value;
            }
            set
            {
                if (UseTransactionPerInstance) instanceTransaction = value;
                else asyncTransaction.Value = value;
            }
        }


        /// <summary>
        /// Starts a new transaction.
        /// </summary>
        /// <param name="cancellationToken">a cancellation token (if any)</param>
        /// <exception cref="InvalidOperationException">if transaction is already in progress</exception>
        protected override async Task<object> TransactionBeginAsync(CancellationToken cancellationToken)
        {
            if (IsTransactionInProgress) throw new InvalidOperationException(
                Properties.Resources.CannotStartTransactionException);

            var connection = await OpenConnectionAsync().ConfigureAwait(false);

            try
            {
                var transaction = await connection.BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);

                // no point in setting AsyncLocal as it will be lost on exit
                // should use method RegisterTransactionForAsyncContext in the transaction method
                if (UseTransactionPerInstance) instanceTransaction = transaction;

                return transaction;
            }
            catch (Exception)
            {
                await connection.CloseAndDisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// As the TransactionBeginAsync method is Async, AsyncLocal value is lost outside of it's context
        /// and needs to be set in the context of the invoking method.
        /// </summary>
        /// <param name="transaction">a transaction that has been initiated by the TransactionBeginAsync method</param>
        protected override void RegisterTransaction(object transaction)
        {
            if (UseTransactionPerInstance) return;

            if (transaction.IsNull()) throw new ArgumentNullException(nameof(transaction));

            var typedTransaction = transaction as MySqlTransaction;
            asyncTransaction.Value = typedTransaction ?? throw new ArgumentException(
                string.Format(Properties.Resources.InvalidTransactionType,
                    transaction.GetType().FullName), nameof(transaction));
        }

        /// <summary>
        /// As the TransactionCommitAsync and TransactionRollbackAsync methods are Async,
        /// AsyncLocal value is lost outside of it's context and needs to be set (to null)
        /// in the context of the invoking method.
        /// </summary>
        protected override void UnRegisterTransaction()
        {
            CurrentTransaction = null;
        }

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no transaction in progress</exception>
        protected override async Task TransactionCommitAsync()
        {
            if (!IsTransactionInProgress) throw new InvalidOperationException(
                Properties.Resources.NoTransactionToCommitException);

            await CurrentTransaction.CommitAsync().ConfigureAwait(false);
            await CleanUpTransactionAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Rollbacks the current transaction.
        /// </summary>
        /// <param name="ex">an exception that caused the rollback</param>
        protected override async Task<Exception> TransactionRollbackAsync(Exception ex)
        {
            var transaction = CurrentTransaction;

            if (!transaction.Connection.IsNull() && transaction.Connection.State == ConnectionState.Open)
            {
                try
                {
                    await CurrentTransaction.RollbackAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await CleanUpTransactionAsync().ConfigureAwait(false);
                    return ex.WrapSqlException(e);
                }
            }

            await CleanUpTransactionAsync().ConfigureAwait(false);

            return ex;
        }

        private async Task CleanUpTransactionAsync()
        {
            if (!IsTransactionInProgress) return;

            await CurrentTransaction.Connection.CloseAndDisposeAsync().ConfigureAwait(false);

            try { CurrentTransaction.Dispose(); }
            catch (Exception) { }
        }

        #endregion

        #region CRUD Methods

        /// <inheritdoc cref="ISqlAgent.FetchScalarAsync"/>
        public override async Task<int?> FetchScalarAsync(string token, SqlParam[] parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (token.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(token));

            var reader = await ReadAsync(GetSqlQuery(token), parameters, cancellationToken)
                .ConfigureAwait(false);

            int? result;
            try
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    result = reader.GetInt32Nullable(0);
                }
                else
                {
                    result = null;
                }
            }
            finally
            {
                await reader.CloseAsync();
            }

            return result;
        }

        /// <inheritdoc cref="ISqlAgent.FetchTableAsync"/>
        public override async Task<LightDataTable> FetchTableAsync(string token, SqlParam[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (token.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(token));

            return await ExecuteAsync<LightDataTable>(GetSqlQuery(token), parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.GetReaderAsync"/>
        public override async Task<ILightDataReader> GetReaderAsync(string token, SqlParam[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (token.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(token));

            return await ReadAsync(GetSqlQuery(token), parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.FetchTablesAsync"/>
        public override async Task<LightDataTable[]> FetchTablesAsync((string Token,
                SqlParam[] Parameters)[] queries, CancellationToken ct = default)
        {
            if (null == queries || queries.Length < 1) throw new ArgumentNullException(nameof(queries));
            if (queries.Any(q => q.Token.IsNullOrWhiteSpace()))
                throw new ArgumentException(Properties.Resources.QueryTokenEmptyException, nameof(queries));

            var tasks = new List<Task<LightDataTable>>();
            var result = new List<LightDataTable>();

            if (this.IsTransactionInProgress)
            {
                foreach (var (Token, Parameters) in queries)
                {
                    result.Add(await ExecuteAsync<LightDataTable>(GetSqlQuery(Token), Parameters, ct)
                        .ConfigureAwait(false));
                }
            }
            else
            {
                using (var conn = await OpenConnectionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        foreach (var (Token, Parameters) in queries)
                        {
                            result.Add(await FetchUsingConnectionAsync(conn, GetSqlQuery(Token),
                                ct, Parameters).ConfigureAwait(false));
                        }
                    }
                    finally
                    {
                        await conn.CloseAndDisposeAsync().ConfigureAwait(false);
                    }
                }
            }

            return result.ToArray();
        }

        /// <inheritdoc cref="ISqlAgent.FetchTableRawAsync"/>
        public override async Task<LightDataTable> FetchTableRawAsync(string sqlQuery,
            SqlParam[] parameters, CancellationToken cancellationToken = default)
        {
            if (sqlQuery.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(sqlQuery));

            return await ExecuteAsync<LightDataTable>(sqlQuery, parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.GetReaderRawAsync"/>
        public override async Task<ILightDataReader> GetReaderRawAsync(string sqlQuery,
            SqlParam[] parameters, CancellationToken cancellationToken = default)
        {
            if (sqlQuery.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(sqlQuery));

            return await ReadAsync(sqlQuery, parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.FetchTableFieldsAsync"/>
        public override Task<LightDataTable> FetchTableFieldsAsync(string table,
            string[] fields, CancellationToken cancellationToken = default)
        {
            if (table.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(table));
            if (null == fields || fields.Length < 1) throw new ArgumentNullException(nameof(fields));

            var preparedFields = new List<string>();
            foreach (var field in fields)
            {
                if (field.IsNullOrWhiteSpace())
                    throw new ArgumentException(Properties.Resources.FieldsEmptyException, nameof(fields));
                preparedFields.Add(field.ToConventional(this));
            }

            var fieldList = string.Join(", ", preparedFields.ToArray());

            return FetchTableRawAsync($"SELECT {fieldList} FROM {table.ToConventional(this)};",
                null, cancellationToken);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteInsertAsync"/>
        public override async Task<long> ExecuteInsertAsync(string insertStatementToken, SqlParam[] parameters)
        {
            if (insertStatementToken.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(insertStatementToken));

            return await ExecuteAsync<long>(GetSqlQuery(insertStatementToken), parameters)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteInsertRawAsync"/>
        public override async Task<long> ExecuteInsertRawAsync(string insertStatement, SqlParam[] parameters)
        {
            if (insertStatement.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(insertStatement));

            return await ExecuteAsync<long>(insertStatement, parameters).ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteCommandAsync"/>
        public override async Task<int> ExecuteCommandAsync(string statementToken, SqlParam[] parameters)
        {
            if (statementToken.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(statementToken));

            return await ExecuteAsync<int>(GetSqlQuery(statementToken), parameters).ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteCommandRawAsync"/>
        public override async Task<int> ExecuteCommandRawAsync(string statement, SqlParam[] parameters)
        {
            if (statement.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(statement));

            return await ExecuteAsync<int>(statement, parameters).ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteCommandBatchAsync"/>
        public override async Task ExecuteCommandBatchAsync(string[] statements)
        {
            if (null == statements || statements.Length < 1)
                throw new ArgumentNullException(nameof(statements));

            if (this.IsTransactionInProgress)
                throw new InvalidOperationException(Properties.Resources.CannotExecuteBatchException);

            var currentStatement = string.Empty;

            var conn = await OpenConnectionAsync().ConfigureAwait(false);

            try
            {
                using (var command = new MySqlCommand())
                {
                    command.Connection = conn;
                    command.CommandTimeout = QueryTimeOut;

                    foreach (var statement in statements.Where(s => !s.IsNullOrWhiteSpace()))
                    {
                        currentStatement = statement;
                        command.CommandText = statement;
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex.WrapSqlException(currentStatement);
            }
            finally
            {
                await conn.CloseAndDisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion


        internal async Task<MySqlConnection> OpenConnectionAsync(bool withoutDatabase = false)
        {
            MySqlConnection result;
            if (CurrentDatabase.IsNullOrWhiteSpace() || withoutDatabase)
            {
                result = new MySqlConnection(BaseConnectionString);
            }
            else
            {
                if (BaseConnectionString.Trim().EndsWith(";"))
                {
                    result = new MySqlConnection(BaseConnectionString + "Database=" + CurrentDatabase.Trim() + ";");
                }
                else
                {
                    result = new MySqlConnection(BaseConnectionString + ";Database=" + CurrentDatabase.Trim() + ";");
                }
            }

            try
            {
                await result.OpenAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await result.CloseAndDisposeAsync().ConfigureAwait(false);

                if (ex is MySqlException mySqlEx)
                {
                    if (mySqlEx.Number == 28000 ||
                        mySqlEx.Number == 42000)
                    {
                        throw new SqlAuthenticationException(Properties.Resources.SqlExceptionAccessDenied,
                            mySqlEx.Number, string.Empty, mySqlEx);
                    }
                    if (mySqlEx.Number == 2003)
                    {
                        throw new SqlException(Properties.Resources.SqlExceptionUnableToConnect,
                            mySqlEx.Number, string.Empty, mySqlEx);
                    }

                    throw mySqlEx.WrapSqlException();
                }
                throw;
            }

            return result;
        }


        private void AddParams(MySqlCommand command, SqlParam[] parameters)
        {
            command.Parameters.Clear();

            if (null == parameters || parameters.Length < 1) return;

            foreach (var p in parameters.Where(p => !p.ReplaceInQuery))
            {
                _ = command.Parameters.AddWithValue(Extensions.ParamPrefix + p.Name.Trim(),
                    p.GetValue(this));
            }
        }

        private string ReplaceParams(string sqlQuery, SqlParam[] parameters)
        {
            if (null == parameters || parameters.Length < 1) return sqlQuery;

            var result = sqlQuery;

            foreach (var parameter in parameters.Where(parameter => parameter.ReplaceInQuery))
            {
                var value = parameter.GetValue(this);
                result = result.Replace(parameter.Name.Trim(), value?.ToString() ?? "NULL");
            }

            return result;
        }

        private async Task<T> ExecuteAsync<T>(string sqlStatement, SqlParam[] parameters,
            CancellationToken ct = default, bool ignoreTransaction = false, bool withoutDatabase = false)
        {
            var transaction = CurrentTransaction;

            var usingTransaction = (!ignoreTransaction && !transaction.IsNull());

            MySqlConnection connection;
            if (usingTransaction)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = await OpenConnectionAsync(withoutDatabase).ConfigureAwait(false);
            }

            MySqlDataReader reader = null;

            try
            {
                using (var command = new MySqlCommand())
                {
                    command.Connection = connection;
                    if (usingTransaction) command.Transaction = transaction;

                    command.CommandTimeout = QueryTimeOut;
                    command.CommandText = ReplaceParams(sqlStatement, parameters);
                    AddParams(command, parameters);

                    if (typeof(T) == typeof(LightDataTable))
                    {
                        reader = await command.ExecuteReaderAsync(ct)
                            .ConfigureAwait(false);
                        return (T) (object) (await LightDataTable.CreateAsync(reader, ct)
                            .ConfigureAwait(false));
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        return (T) (object) command.LastInsertedId;
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        return (T) (object) (await command.ExecuteNonQueryAsync().ConfigureAwait(false));
                    }
                    else
                        throw new NotSupportedException(string.Format(
                            Properties.Resources.NotSupportedExecutionTypeException, typeof(T).FullName));
                }
            }
            catch (Exception ex)
            {
                throw ex.WrapSqlException(sqlStatement, parameters);
            }
            finally
            {
                reader?.Close();
                if (!usingTransaction) await connection.CloseAndDisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task<ILightDataReader> ReadAsync(string sqlStatement, SqlParam[] parameters,
            CancellationToken cancellationToken, bool ignoreTransaction = false, bool withoutDatabase = false)
        {
            var transaction = CurrentTransaction;

            var usingTransaction = (!ignoreTransaction && !transaction.IsNull());

            MySqlConnection connection;
            if (usingTransaction)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = await OpenConnectionAsync(withoutDatabase).ConfigureAwait(false);
            }

            var command = new MySqlCommand
            {
                Connection = connection,
                CommandTimeout = QueryTimeOut,
                CommandText = ReplaceParams(sqlStatement, parameters)
            };
            if (usingTransaction) command.Transaction = transaction;

            MySqlDataReader reader = null;

            AddParams(command, parameters);

            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new MySqlLightDataReader(reader, connection, command, usingTransaction);
            }
            catch (Exception ex)
            {
                reader?.Close();
                if (!usingTransaction) await connection.CloseAndDisposeAsync().ConfigureAwait(false);

                throw ex.WrapSqlException(sqlStatement, parameters);
            }
        }

        internal async Task<LightDataTable> FetchUsingConnectionAsync(MySqlConnection connection,
            string sqlStatement, CancellationToken cancellationToken, SqlParam[] parameters = null)
        {
            if (null == connection) throw new ArgumentNullException(nameof(connection));
            if (sqlStatement.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(sqlStatement));

            try
            {
                using (var command = new MySqlCommand())
                {
                    command.Connection = connection;
                    command.CommandTimeout = QueryTimeOut;
                    command.CommandText = ReplaceParams(sqlStatement, parameters);
                    AddParams(command, parameters);

                    using (var reader = await command.ExecuteReaderAsync(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        return await LightDataTable.CreateAsync(reader, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex.WrapSqlException(sqlStatement, parameters);
            }
        }

        protected override void DisposeManagedState() { }
    }
}
