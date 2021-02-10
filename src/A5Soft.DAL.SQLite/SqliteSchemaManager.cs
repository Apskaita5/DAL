using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using A5Soft.DAL.Core;
using A5Soft.DAL.Core.DbSchema;
using A5Soft.DAL.Core.MicroOrm;

namespace A5Soft.DAL.SQLite
{
    /// <summary>
    /// Manages SQLite database schema (creates, drops, extracts schema, checks for errors against gauge schema, clones database)
    /// </summary>
    public class SqliteSchemaManager : SchemaManagerBase
    {

        /// <summary>
        /// Gets an id of the concrete SQL implementation, i.e. sqlite.
        /// The id is used to check for SqlAgent implementation mismatch.
        /// </summary>
        public override string SqlImplementationId => Extensions.SqliteImplementationId;

        /// <summary>
        /// Gets a typed (native) SQLiteAgent.
        /// </summary>
        private SqliteAgent MyAgent => (SqliteAgent)Agent;


        /// <summary>
        /// Creates a new SQLite database schema manager.
        /// </summary>
        /// <param name="agent">an SQLite agent to use for schema management</param>
        public SqliteSchemaManager(SqliteAgent agent) : base(agent) { }


        #region GetDbSchemaAsync Implementation

        /// <summary>
        /// Gets a <see cref="Schema">DbSchema</see> instance (a canonical database description) 
        /// for the current database.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot get schema while a transaction is in progress.</exception>
        /// <exception cref="InvalidOperationException">Database is not set, cannot get schema.</exception>
        public override async Task<Schema> GetDbSchemaAsync(CancellationToken cancellationToken = default)
        {
            if (Agent.IsTransactionInProgress) throw new InvalidOperationException(
                Properties.Resources.DbSchemaExceptionCannotGetInTransaction);

            var result = new Schema { Tables = new List<TableSchema>() };

            var conn = await MyAgent.OpenConnectionAsync().ConfigureAwait(false);

            try
            {

                var indexData = await MyAgent.FetchUsingConnectionAsync(conn,
                    "SELECT name, tbl_name, sql FROM sqlite_master WHERE type='index' AND NOT sql IS NULL;",
                    cancellationToken).ConfigureAwait(false);

                var tablesData = await MyAgent.FetchUsingConnectionAsync(conn,
                    "SELECT name, sql FROM sqlite_master WHERE type='table' AND NOT name LIKE 'sqlite_%';",
                    cancellationToken).ConfigureAwait(false);

                foreach (var row in tablesData.Rows)
                {
                    result.Tables.Add(await GetDbTableSchemaAsync(conn, row, indexData, cancellationToken));
                }
            }
            finally
            {
                conn.CloseAndDispose();
            }

            return result;
        }

        private async Task<TableSchema> GetDbTableSchemaAsync(SQLiteConnection conn, LightDataRow tableStatusRow,
            LightDataTable indexData, CancellationToken cancellationToken)
        {
            var result = new TableSchema
            {
                Name = tableStatusRow.GetString(0).Trim(),
                Fields = new List<FieldSchema>()
            };

            var indexes = indexData.Rows.Where(
                row => row.GetStringOrDefault(1, string.Empty).EqualsByConvention(result.Name)).ToList();

            var foreignKeys = await MyAgent.FetchUsingConnectionAsync(conn, 
                $"PRAGMA foreign_key_list({result.Name});", cancellationToken)
                .ConfigureAwait(false);

            var fieldsData = await MyAgent.FetchUsingConnectionAsync(conn, 
                $"PRAGMA table_info({result.Name});", cancellationToken)
                .ConfigureAwait(false);

            var createSql = tableStatusRow.GetString(1);
            createSql = createSql.Replace("[", "").Replace("]", "")
                .Substring(createSql.IndexOf("(") + 1);
            createSql = createSql.Substring(0, createSql.Length - 1);

            foreach (var row in fieldsData.Rows)
            {
                result.Fields.Add(this.GetDbFieldSchema(row, indexes, foreignKeys,
                    createSql.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries), result.Name));
            }

            return result;
        }

        private FieldSchema GetDbFieldSchema(LightDataRow fieldStatusRow,
            List<LightDataRow> indexes, LightDataTable foreignKeys, string[] createTableSql,
            string tableName)
        {
            var result = new FieldSchema
            {
                Name = fieldStatusRow.GetString(1).Trim(),
                NotNull = (fieldStatusRow.GetInt32(3) > 0),
                Unsigned = false,
                Description = string.Empty,
            };

            result.Autoincrement = createTableSql.Any(line => line.Trim().StartsWith(
                result.Name + " ", StringComparison.OrdinalIgnoreCase) 
            && line.ContainsByConvention("AUTOINCREMENT"));

            var rawType = fieldStatusRow.GetString(2).Trim();

            result.DataType = rawType.GetBaseDataType();

            var typeDetails = "";
            if (rawType.Contains("("))
                typeDetails = rawType.Substring(rawType.IndexOf("(", StringComparison.Ordinal) + 1,
                    rawType.IndexOf(")", StringComparison.Ordinal)
                    - rawType.IndexOf("(", StringComparison.Ordinal) - 1)
                    .Trim()
                    .Replace("`", "").Replace("'", "").Replace("\"", "");

            if (!string.IsNullOrEmpty(typeDetails) && (result.DataType == DbDataType.Char
                || result.DataType == DbDataType.VarChar))
            {
                result.Length = 255;
                if (int.TryParse(typeDetails, out int length)) result.Length = length;
            }

            if (fieldStatusRow.GetInt32(5) > 0)
            {
                result.IndexType = IndexType.Primary;
                if (FindForeignKey(result, tableName, foreignKeys, createTableSql))
                    result.IndexType = IndexType.ForeignPrimary;
            }
            else
            {
                if (!FindForeignKey(result, tableName, foreignKeys, createTableSql))
                {
                    if (!FindIndex(result, tableName, indexes)) result.IndexType = IndexType.None;
                }
            }

            return result;
        }

        private static bool FindForeignKey(FieldSchema schema, string table,
            LightDataTable foreignKeys, string[] createTableSql)
        {
            var row = foreignKeys.Rows.FirstOrDefault(dr => 
                dr.GetString(3).EqualsByConvention(schema.Name));

            if (row.IsNull()) return false;

            schema.IndexType = IndexType.ForeignKey;

            schema.IndexName = $"{table}_{schema.Name}_fk";

            schema.RefTable = row.GetString(2).Trim();
            schema.RefField = row.GetString(4).Trim();

            var action = row.GetString(5);
            if (action.EqualsByConvention(Extensions.NativeActionType_SetNull))
                schema.OnUpdateForeignKey = ForeignKeyActionType.SetNull;
            else if (action.EqualsByConvention(Extensions.NativeActionType_Cascade))
                schema.OnUpdateForeignKey = ForeignKeyActionType.Cascade;
            else schema.OnUpdateForeignKey = ForeignKeyActionType.Restrict;

            action = row.GetString(6);
            if (action.EqualsByConvention(Extensions.NativeActionType_SetNull))
                schema.OnDeleteForeignKey = ForeignKeyActionType.SetNull;
            else if (action.EqualsByConvention(Extensions.NativeActionType_Cascade))
                schema.OnDeleteForeignKey = ForeignKeyActionType.Cascade;
            else schema.OnDeleteForeignKey = ForeignKeyActionType.Restrict;

            return true;
        }

        private static bool FindIndex(FieldSchema schema, string table, List<LightDataRow> indexes)
        {
            var row = indexes.FirstOrDefault(dr => dr.GetString(2).ContainsByConvention(
                $"ON {table.Trim()}({schema.Name.Trim()})"));

            if (row.IsNull()) return false;

            schema.IndexName = row.GetString(0).Trim();
            if (row.GetString(2).ContainsByConvention("UNIQUE"))
            {
                schema.IndexType = IndexType.Unique;
            }
            else
            {
                schema.IndexType = IndexType.Simple;
            }

            return true;
        }

        #endregion

        #region GetDbSchemaErrors Implementation

        /// <summary>
        /// Compares the actualSchema definition to the gaugeSchema definition and returns
        /// a list of DbSchema errors, i.e. inconsistencies found and SQL statements to repair them.
        /// </summary>
        /// <param name="gaugeSchema">the gauge schema definition to compare the actualSchema against</param>
        /// <param name="actualSchema">the schema to check for inconsistencies (and repair)</param>
        public override List<SchemaError> GetDbSchemaErrors(Schema gaugeSchema, Schema actualSchema)
        {
            if (gaugeSchema.IsNull()) throw new ArgumentNullException(nameof(gaugeSchema));
            if (actualSchema.IsNull()) throw new ArgumentNullException(nameof(actualSchema));
            if (Agent.IsTransactionInProgress) throw new InvalidOperationException(
                Properties.Resources.DbSchemaErrorsExceptionCannotGetInTransaction);

            var result = new List<SchemaError>();

            foreach (var gaugeTable in gaugeSchema.Tables)
            {
                var gaugeTableFound = false;

                foreach (var actualTable in actualSchema.Tables)
                {
                    if (actualTable.Name.EqualsByConvention(gaugeTable.Name))
                    {
                        result.AddRange(GetDbTableSchemaErrors(gaugeTable, actualTable));
                        gaugeTableFound = true;
                        break;
                    }
                }

                if (!gaugeTableFound)
                {
                    result.Add(GetDbSchemaError(SchemaErrorType.TableMissing, string.Format(
                        Properties.Resources.TableMissingErrorDescription, gaugeTable.Name),
                        gaugeTable.Name, gaugeTable.GetCreateTableStatements(MyAgent).ToArray()));
                }
            }

            foreach (var actualTable in actualSchema.Tables)
            {
                var actualTableFound = gaugeSchema.Tables.Any(
                    gaugeTable => actualTable.Name.EqualsByConvention(gaugeTable.Name));

                if (!actualTableFound)
                {
                    result.Add(GetDbSchemaError(SchemaErrorType.TableObsolete, string.Format(
                        Properties.Resources.TableObsoleteErrorDescription, actualTable.Name),
                        actualTable.Name, actualTable.GetDropTableStatements(MyAgent).ToArray()));
                }
            }

            return result;
        }

        private List<SchemaError> GetDbTableSchemaErrors(TableSchema gaugeSchema,
            TableSchema actualSchema)
        {
            var result = new List<SchemaError>();

            foreach (var gaugeField in gaugeSchema.Fields)
            {
                var gaugeFieldFound = false;

                foreach (var actualField in actualSchema.Fields)
                {
                    if (gaugeField.Name.EqualsByConvention(actualField.Name))
                    {
                        var schemasMatch = gaugeField.FieldSchemaMatch(actualField);
                        var indexMatch = gaugeField.FieldIndexMatch(actualField);

                        if (!schemasMatch)
                        {
                            result.Add(GetUnrepairableDbSchemaError(SchemaErrorType.FieldDefinitionObsolete,
                                string.Format(Properties.Resources.FieldObsoleteErrorDescription,
                                actualSchema.Name, actualField.Name, actualField.GetFieldDefinition(false, MyAgent),
                                gaugeField.GetFieldDefinition(false, MyAgent)), gaugeSchema.Name, gaugeField.Name));
                        }

                        if (!indexMatch)
                        {
                            if (actualField.IndexType == IndexType.None &&
                                (gaugeField.IndexType == IndexType.Simple ||
                                 gaugeField.IndexType == IndexType.Unique))
                            {
                                result.Add(GetDbSchemaError(SchemaErrorType.IndexMissing,
                                    string.Format(Properties.Resources.IndexMissingErrorDescription,
                                    gaugeField.IndexName, actualSchema.Name, actualField.Name),
                                    actualSchema.Name, actualField.Name,
                                    gaugeField.GetAddIndexStatements(actualSchema.Name, MyAgent).ToArray()));
                            }
                            else if ((actualField.IndexType == IndexType.Simple &&
                                gaugeField.IndexType == IndexType.Unique) ||
                                (actualField.IndexType == IndexType.Unique &&
                                gaugeField.IndexType == IndexType.Simple))
                            {
                                var statements = actualField.GetDropIndexStatements(MyAgent);
                                statements.AddRange(gaugeField.GetAddIndexStatements(actualSchema.Name, MyAgent));
                                result.Add(GetDbSchemaError(SchemaErrorType.IndexObsolete,
                                    string.Format(Properties.Resources.IndexObsoleteErrorDescription,
                                    gaugeField.IndexName, actualSchema.Name, actualField.Name),
                                    actualSchema.Name, actualField.Name, statements.ToArray()));
                            }
                            else if ((actualField.IndexType == IndexType.Simple ||
                                      actualField.IndexType == IndexType.Unique) &&
                                     gaugeField.IndexType == IndexType.None)
                            {
                                var statements = actualField.GetDropIndexStatements(MyAgent);
                                statements.AddRange(gaugeField.GetAddIndexStatements(actualSchema.Name, MyAgent));
                                result.Add(GetDbSchemaError(SchemaErrorType.IndexObsolete,
                                    string.Format(Properties.Resources.IndexRedundantErrorDescription,
                                    gaugeField.IndexName, actualSchema.Name, actualField.Name),
                                    actualSchema.Name, actualField.Name,
                                    actualField.GetDropIndexStatements(MyAgent).ToArray()));
                            }
                            else
                            {
                                result.Add(GetUnrepairableDbSchemaError(SchemaErrorType.IndexObsolete,
                                    string.Format(Properties.Resources.IndexObsoleteUnreparableErrorDescription,
                                    gaugeField.IndexName, actualSchema.Name, actualField.Name),
                                    actualSchema.Name, actualField.Name));
                            }
                        }

                        gaugeFieldFound = true;
                        break;
                    }
                }

                if (!gaugeFieldFound)
                {
                    if (gaugeField.DataType.ToBaseType() == DbDataType.Blob && gaugeField.NotNull)
                    {
                        result.Add(GetUnrepairableDbSchemaError(SchemaErrorType.FieldMissing,
                            string.Format(Properties.Resources.FieldMissingUnreparableErrorDescription,
                            gaugeField.Name, gaugeSchema.Name), gaugeSchema.Name, gaugeField.Name));
                    }
                    else
                    {
                        result.Add(GetDbSchemaError(SchemaErrorType.FieldMissing,
                            string.Format(Properties.Resources.FieldMissingErrorDescription,
                            gaugeField.Name, gaugeSchema.Name), gaugeSchema.Name, gaugeField.Name,
                            gaugeField.GetAddFieldStatements(gaugeSchema.Name, MyAgent).ToArray()));
                    }
                }
            }

            foreach (var actualField in actualSchema.Fields)
            {
                if (!gaugeSchema.Fields.Any(gaugeField => actualField.Name.EqualsByConvention(gaugeField.Name)))
                {
                    result.Add(GetUnrepairableDbSchemaError(SchemaErrorType.FieldObsolete,
                        string.Format(Properties.Resources.FieldRedundantUnreparableErrorDescription, actualField.Name,
                        actualSchema.Name), actualSchema.Name, actualField.Name));
                }
            }

            return result;
        }

        #endregion

        /// <summary>
        /// Gets an SQL script to create a database for the dbSchema specified.
        /// </summary>
        /// <param name="schema">the database schema to get the create database script for</param>
        public override string GetCreateDatabaseSql(Schema schema)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (schema.Tables.Count < 1) throw new ArgumentException(
                Properties.Resources.DatabaseCreateExceptionNoTables, nameof(schema));

            var script = schema.GetTablesInCreateOrder().Aggregate(new List<string>(),
                            (seed, table) =>
                            {
                                seed.AddRange(table.GetCreateTableStatements(MyAgent));
                                return seed;
                            });
            return string.Join(Environment.NewLine, script.ToArray());
        }

        /// <summary>
        /// A method that should do the actual new database creation.
        /// </summary>
        /// <param name="schema">a DbSchema to use for the new database</param>
        /// <remarks>After creating a new database the <see cref="SqlAgentBase.CurrentDatabase">CurrentDatabase</see>
        /// property should be set to the new database name.</remarks>
        /// <exception cref="ArgumentException">Database schema should contain at least one table.</exception>
        public override Task CreateDatabaseAsync(Schema schema)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (Agent.IsTransactionInProgress)
                throw new InvalidOperationException(Properties.Resources.CreateDatabaseExceptionTransactionInProgress);
            if (schema.Tables.Count < 1)
                throw new ArgumentException(Properties.Resources.DatabaseCreateExceptionNoTables, nameof(schema));

            if (!schema.AllIndexesUnique()) schema.SetSafeIndexNames();

            var script = schema.GetTablesInCreateOrder().Aggregate(new List<string>(),
                            (seed, table) =>
                            {
                                seed.AddRange(table.GetCreateTableStatements(MyAgent));
                                return seed;
                            });

            return Agent.ExecuteCommandBatchAsync(script.ToArray());
        }

        /// <summary>
        /// Drops (deletes) the database specified.
        /// </summary>
        /// <exception cref="ArgumentNullException">Parameter databaseName is not specified.</exception>
        /// <exception cref="InvalidOperationException">Cannot drop database while transaction is in progress.</exception>
        /// <exception cref="ArgumentException">Path to the database contains one or more invalid characters 
        /// as defined by InvalidPathChars.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid 
        /// (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">The specified file is in use.</exception>
        /// <exception cref="NotSupportedException">path is in an invalid format</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed 
        /// the system-defined maximum length. For example, on Windows-based platforms, 
        /// paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission
        /// or the path specified a read-only file.</exception>
        public override async Task DropDatabaseAsync()
        {
            if (Agent.IsTransactionInProgress) throw new InvalidOperationException(
                Properties.Resources.DropDatabaseExceptionTransactionInProgress);

            using (var stream = new FileStream(Agent.CurrentDatabase, FileMode.Open, FileAccess.Read, FileShare.None,
                4096, FileOptions.DeleteOnClose | FileOptions.Asynchronous))
            {
                await stream.FlushAsync().ConfigureAwait(false);
            }
        }

        #region Database Cloning Methods

        /// <summary>
        /// Copies table data from the current SqlAgent instance to the target SqlAgent instance.
        /// </summary>
        /// <param name="schema">a schema of the database to copy the data</param>
        /// <param name="targetManager">the target schema manager to copy the data to</param>
        /// <remarks>Required for <see cref="SqlAgentBase.CloneDatabase">CloneDatabase</see> infrastructure.
        /// Basicaly iterates tables, selects data, creates an IDataReader for the table and passes it to the 
        /// <see cref="InsertTableData">InsertTableData</see> method of the target SqlAgent.</remarks>
        protected override async Task CopyData(Schema schema, SchemaManagerBase targetManager,
            IProgress<CloneProgressArgs> progress, CancellationToken ct)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (targetManager.IsNull()) throw new ArgumentNullException(nameof(targetManager));

            var currentStatement = string.Empty;

            using (var conn = await MyAgent.OpenConnectionAsync().ConfigureAwait(false))
            {
                try
                {
                    using (var command = new SQLiteCommand())
                    {
                        command.Connection = conn;
                        command.CommandTimeout = Agent.QueryTimeOut;

                        progress?.Report(new CloneProgressArgs(CloneProgressArgs.Stage.FetchingRowCount, 
                            string.Empty, 0));

                        long totalRowCount = 0;
                        foreach (var table in schema.Tables)
                        {
                            currentStatement = $"SELECT COUNT(*) FROM {table.Name.ToConventional(Agent)};";
                            command.CommandText = currentStatement;
                            totalRowCount += (long)await command.ExecuteScalarAsync()
                                .ConfigureAwait(false);
                            if (CloneCanceled(progress, ct)) return;
                        }

                        long currentRow = 0;
                        int currentProgress = 0;

                        foreach (var table in schema.Tables)
                        {
                            var fields = string.Join(", ", table.Fields.Select(
                                field => field.Name.ToConventional(Agent)).ToArray());

                            currentStatement = $"SELECT {fields} FROM {table.Name.ToConventional(Agent)};";
                            command.CommandText = currentStatement;

                            using (IDataReader reader = await command.ExecuteReaderAsync()
                                .ConfigureAwait(false))
                            {
                                currentRow = await CallInsertTableDataAsync(targetManager, table, reader,
                                    totalRowCount, currentRow, currentProgress, progress, ct)
                                    .ConfigureAwait(false);
                                if (currentRow < 0) break;
                                if (totalRowCount > 0) currentProgress = (int)(100 * currentRow / (double)totalRowCount);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex.WrapSqlException(currentStatement);
                }
                finally
                {
                    if (conn != null && conn.State != ConnectionState.Closed)
                    {
                        try { conn.Close(); }
                        catch (Exception) { }
                    }
                }
            }
        }

        /// <summary>
        /// Disables foreign key checks for the current transaction.
        /// </summary>
        /// <remarks>Required for <see cref="SqlAgentBase.CloneDatabase">CloneDatabase</see> infrastructure.</remarks>
        protected override async Task DisableForeignKeysForCurrentTransactionAsync()
        {
            if (!Agent.IsTransactionInProgress) throw new InvalidOperationException(
                Properties.Resources.DisableForeignKeysExceptionTransactionNull);
            await Agent.ExecuteCommandRawAsync("PRAGMA foreign_keys = OFF;", null)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Inserts table data from the reader to the current SqlAgent instance,
        /// </summary>
        /// <param name="table">a schema of the table to insert the data to</param>
        /// <param name="reader">an IDataReader to read the table data from.</param>
        /// <remarks>Required for <see cref="SqlAgentBase.CloneDatabase">CloneDatabase</see> infrastructure.
        /// The insert is performed using a transaction that is already initiated by the 
        /// <see cref="SqlAgentBase.CloneDatabase">CloneDatabase</see>.</remarks>
        protected override async Task<long> InsertTableDataAsync(TableSchema table, IDataReader reader,
            long totalRowCount, long currentRow, int currentProgress, IProgress<CloneProgressArgs> progress,
            CancellationToken ct)
        {
            var fields = table.Fields.Select(field => field.Name.ToConventional(Agent)).ToList();

            var paramPrefixedNames = new List<string>();
            var paramNames = new List<string>();
            for (int i = 0; i < fields.Count; i++)
            {
                var paramName = GetParameterName(i);
                paramNames.Add(paramName);
                paramPrefixedNames.Add(Extensions.ParamPrefix + paramName);
            }

            var insertStatement = string.Format("INSERT INTO {0}({1}) VALUES({2});",
                table.Name.ToConventional(Agent), string.Join(", ", fields.ToArray()),
                string.Join(", ", paramPrefixedNames.ToArray()));

            while (reader.Read())
            {
                var paramValues = new List<SqlParam>();
                for (int i = 0; i < fields.Count; i++)
                {
                    paramValues.Add(SqlParam.Create(paramNames[i], reader.GetValue(i)));
                }

                await Agent.ExecuteCommandRawAsync(insertStatement, paramValues.ToArray())
                    .ConfigureAwait(false);

                if (CloneCanceled(progress, ct)) return -1;

                currentRow += 1;

                if (progress != null && totalRowCount > 0)
                {
                    var recalculatedProgress = (int)(100 * currentRow / (double)totalRowCount);
                    if (recalculatedProgress > currentProgress)
                    {
                        currentProgress = recalculatedProgress;
                        progress.Report(new CloneProgressArgs(CloneProgressArgs.Stage.CopyingData,
                            table.Name, currentProgress));
                    }
                }
            }

            return currentRow;
        }

        #endregion

    }
}
