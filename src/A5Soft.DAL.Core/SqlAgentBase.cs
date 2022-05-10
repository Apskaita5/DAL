using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using A5Soft.DAL.Core.DbSchema;

namespace A5Soft.DAL.Core
{
    /// <summary>
    /// Represents a base class for a concrete SQL implementation, e.g. MySql, SQLite, etc.
    /// </summary>
    /// <remarks>On ASP .NET core should be used:
    /// - as a singleton if only one database is used;
    /// - inside a scoped wrapper that would have IHttpContextAccessor dependency in constructor and
    /// initialize the encapsulated SQL agent using request info.
    /// On standalone application should use (static?) dictionary per database </remarks>
    public abstract class SqlAgentBase : ISqlAgent, IDisposable
    {
        #region Fields

        private readonly ISqlDictionary _sqlDictionary;

        #endregion

        #region Properties         

        /// <inheritdoc cref="ISqlAgent.Name"/>
        public abstract string Name { get; }

        /// <inheritdoc cref="ISqlAgent.SqlImplementationId"/>
        public abstract string SqlImplementationId { get; }

        /// <inheritdoc cref="ISqlAgent.IsFileBased"/>
        public abstract bool IsFileBased { get; }

        /// <inheritdoc cref="ISqlAgent.RootName"/>
        public abstract string RootName { get; }

        /// <inheritdoc cref="ISqlAgent.Wildcart"/>
        public abstract string Wildcart { get; }

        /// <inheritdoc cref="ISqlAgent.BaseConnectionString"/>
        public string BaseConnectionString { get; }

        /// <inheritdoc cref="ISqlAgent.CurrentDatabase"/>
        public string CurrentDatabase { get; }

        /// <inheritdoc cref="ISqlAgent.IsTransactionInProgress"/>
        public abstract bool IsTransactionInProgress { get; }


        /// <inheritdoc cref="ISqlAgent.QueryTimeOut"/>
        public int QueryTimeOut { get; set; } = 10000;

        /// <inheritdoc cref="ISqlAgent.UseTransactionPerInstance"/>
        public bool UseTransactionPerInstance { get; set; } = false;

        /// <inheritdoc cref="ISqlAgent.BooleanStoredAsTinyInt"/>
        public bool BooleanStoredAsTinyInt { get; set; } = true;

        /// <inheritdoc cref="ISqlAgent.GuidStoredAsBlob"/>
        public bool GuidStoredAsBlob { get; set; } = false;

        /// <inheritdoc cref="ISqlAgent.AllSchemaNamesLowerCased"/>
        public bool AllSchemaNamesLowerCased { get; set; } = true;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new SqlAgent instance.
        /// </summary>
        /// <param name="baseConnectionString">a connection string to use to connect to
        /// a database (should not include database parameter that is added by the
        /// SqlAgent implementation depending on the database chosen, should include password
        /// (if any), password replacement (if needed) should be handled by the user class)</param>
        /// <param name="allowEmptyConnString">whether the SqlAgent implementation can handle
        /// empty connection string (e.g. when the database parameter is the only parameter)</param>
        /// <param name="databaseName">a name of the database to use (if any)</param>
        /// <param name="sqlDictionary">an implementation of SQL dictionary to use (if any)</param>
        protected SqlAgentBase(string baseConnectionString, bool allowEmptyConnString,
            string databaseName, ISqlDictionary sqlDictionary)
        {
            if (baseConnectionString.IsNullOrWhiteSpace() && !allowEmptyConnString)
                throw new ArgumentNullException(nameof(baseConnectionString));

            _sqlDictionary = sqlDictionary;
            BaseConnectionString = baseConnectionString?.Trim() ?? string.Empty;
            CurrentDatabase = databaseName ?? string.Empty;
        }

        /// <summary>
        /// Creates an SqlAgent clone.
        /// </summary>
        /// <param name="agentToClone">an SqlAgent to clone</param>
        protected SqlAgentBase(SqlAgentBase agentToClone)
        {
            if (agentToClone.IsNull()) throw new ArgumentNullException(nameof(agentToClone));

            BaseConnectionString = agentToClone.BaseConnectionString;
            CurrentDatabase = agentToClone.CurrentDatabase;
            BooleanStoredAsTinyInt = agentToClone.BooleanStoredAsTinyInt;
            AllSchemaNamesLowerCased = agentToClone.AllSchemaNamesLowerCased;
            GuidStoredAsBlob = agentToClone.GuidStoredAsBlob;
            QueryTimeOut = agentToClone.QueryTimeOut;
            UseTransactionPerInstance = agentToClone.UseTransactionPerInstance;
            _sqlDictionary = agentToClone._sqlDictionary;
        }

        #endregion

        #region Common ISqlAgent Methods

        /// <inheritdoc cref="ISqlAgent.TestConnectionAsync"/>
        public abstract Task TestConnectionAsync();

        /// <inheritdoc cref="ISqlAgent.DatabaseExistsAsync"/>
        public abstract Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.DatabaseEmptyAsync"/>
        public abstract Task<bool> DatabaseEmptyAsync(CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.FetchDatabasesAsync"/>
        public abstract Task<List<string>> FetchDatabasesAsync(string pattern = null,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.GetDefaultSchemaManager"/>
        public abstract ISchemaManager GetDefaultSchemaManager();

        /// <inheritdoc cref="ISqlAgent.GetDefaultOrmService"/>
        public abstract IOrmService GetDefaultOrmService(Dictionary<Type, Type> customPocoMaps);

        /// <summary>
        /// Gets a clean copy (i.e. only connection data, not connection itself) of the SqlAgent instance
        /// in order to reuse instance data.
        /// </summary>
        public abstract SqlAgentBase GetCopy();

        /// <inheritdoc cref="ISqlAgent.GetCopy"/>
        ISqlAgent ISqlAgent.GetCopy()
        {
            return GetCopy();
        }

        #endregion

        #region Transactions        

        /// <inheritdoc cref="ISqlAgent.ExecuteInTransactionAsync"/>
        public async Task ExecuteInTransactionAsync(Func<Task> method, CancellationToken cancellationToken = default)
        {
            if (null == method) throw new ArgumentNullException(nameof(method));

            var transaction = await CaptureOrInitTransaction(cancellationToken);
            // transaction shall be registered within async context of this method
            if (null != transaction) RegisterTransaction(transaction);

            bool isOwner = (null != transaction);

            try
            {
                await method().ConfigureAwait(false);
                if (isOwner)
                {
                    await TransactionCommitAsync().ConfigureAwait(false);
                    UnRegisterTransaction();
                }
            }
            catch (Exception ex)
            {
                if (isOwner && IsTransactionInProgress)
                {
                    var rollbackEx = await TransactionRollbackAsync(ex);
                    UnRegisterTransaction();
                    if (null != rollbackEx) throw rollbackEx;
                }

                throw;
            }
        }

        /// <inheritdoc cref="ISqlAgent.FetchInTransactionAsync{TResult}"/>
        public async Task<TResult> FetchInTransactionAsync<TResult>(Func<Task<TResult>> method,
            CancellationToken cancellationToken = default)
        {
            if (null == method) throw new ArgumentNullException(nameof(method));

            var transaction = await CaptureOrInitTransaction(cancellationToken);
            // transaction shall be registered within async context of this method
            if (null != transaction) RegisterTransaction(transaction);

            bool isOwner = (null != transaction);

            try
            {
                var result = await method().ConfigureAwait(false);

                if (isOwner)
                {
                    await TransactionCommitAsync().ConfigureAwait(false);
                    UnRegisterTransaction();
                }

                return result;
            }
            catch (Exception ex)
            {
                if (isOwner && IsTransactionInProgress)
                {
                    var rollbackEx = await TransactionRollbackAsync(ex);
                    UnRegisterTransaction();
                    if (null != rollbackEx) throw rollbackEx;
                }

                throw;
            }
        }

        
        private async Task<object> CaptureOrInitTransaction(CancellationToken ct)
        {
            if (IsTransactionInProgress) return null;
            return await TransactionBeginAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        protected abstract Task<object> TransactionBeginAsync(CancellationToken cancellationToken);

        /// <summary>
        /// As the TransactionBeginAsync method is Async, AsyncLocal value is lost outside of it's context
        /// and needs to be set in the context of the invoking method.
        /// </summary>
        /// <param name="transaction">a transaction that has been initiated by the TransactionBeginAsync method</param>
        protected abstract void RegisterTransaction(object transaction);

        /// <summary>
        /// As the TransactionCommitAsync and TransactionRollbackAsync methods are Async,
        /// AsyncLocal value is lost outside of it's context and needs to be set (to null)
        /// in the context of the invoking method.
        /// </summary>
        protected abstract void UnRegisterTransaction();

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no transaction in progress</exception>
        protected abstract Task TransactionCommitAsync();

        /// <summary>
        /// Rollbacks the current transaction.
        /// </summary>
        /// <param name="ex">an exception that caused the rollback</param>
        /// <returns>rollback exception that wraps original exception if rollback failed; otherwise null</returns>
        protected abstract Task<Exception> TransactionRollbackAsync(Exception ex);

        #endregion

        #region CRUD Methods

        /// <inheritdoc cref="ISqlAgent.FetchScalarAsync"/>
        public abstract Task<int?> FetchScalarAsync(string token, SqlParam[] parameters = null,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.FetchTableAsync"/>
        public abstract Task<LightDataTable> FetchTableAsync(string token, SqlParam[] parameters,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.GetReaderAsync"/>
        public abstract Task<ILightDataReader> GetReaderAsync(string token, SqlParam[] parameters,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.FetchTablesAsync"/>
        public abstract Task<LightDataTable[]> FetchTablesAsync((string Token, SqlParam[] Parameters)[] queries,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.FetchTableRawAsync"/>
        public abstract Task<LightDataTable> FetchTableRawAsync(string sqlQuery, SqlParam[] parameters,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.GetReaderRawAsync"/>
        public abstract Task<ILightDataReader> GetReaderRawAsync(string token, SqlParam[] parameters,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.FetchTableFieldsAsync"/>
        public abstract Task<LightDataTable> FetchTableFieldsAsync(string table, string[] fields,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ISqlAgent.ExecuteInsertAsync"/>
        public abstract Task<Int64> ExecuteInsertAsync(string insertStatementToken, SqlParam[] parameters);

        /// <inheritdoc cref="ISqlAgent.ExecuteInsertRawAsync"/>
        public abstract Task<Int64> ExecuteInsertRawAsync(string insertStatement, SqlParam[] parameters);

        /// <inheritdoc cref="ISqlAgent.ExecuteCommandAsync"/>
        public abstract Task<int> ExecuteCommandAsync(string statementToken, SqlParam[] parameters);

        /// <inheritdoc cref="ISqlAgent.ExecuteCommandRawAsync"/>
        public abstract Task<int> ExecuteCommandRawAsync(string statement, SqlParam[] parameters);

        /// <inheritdoc cref="ISqlAgent.ExecuteCommandBatchAsync"/>
        public abstract Task ExecuteCommandBatchAsync(string[] statements);

        #endregion

        /// <summary>
        /// Gets an SQL query identified by the token for the specific SQL implementation sqlAgent.
        /// </summary>
        /// <param name="token">a token (code) that identifies an SQL query in SQL repository</param>
        /// <exception cref="ArgumentNullException">Parameter token is not specified.</exception>
        /// <exception cref="ArgumentNullException">Parameter sqlAgent is not specified.</exception>
        /// <exception cref="InvalidOperationException">SQL repository path is not initialized.</exception>
        /// <exception cref="InvalidOperationException">Global cache provider ir not initialized.</exception>
        /// <exception cref="ArgumentException">SQL agent does not implement repository file prefix.</exception>
        /// <exception cref="FileNotFoundException">No repository files found or they contain no data
        /// for the SQL agent type specified.</exception>
        /// <exception cref="Exception">Failed to load file due to missing query tokens.</exception>
        /// <exception cref="Exception">Failed to load file due to duplicate query token.</exception>
        /// <exception cref="InvalidOperationException">SQL dictionary failed to initialize for unknown reason.</exception>
        /// <exception cref="InvalidOperationException">SQL query token is unknown.</exception>
        protected string GetSqlQuery(string token)
        {
            if (_sqlDictionary.IsNull())
                throw new InvalidOperationException(Properties.Resources.SqlDictionaryNotConfiguredException);
            return _sqlDictionary.GetSqlQuery(token, this);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    DisposeManagedState();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected abstract void DisposeManagedState();

        #endregion
    }
}
