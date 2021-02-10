using A5Soft.DAL.Core;
using A5Soft.DAL.Core.DbSchema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using A5Soft.DAL.Core.MicroOrm;

namespace A5Soft.DAL.SQLite
{
    public class SqliteAgent : SqlAgentBase
    {
        #region Constants

        private const string AgentName = "SQLite connector";
        private const bool AgentIsFileBased = true;
        private const string AgentRootName = "";
        private const string AgentWildcart = "%";

        #endregion

        #region Properties

        /// <summary>
        /// Gets a name of the SQL implementation behind the SqlAgent, i. e. SQLite connector.
        /// </summary>
        public override string Name => AgentName;

        /// <summary>
        /// Gets an id of the concrete SQL implementation, i.e. SQLite.
        /// The id is used to select an appropriate SQL token dictionary.
        /// </summary>
        public override string SqlImplementationId => Extensions.SqliteImplementationId;

        /// <summary>
        /// Gets a value indicationg whether the SQL engine is file based, i.e. true.
        /// </summary>
        public override bool IsFileBased => AgentIsFileBased;

        /// <summary>
        /// Gets a name of the root user as defined in the SQL implementation behind the SqlAgent, i.e. none.
        /// </summary>
        public override string RootName => AgentRootName;

        /// <summary>
        /// Gets a simbol used as a wildcart for the SQL implementation behind the SqlAgent, i.e. %.
        /// </summary>
        public override string Wildcart => AgentWildcart;

        /// <summary>
        /// Gets a value indicationg whether an SQL transation is in progress.
        /// </summary>
        public override bool IsTransactionInProgress => (CurrentTransaction != null);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new SqliteAgent instance.
        /// </summary>
        /// <param name="baseConnectionString">a connection string to use to connect to
        /// a database (should not include database parameter that is added by the
        /// SqlAgent implementation depending on the database chosen, should include password
        /// (if any), password replacement (if needed) should be handled by the user class)</param>
        /// <param name="databaseName">a name of the database to use (if any)</param>
        /// <param name="sqlDictionary">an implementation of SQL dictionary to use (if any)</param>
        public SqliteAgent(string baseConnectionString, string databaseName, ISqlDictionary sqlDictionary)
            : base(baseConnectionString, true, databaseName, sqlDictionary)
        {
            if (databaseName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(databaseName));
        }

        /// <summary>
        /// Clones an SqliteAgent instance.
        /// </summary>
        /// <param name="agentToClone">an SqliteAgent instance to clone</param>
        private SqliteAgent(SqliteAgent agentToClone) : base(agentToClone) { }

        #endregion

        #region Common ISqlAgent Methods

        /// <inheritdoc cref="ISqlAgent.TestConnectionAsync"/>
        public override async Task TestConnectionAsync()
        {
            using (var result = await OpenConnectionAsync().ConfigureAwait(false))
            {
                result.Close();
            }
        }

        /// <inheritdoc cref="ISqlAgent.DatabaseExistsAsync"/>
        public override Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(File.Exists(CurrentDatabase));
        }

        /// <inheritdoc cref="ISqlAgent.DatabaseEmptyAsync"/>
        public override async Task<bool> DatabaseEmptyAsync(CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync())
            {
                var table = await ExecuteAsync<LightDataTable>(
                    "SELECT name, sql FROM sqlite_master WHERE type='table' AND NOT name LIKE 'sqlite_%';",
                    null, CancellationToken.None).ConfigureAwait(false);
                conn.Close();
                return (table.Rows.Count < 1);
            }
        }

        /// <inheritdoc cref="ISqlAgent.GetDefaultSchemaManager"/>
        public override ISchemaManager GetDefaultSchemaManager() => new SqliteSchemaManager(this);

        /// <inheritdoc cref="ISqlAgent.GetDefaultOrmService"/>
        public override IOrmService GetDefaultOrmService(Dictionary<Type, Type> customPocoMaps)
            => new SqliteOrmService(this, customPocoMaps);

        /// <inheritdoc cref="ISqlAgent.GetCopy"/>
        public override SqlAgentBase GetCopy() => new SqliteAgent(this);

        #endregion

        #region Transactions

        private static readonly AsyncLocal<SQLiteTransaction> asyncTransaction = new AsyncLocal<SQLiteTransaction>();
        private SQLiteTransaction instanceTransaction = null;


        private SQLiteTransaction CurrentTransaction
        {
            get
            {
                if (UseTransactionPerInstance) return instanceTransaction;
                return asyncTransaction.Value;
            }
            set
            {
                if (UseTransactionPerInstance) instanceTransaction = value;
                asyncTransaction.Value = value;
            }
        }


        /// <summary>
        /// Starts a new transaction.
        /// </summary> 
        /// <param name="cancellationToken">a cancelation token (if any); does nothing for SQLite implementation</param>
        /// <exception cref="InvalidOperationException">if transaction is already in progress</exception>
        protected override async Task<object> TransactionBeginAsync(CancellationToken cancellationToken)
        {
            if (IsTransactionInProgress) throw new InvalidOperationException(Properties.Resources.CannotStartTransactionException);

            var connection = await OpenConnectionAsync().ConfigureAwait(false);

            try
            {
                var transaction = connection.BeginTransaction();

                // no point in setting AsyncLocal as it will be lost on exit
                // should use method RegisterTransactionForAsyncContext in the transaction method
                if (UseTransactionPerInstance) instanceTransaction = transaction;
                
                return transaction;
            }
            catch (Exception)
            {
                connection.CloseAndDispose();

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

            var typedTransaction = transaction as SQLiteTransaction;
            asyncTransaction.Value = typedTransaction ?? throw new ArgumentException(
                string.Format(Properties.Resources.InvalidTransactionTypeException, 
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
        protected override Task TransactionCommitAsync()
        {
            if (!IsTransactionInProgress) throw new InvalidOperationException(
                Properties.Resources.NoTransactionToCommitException);

            CurrentTransaction.Commit();
            CleanUpTransaction();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Rollbacks the current transaction.
        /// </summary>
        /// <param name="ex">an exception that caused the rollback</param>
        protected override Task<Exception> TransactionRollbackAsync(Exception ex)
        {
            if (!CurrentTransaction.Connection.IsNull() && CurrentTransaction.Connection.State == ConnectionState.Open)
            {
                try
                {
                    CurrentTransaction.Rollback();
                }
                catch (Exception e)
                {
                    CleanUpTransaction();
                    return Task.FromResult(ex.WrapSqlException(e));
                }
            }

            CleanUpTransaction();

            return Task.FromResult<Exception>(null);
        }

        private void CleanUpTransaction()
        {
            if (!IsTransactionInProgress) return;

            CurrentTransaction.Connection.CloseAndDispose();

            try { CurrentTransaction.Dispose(); }
            catch (Exception) { }
        }

        #endregion

        #region CRUD Methods

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
        public override async Task<LightDataTable[]> FetchTablesAsync((string Token, SqlParam[] Parameters)[] queries,
            CancellationToken cancellationToken = default)
        {
            if (null == queries || queries.Length < 1) throw new ArgumentNullException(nameof(queries));
            if (queries.Any(q => q.Token.IsNullOrWhiteSpace())) 
                throw new ArgumentException(Properties.Resources.QueryTokenEmptyException, nameof(queries));

            var tasks = new List<Task<LightDataTable>>();

            if (this.IsTransactionInProgress)
            {
                foreach (var (Token, Parameters) in queries)
                {
                    tasks.Add(ExecuteAsync<LightDataTable>(GetSqlQuery(Token), Parameters, cancellationToken));
                }
                return await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                using (var conn = await OpenConnectionAsync().ConfigureAwait(false))
                {

                    LightDataTable[] result;

                    try
                    {
                        foreach (var (Token, Parameters) in queries) 
                            tasks.Add(FetchUsingConnectionAsync(conn, GetSqlQuery(Token),
                            cancellationToken, Parameters));
                        result = (await Task.WhenAll(tasks).ConfigureAwait(false));
                    }
                    finally
                    {
                        conn.CloseAndDispose();
                    }

                    return result;
                }
            }
        }

        /// <inheritdoc cref="ISqlAgent.FetchTableRawAsync"/>
        public override async Task<LightDataTable> FetchTableRawAsync(string sqlQuery, SqlParam[] parameters,
            CancellationToken cancellationToken = default)
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
        public override async Task<LightDataTable> FetchTableFieldsAsync(string table, string[] fields,
            CancellationToken cancellationToken = default)
        {
            if (table.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(table));
            if (null == fields || fields.Length < 1) throw new ArgumentNullException(nameof(fields));
            if (fields.Any(field => field.IsNullOrWhiteSpace()))
                throw new ArgumentException(Properties.Resources.FieldsEmptyException, nameof(fields));

            var fieldsQuery = string.Join(", ", fields.Select(field => 
                field.ToConventional(this)).ToArray());

            return await FetchTableRawAsync($"SELECT {fieldsQuery} FROM {table.ToConventional(this)};", 
                null, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteInsertAsync"/>
        public override async Task<long> ExecuteInsertAsync(string insertStatementToken, SqlParam[] parameters)
        {
            if (insertStatementToken.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(insertStatementToken));

            return await ExecuteAsync<long>(GetSqlQuery(insertStatementToken), parameters,
                CancellationToken.None).ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteInsertRawAsync"/>
        public override async Task<long> ExecuteInsertRawAsync(string insertStatement, SqlParam[] parameters)
        {
            if (insertStatement.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(insertStatement));

            return await ExecuteAsync<long>(insertStatement, parameters, CancellationToken.None)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteCommandAsync"/>
        public override async Task<int> ExecuteCommandAsync(string statementToken, SqlParam[] parameters)
        {
            if (statementToken.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(statementToken));

            return await ExecuteAsync<int>(GetSqlQuery(statementToken), parameters,
                CancellationToken.None).ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteCommandRawAsync"/>
        public override async Task<int> ExecuteCommandRawAsync(string statement, SqlParam[] parameters)
        {
            if (statement.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(statement));

            return await ExecuteAsync<int>(statement, parameters, CancellationToken.None)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="ISqlAgent.ExecuteCommandBatchAsync"/>
        public override async Task ExecuteCommandBatchAsync(string[] statements)
        {
            if (null == statements || statements.Length < 1)
                throw new ArgumentNullException(nameof(statements));
            if (statements.All(statement => statement.IsNullOrWhiteSpace()))
                throw new ArgumentException(Properties.Resources.StatementsEmptyException, nameof(statements));
            if (this.IsTransactionInProgress)
                throw new InvalidOperationException(Properties.Resources.NoBatchInTransactionException);

            string currentStatement = string.Empty;

            using (var conn = await OpenConnectionAsync().ConfigureAwait(false))
            {
                try
                {
                    using (var command = new SQLiteCommand())
                    {
                        command.Connection = conn;
                        command.CommandTimeout = QueryTimeOut;

                        foreach (var statement in statements.Where(s => !s.IsNullOrWhiteSpace()))
                        {
                            command.CommandText = statement;
                            currentStatement = statement;
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
                    conn.CloseAndDispose();
                }
            }

        }

        #endregion
           
        internal async Task<SQLiteConnection> OpenConnectionAsync(bool enableForeignKeys = true)
        {
            SQLiteConnection result;
            if (BaseConnectionString.Trim().StartsWith(";"))
            {
                result = new SQLiteConnection("Data Source=" + CurrentDatabase.Trim() + BaseConnectionString);
            }
            else
            {
                result = new SQLiteConnection("Data Source=" + CurrentDatabase.Trim() + ";" + BaseConnectionString);
            }

            try
            {
                await result.OpenAsync().ConfigureAwait(false);

                if (enableForeignKeys)
                {
                    // foreign keys are disabled by default in SQLite
                    using (var command = new SQLiteCommand())
                    {
                        command.Connection = result;
                        command.CommandText = "PRAGMA foreign_keys = ON;";
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                result.CloseAndDispose();

                if (ex is SQLiteException sqliteEx)
                {
                    if (sqliteEx.ErrorCode == (int)SQLiteErrorCode.Auth ||
                        sqliteEx.ErrorCode == (int)SQLiteErrorCode.Auth_User ||
                        sqliteEx.ErrorCode == (int)SQLiteErrorCode.NotADb ||
                        sqliteEx.ErrorCode == (int)SQLiteErrorCode.CantOpen ||
                        sqliteEx.ErrorCode == (int)SQLiteErrorCode.Corrupt)
                    {
                        throw new SqlException(Properties.Resources.SqlExceptionPasswordInvalid, sqliteEx.ErrorCode,
                            string.Empty, sqliteEx);
                    }
                    if (sqliteEx.ErrorCode == (int)SQLiteErrorCode.NotFound)
                    {
                        throw new SqlException(string.Format(Properties.Resources.SqlExceptionDatabaseNotFound,
                            CurrentDatabase, sqliteEx.Message), sqliteEx.ErrorCode, string.Empty, sqliteEx);
                    }

                    throw sqliteEx.WrapSqlException();
                }

                throw;
            }

            return result;
        }

        
        private void AddParams(SQLiteCommand command, SqlParam[] parameters)
        {
            command.Parameters.Clear();

            if (null == parameters || parameters.Length < 1) return;

            foreach (var p in parameters.Where(p => !p.ReplaceInQuery)) 
                command.Parameters.AddWithValue(Extensions.ParamPrefix + p.Name.Trim(), p.GetValue(this));
        }

        private string ReplaceParams(string sqlQuery, SqlParam[] parameters)
        {
            if (null == parameters || parameters.Length < 1) return sqlQuery;

            var result = sqlQuery;

            foreach (var parameter in parameters.Where(parameter => parameter.ReplaceInQuery))
            {
                if (parameter.Value.IsNull())
                {
                    result = result.Replace(parameter.Name.Trim(), "NULL");
                }
                else
                {
                    result = result.Replace(parameter.Name.Trim(), parameter.Value.ToString());
                }
            }

            return result;
        }

        private string ReplaceParamsInRawQuery(string sqlQuery, SqlParam[] parameters)
        {
            if (null == parameters || parameters.Length < 1) return sqlQuery;

            return parameters.Where(parameter => parameter.ReplaceInQuery).
                Aggregate(sqlQuery, (current, parameter) =>
                    current.Replace("?" + parameter.Name.Trim(), 
                        Extensions.ParamPrefix + parameter.Name.Trim()));
        }
                               
        
        private async Task<T> ExecuteAsync<T>(string sqlStatement, SqlParam[] parameters,
            CancellationToken cancellationToken, bool ignoreTransaction = false)
        {
            var transaction = CurrentTransaction;

            var usingTransaction = (!ignoreTransaction && !transaction.IsNull());

            SQLiteConnection connection;
            if (usingTransaction)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = await OpenConnectionAsync().ConfigureAwait(false);
            }

            try
            {
                using (var command = new SQLiteCommand())
                {
                    command.Connection = connection;
                    if (usingTransaction) command.Transaction = transaction;

                    command.CommandTimeout = QueryTimeOut;
                    
                    var commandText = ReplaceParams(sqlStatement, parameters).Trim();
                    if (typeof(T) == typeof(long))
                    {
                        if (commandText.EndsWith(";"))
                        {
                            commandText += " SELECT last_insert_rowid() AS LastInsertId;";
                        }
                        else
                        {
                            commandText += "; SELECT last_insert_rowid() AS LastInsertId;";
                        }
                    }
                    command.CommandText = commandText;

                    AddParams(command, parameters);

                    if (typeof(T) == typeof(LightDataTable))
                    {
                        var reader = await command.ExecuteReaderAsync(cancellationToken)
                            .ConfigureAwait(false);
                        return (T)(object)(await LightDataTable.CreateAsync(reader, cancellationToken)
                            .ConfigureAwait(false));
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        return (T)await command.ExecuteScalarAsync().ConfigureAwait(false);
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        return (T)(object)(await command.ExecuteNonQueryAsync().ConfigureAwait(false));
                    }
                    else
                        throw new NotSupportedException(string.Format(
                            Properties.Resources.InvalidInternalExecuteParamException, typeof(T).FullName));
                }
            }
            catch (Exception ex)
            {
                throw ex.WrapSqlException(sqlStatement, parameters);
            }
            finally
            {
                if (!usingTransaction) connection.CloseAndDispose();
            }
        }

        private async Task<ILightDataReader> ReadAsync(string sqlStatement, SqlParam[] parameters,
            CancellationToken cancellationToken, bool ignoreTransaction = false)
        {
            var transaction = CurrentTransaction;

            var usingTransaction = (!ignoreTransaction && !transaction.IsNull());

            SQLiteConnection connection;
            if (usingTransaction)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = await OpenConnectionAsync().ConfigureAwait(false);
            }

            try
            {
                var command = new SQLiteCommand()
                {
                    Connection = connection,
                    CommandTimeout = QueryTimeOut,
                    CommandText = ReplaceParams(sqlStatement, parameters)
                };
                if (usingTransaction) command.Transaction = transaction;

                AddParams(command, parameters);

                var reader = await command.ExecuteReaderAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new SQLiteLightDataReader(reader, connection, command, usingTransaction);
            }
            catch (Exception ex)
            {
                if (!usingTransaction) connection.CloseAndDispose();
                throw ex.WrapSqlException(sqlStatement, parameters);
            }
        }

        internal async Task<LightDataTable> FetchUsingConnectionAsync(SQLiteConnection connection,
            string sqlStatement, CancellationToken cancellationToken, SqlParam[] parameters = null)
        {
            if (null == connection) throw new ArgumentNullException(nameof(connection));
            if (sqlStatement.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(sqlStatement));

            try
            {
                using (var command = new SQLiteCommand())
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
