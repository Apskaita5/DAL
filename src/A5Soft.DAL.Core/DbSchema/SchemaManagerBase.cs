using A5Soft.DAL.Core.MicroOrm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.DbSchema
{
    public abstract class SchemaManagerBase : ISchemaManager
    {
        private readonly ISqlAgent _agent;


        /// <summary>
        /// Gets an id of the concrete SQL implementation, e.g. MySQL, SQLite.
        /// The id is used to make sure that the OrmServiceBase implementation match SqlAgentBase implementation.
        /// </summary>
        public abstract string SqlImplementationId { get; }

        /// <summary>
        /// Gets an instance of an Sql agent to use for queries and statements.
        /// </summary>
        protected ISqlAgent Agent => _agent;


        /// <summary>
        /// Creates a new Orm service.
        /// </summary>
        /// <param name="agent">an instance of an Sql agent to use for queries and statements; its implementation
        /// type should match Orm service implementation type</param>
        protected SchemaManagerBase(ISqlAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            
            if (!_agent.SqlImplementationId.EqualsByConvention(SqlImplementationId))
                throw new ArgumentException(string.Format(
                    Properties.Resources.SqlAgentAndSchemaManagerTypeMismatchException,
                    _agent.SqlImplementationId, SqlImplementationId), nameof(agent));

            if (_agent.CurrentDatabase.IsNullOrWhiteSpace()) throw new ArgumentException(
                Properties.Resources.SchemaManagerRequiresDatabaseException, nameof(agent));
        }


        /// <inheritdoc cref="ISchemaManager.GetDbSchemaAsync"/>
        public abstract Task<Schema> GetDbSchemaAsync(CancellationToken cancellationToken = default);

        #region GetDbSchemaErrorsAsync Implementation

        /// <inheritdoc cref="ISchemaManager.GetDbSchemaErrorsAsync"/>
        public async Task<List<SchemaError>> GetDbSchemaErrorsAsync(Schema gaugeSchema, 
            CancellationToken cancellationToken = default)
        {
            if (gaugeSchema.IsNull()) throw new ArgumentNullException(nameof(gaugeSchema));
            if (gaugeSchema.Tables.Count < 1) throw new ArgumentException(
                Properties.Resources.SqlAgentBase_GaugeSchemaEmpty);

            var actualSchema = await GetDbSchemaAsync(cancellationToken).ConfigureAwait(false);

            return this.GetDbSchemaErrors(gaugeSchema, actualSchema);
        }

        /// <inheritdoc cref="ISchemaManager.GetDbSchemaErrors"/>
        public abstract List<SchemaError> GetDbSchemaErrors(Schema gaugeSchema, Schema actualSchema);

        /// <summary>
        /// Gets a new instance of the SqlSchemaError for a repairable field level error.
        /// </summary>
        /// <param name="errorType">a type of the error (inconsistency)</param>
        /// <param name="description">a description of the error (inconsistency) (must be specified)</param>
        /// <param name="table">the name of the database table which field is inconsistent  (must be specified)</param>
        /// <param name="field">the name of the database field that is inconsistent</param>
        /// <param name="sqlStatementsToRepair">a collection of the SQL statements 
        /// that should be issued to repair the error (must be specified)</param>
        /// <exception cref="ArgumentNullException">Error description is not specified.</exception>
        /// <exception cref="ArgumentNullException">Error table is not specified.</exception>
        /// <exception cref="ArgumentNullException">SQL statements to repair the error is not specified.</exception>
        /// <exception cref="ArgumentException">No SQL statement could be empty.</exception>
        protected SchemaError GetDbSchemaError(SchemaErrorType errorType, string description,
            string table, string field, string[] sqlStatementsToRepair)
        {
            return new SchemaError(errorType, description, table, field, sqlStatementsToRepair);
        }

        /// <summary>
        /// Gets a new instance of the SqlSchemaError for a repairable table level error.
        /// </summary>
        /// <param name="errorType">a type of the error (inconsistency)</param>
        /// <param name="description">a description of the error (inconsistency) (must be specified)</param>
        /// <param name="table">the name of the database table which schema is inconsistent  (must be specified)</param>
        /// <param name="sqlStatementsToRepair">a collection of the SQL statements 
        /// that should be issued to repair the error (must be specified)</param>
        /// <exception cref="ArgumentNullException">Error description is not specified.</exception>
        /// <exception cref="ArgumentNullException">Error table is not specified.</exception>
        /// <exception cref="ArgumentNullException">SQL statements to repair the error is not specified.</exception>
        /// <exception cref="ArgumentException">No SQL statement could be empty.</exception>
        protected SchemaError GetDbSchemaError(SchemaErrorType errorType, string description,
            string table, string[] sqlStatementsToRepair)
        {
            return new SchemaError(errorType, description, table, sqlStatementsToRepair);
        }

        /// <summary>
        /// Gets a new instance of the SqlSchemaError for a repairable database level error.
        /// </summary>
        /// <param name="errorType">a type of the error (inconsistency)</param>
        /// <param name="description">a description of the error (inconsistency) (must be specified)</param>
        /// <param name="sqlStatementsToRepair">a collection of the SQL statements 
        /// that should be issued to repair the error (must be specified)</param>
        /// <exception cref="ArgumentNullException">Error description is not specified.</exception>
        /// <exception cref="ArgumentNullException">Error table is not specified.</exception>
        /// <exception cref="ArgumentNullException">SQL statements to repair the error is not specified.</exception>
        /// <exception cref="ArgumentException">No SQL statement could be empty.</exception>
        protected SchemaError GetDbSchemaError(SchemaErrorType errorType, string description,
            string[] sqlStatementsToRepair)
        {
            return new SchemaError(errorType, description, sqlStatementsToRepair);
        }

        /// <summary>
        /// Gets a new instance of the SqlSchemaError for an unrepairable error.
        /// </summary>
        /// <param name="errorType">a type of the error (inconsistency)</param>
        /// <param name="description">a description of the error (inconsistency) (must be specified)</param>
        /// <param name="table">the name of the database table which field is inconsistent</param>
        /// <param name="field">the name of the database field that is inconsistent</param>
        /// <exception cref="ArgumentNullException">Error description is not specified.</exception>
        protected SchemaError GetUnrepairableDbSchemaError(SchemaErrorType errorType,
            string description, string table, string field)
        {
            return new SchemaError(errorType, description, table, field);
        }

        #endregion

        /// <inheritdoc cref="ISchemaManager.GetCreateDatabaseSql"/>
        public abstract string GetCreateDatabaseSql(Schema schema);

        /// <inheritdoc cref="ISchemaManager.CreateDatabaseAsync"/>
        public abstract Task CreateDatabaseAsync(Schema schema);

        /// <inheritdoc cref="ISchemaManager.InitDatabaseAsync"/>
        public abstract Task InitDatabaseAsync(Schema schema);

        /// <inheritdoc cref="ISchemaManager.DropDatabaseAsync"/>
        public abstract Task DropDatabaseAsync();

        #region Database Cloning Methods

        /// <inheritdoc cref="ISchemaManager.CloneDatabaseAsync"/>
        public async Task CloneDatabaseAsync(SchemaManagerBase cloneManager, Schema schemaToUse,
            IProgress<CloneProgressArgs> progress = null, CancellationToken ct = default)
        {
            if (cloneManager.IsNull()) throw new ArgumentNullException(nameof(cloneManager));

            progress?.Report(new CloneProgressArgs(CloneProgressArgs.Stage.FetchingSchema, 
                string.Empty, 0));

            if (schemaToUse.IsNull()) 
                schemaToUse = await GetDbSchemaAsync(ct).ConfigureAwait(false);

            if (CloneCanceled(progress, ct)) return;

            progress?.Report(new CloneProgressArgs(CloneProgressArgs.Stage.CreatingSchema, 
                string.Empty, 0));

            await cloneManager.CreateDatabaseAsync(schemaToUse).ConfigureAwait(false);

            if (CloneCanceled(progress, ct)) return;

            await cloneManager.Agent.ExecuteInTransactionAsync(async () =>
            {
                await cloneManager.DisableForeignKeysForCurrentTransactionAsync().ConfigureAwait(false);
                await CopyData(schemaToUse, cloneManager, progress, ct).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!ct.IsCancellationRequested) progress?.Report(
                new CloneProgressArgs(CloneProgressArgs.Stage.Completed, 
                    string.Empty, 100));
        }

        protected static bool CloneCanceled(IProgress<CloneProgressArgs> progress, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                progress?.Report(new CloneProgressArgs(CloneProgressArgs.Stage.Canceled, 
                    string.Empty, 100));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Copies table data from the current SqlAgent instance to the target SqlAgent instance.
        /// </summary>
        /// <param name="schema">a schema of the database to copy the data</param>
        /// <param name="targetManager">the target Sql schema manager to copy the data to</param>
        /// <remarks>Required for <see cref="CloneDatabaseAsync">CloneDatabaseAsync</see> infrastructure.
        /// Basically iterates tables, selects data, creates an IDataReader for the table and passes it to the 
        /// <see cref="InsertTableDataAsync">InsertTableData</see> method of the target SqlAgent.</remarks>
        protected abstract Task CopyData(Schema schema, SchemaManagerBase targetManager,
            IProgress<CloneProgressArgs> progress, CancellationToken ct);

        /// <summary>
        /// Disables foreign key checks for the current transaction.
        /// </summary>
        /// <remarks>Required for <see cref="CloneDatabaseAsync">CloneDatabaseAsync</see> infrastructure.</remarks>
        protected abstract Task DisableForeignKeysForCurrentTransactionAsync();

        /// <summary>
        /// Invokes protected <see cref="InsertTableDataAsync">InsertTableDataAsync</see>
        /// method on target SqlAgent. Used to bypass cross instance protected method
        /// access limitation.
        /// </summary>
        /// <param name="target">the SqlAgent to invoke the <see cref="InsertTableDataAsync">InsertTableDataAsync</see>
        /// method on</param>
        /// <param name="table">a schema of the table to insert the data to</param>
        /// <param name="reader">an IDataReader to read the table data from</param>
        /// <remarks>Required for <see cref="CloneDatabaseAsync">CloneDatabaseAsync</see> infrastructure.
        /// The insert is performed using a transaction that is already initiated by the 
        /// <see cref="CloneDatabaseAsync">CloneDatabaseAsync</see>.</remarks>
        protected static Task<long> CallInsertTableDataAsync(SchemaManagerBase target, TableSchema table,
            IDataReader reader, long totalRowCount, long currentRow, int currentProgress,
            IProgress<CloneProgressArgs> progress, CancellationToken ct)
        {
            return target.InsertTableDataAsync(table, reader, totalRowCount, currentRow, currentProgress, progress, ct);
        }

        /// <summary>
        /// Inserts table data from the reader to the current SqlAgent instance,
        /// </summary>
        /// <param name="table">a schema of the table to insert the data to</param>
        /// <param name="reader">an IDataReader to read the table data from.</param>
        /// <remarks>Required for <see cref="CloneDatabaseAsync">CloneDatabaseAsync</see> infrastructure.
        /// The insert is performed using a transaction that is already initiated by the 
        /// <see cref="CloneDatabaseAsync">CloneDatabaseAsync</see>.</remarks>
        protected abstract Task<long> InsertTableDataAsync(TableSchema table, IDataReader reader,
            long totalRowCount, long currentRow, int currentProgress, IProgress<CloneProgressArgs> progress,
            CancellationToken ct);

        private static readonly string[] ParamLetters = new string[]{"A", "B", "C", "D", "E",
            "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "Q", "P", "R", "S", "T", "U", "V", "Z", "W"};
        private static List<string> _paramDictionary = null;

        /// <summary>
        /// Gets a parameter name for a parameter at the index (position) specified.
        /// </summary>
        /// <param name="index">the zero based index (position) of the parameter</param>
        /// <remarks>Infrastructure for insert statement generation.</remarks>
        protected static string GetParameterName(int index)
        {
            if (index < 0 || index + 1 > 400)
                throw new IndexOutOfRangeException();

            if (_paramDictionary.IsNull())
            {
                _paramDictionary = new List<string>();
                for (int i = 1; i <= 400; i++)
                {
                    _paramDictionary.Add(ParamLetters[(int)Math.Ceiling((i / 24.0) - 1)]
                        + ParamLetters[i - (int)(Math.Ceiling((i / 24.0) - 1) * 24 + 1)]);
                }
                _paramDictionary.Remove("AS");
                _paramDictionary.Remove("BY");
                _paramDictionary.Remove("IF");
                _paramDictionary.Remove("IN");
                _paramDictionary.Remove("IS");
                _paramDictionary.Remove("ON");
                _paramDictionary.Remove("OR");
                _paramDictionary.Remove("TO");
            }

            return _paramDictionary[index];
        }

        #endregion

    }
}
