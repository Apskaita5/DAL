﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using A5Soft.DAL.Core;
using A5Soft.DAL.Core.DbSchema;
using A5Soft.DAL.Core.MicroOrm;
using MySqlConnector;

namespace A5Soft.DAL.MySql
{
    /// <summary>
    /// Manages MySql database schema (creates, drops, extracts schema, checks for errors against gauge schema, clones database)
    /// </summary>
    public class MySqlSchemaManager : SchemaManagerBase
    {

        #region Properties

        private const string DefaultEngine = "InnoDB";
        private const string DefaultCharset = "utf8";

        private string _engine = DefaultEngine;
        private string _charset = DefaultCharset;


        /// <summary>
        /// Gets or sets the SQL engine to use when creating a database. (default - InnoDB)
        /// </summary>
        public string Engine
        {
            get { return _engine.IsNullOrWhiteSpace() ? DefaultEngine : _engine?.Trim(); }
            set { _engine = value?.Trim() ?? throw new ArgumentNullException(nameof(value), 
                "Engine cannot be null."); }
        }

        /// <summary>
        /// Gets or sets the default charset to use when creating a database. (default - utf8)
        /// </summary>
        public string Charset
        {
            get { return _charset.IsNullOrWhiteSpace() ? DefaultCharset : _charset?.Trim(); }
            set { _charset = value?.Trim() ?? throw new ArgumentNullException(nameof(value),
                "Charset cannot be null."); }
        }

        /// <summary>
        /// Gets an id of the concrete SQL implementation, i.e. mysql.
        /// The id is used to check for SqlAgent implementation mismatch.
        /// </summary>
        public override string SqlImplementationId => Extensions.MySqlImplementationId;

        /// <summary>
        /// Gets a typed (native) MySqlAgent.
        /// </summary>
        private MySqlAgent MyAgent => (MySqlAgent)Agent;

        #endregion


        /// <summary>
        /// Creates a new MySql database schema manager.
        /// </summary>
        /// <param name="agent">MySql agent to use for schema management.</param>
        public MySqlSchemaManager(MySqlAgent agent) : base(agent) { }


        #region GetDbSchemaAsync Implementation

        /// <summary>
        /// Gets a <see cref="Schema">DbSchema</see> instance (a canonical database description) 
        /// for the current database.
        /// </summary>  
        /// <param name="cancellationToken">a cancellation token (if any)</param>
        public override async Task<Schema> GetDbSchemaAsync(CancellationToken cancellationToken = default)
        {
            if (Agent.CurrentDatabase.IsNullOrWhiteSpace())
                throw new InvalidOperationException(Properties.Resources.GetSchemaExceptionNullDatabase);
            if (Agent.IsTransactionInProgress)
                throw new InvalidOperationException(Properties.Resources.GetSchemaExceptionTransactionInProgress);

            var result = new Schema();

            var conn = await MyAgent.OpenConnectionAsync().ConfigureAwait(false);

            try
            {
                var dbData = await MyAgent.FetchUsingConnectionAsync(conn, 
                    "SELECT @@character_set_database, @@default_storage_engine;",
                    cancellationToken)
                    .ConfigureAwait(false);
                if (dbData.Rows.Count > 0)
                {
                    result.Description = string.Format(Properties.Resources.DbSchemaDescription,
                        Agent.CurrentDatabase, dbData.Rows[0].GetString(1), dbData.Rows[0].GetString(0));
                }

                var indexDictionary = await GetIndexesAsync(conn, cancellationToken)
                    .ConfigureAwait(false);
                var fkDictionary = await GetForeignKeysAsync(conn, cancellationToken)
                    .ConfigureAwait(false);

                result.Tables = new List<TableSchema>();

                var tablesData = await MyAgent.FetchUsingConnectionAsync(conn, "SHOW TABLE STATUS;",
                    cancellationToken)
                    .ConfigureAwait(false);
                foreach (var row in tablesData.Rows)
                {
                    result.Tables.Add(await GetDbTableSchemaAsync(conn, row, indexDictionary, fkDictionary,
                        cancellationToken)
                        .ConfigureAwait(false));
                }
            }
            finally
            {
                await conn.CloseAndDisposeAsync().ConfigureAwait(false);
            }

            return result;
        }

        private async Task<TableSchema> GetDbTableSchemaAsync(MySqlConnection conn, 
            LightDataRow tableStatusRow, Dictionary<string, Dictionary<string, string>> indexDictionary,
            Dictionary<string, Dictionary<string, ForeignKeyData>> fkDictionary, 
            CancellationToken cancellationToken)
        {
            var result = new TableSchema
            {
                Name = tableStatusRow.GetString(0).Trim(),
                Description = string.Format(Properties.Resources.DbTableSchemaDescription,
                    tableStatusRow.GetString(0)?.Trim(), tableStatusRow.GetString(17)?.Trim(),
                    tableStatusRow.GetString(1)?.Trim(), tableStatusRow.GetString(14)?.Trim()),
                Fields = new List<FieldSchema>()
            };

            Dictionary<string, string> tableIndexDictionary = null;
            Dictionary<string, ForeignKeyData> tableFkDictionary = null;
            if (indexDictionary.ContainsKey(result.Name))
                tableIndexDictionary = indexDictionary[result.Name];
            if (fkDictionary.ContainsKey(result.Name))
                tableFkDictionary = fkDictionary[result.Name];

            var fieldsData = await MyAgent.FetchUsingConnectionAsync(conn,
                $"SHOW FULL COLUMNS FROM {result.Name};", cancellationToken)
                .ConfigureAwait(false);

            foreach (var row in fieldsData.Rows)
            {
                result.Fields.Add(this.GetDbFieldSchema(row, tableIndexDictionary, tableFkDictionary));
            }

            return result;
        }

        private FieldSchema GetDbFieldSchema(LightDataRow fieldStatusRow,
            Dictionary<string, string> indexDictionary, 
            Dictionary<string, ForeignKeyData> fkIndex)
        {
            var result = new FieldSchema
            {
                Name = fieldStatusRow.GetString(0).Trim(),
                NotNull = (fieldStatusRow.GetString(3).EqualsByConvention("no")),
                Unsigned = (fieldStatusRow.GetString(1).ContainsByConvention("unsigned")),
                Autoincrement = (fieldStatusRow.GetString(6).ContainsByConvention("auto_increment")),
                Description = fieldStatusRow.GetStringOrDefault(8).Trim()
            };

            var collation = fieldStatusRow.GetStringOrDefault(2);
            if (collation.EqualsByConvention("ascii_bin"))
                result.CollationType = FieldCollationType.ASCII_Binary;
            else if (collation.EqualsByConvention("ascii_general_ci"))
                result.CollationType = FieldCollationType.ASCII_CaseInsensitive;

            var rawType = fieldStatusRow.GetString(1).Trim();
            result.DataType = GetFieldType(rawType);

            var typeDetails = string.Empty;
            if (rawType.Contains("("))
                typeDetails = rawType.Substring(rawType.IndexOf("(", StringComparison.Ordinal) + 1,
                    rawType.IndexOf(")", StringComparison.Ordinal)
                    - rawType.IndexOf("(", StringComparison.Ordinal) - 1)
                    .Trim()
                    .Replace("`", "").Replace("'", "").Replace("\"", "");

            if (result.DataType == DbDataType.Char || result.DataType == DbDataType.VarChar)
            {
                if (int.TryParse(typeDetails, out int length)) result.Length = length;
            }
            else if (result.DataType == DbDataType.Decimal)
            {
                var intValues = typeDetails.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (intValues.Length > 1)
                {
                    if (int.TryParse(intValues[1].Trim(), out int length))
                        result.Length = length;
                }
            }
            else if (result.DataType == DbDataType.Enum)
            {
                result.EnumValues = typeDetails;
            }

            if (fieldStatusRow.GetString(4).EqualsByConvention("pri"))
            {
                result.IndexType = IndexType.Primary;
                if (fkIndex != null && fkIndex.ContainsKey(result.Name))
                {
                    result.IndexType = IndexType.ForeignPrimary;
                    fkIndex[result.Name].SetSchema(result);
                }
            }
            else if (fkIndex != null && fkIndex.ContainsKey(result.Name))
            {
                result.IndexType = IndexType.ForeignKey;
                fkIndex[result.Name].SetSchema(result);
            }
            else if (indexDictionary != null && indexDictionary.ContainsKey(result.Name))
            {
                result.IndexType = IndexType.Simple;
                if (fieldStatusRow.GetString(4).EqualsByConvention("uni"))
                    result.IndexType = IndexType.Unique;
                result.IndexName = indexDictionary[result.Name];
            }

            return result;
        }

        private static DbDataType GetFieldType(string definition)
        {
            var nativeName = definition;
            if (nativeName.Contains("("))
                nativeName = nativeName.Substring(0, nativeName.IndexOf("(", StringComparison.Ordinal));
            if (nativeName.Contains(" "))
                nativeName = nativeName.Substring(0, nativeName.IndexOf(" ", StringComparison.Ordinal));
            nativeName = nativeName.Trim().ToLowerInvariant();

            switch (nativeName)
            {
                case "blob":
                    return DbDataType.Blob;
                case "longblob":
                    return DbDataType.BlobLong;
                case "mediumblob":
                    return DbDataType.BlobMedium;
                case "tinyblob":
                    return DbDataType.BlobTiny;
                case "char":
                    return DbDataType.Char;
                case "date":
                    return DbDataType.Date;
                case "datetime":
                    return DbDataType.DateTime;
                case "decimal":
                    return DbDataType.Decimal;
                case "double":
                    return DbDataType.Double;
                case "enum":
                    return DbDataType.Enum;
                case "float":
                    return DbDataType.Float;
                case "int":
                    return DbDataType.Integer;
                case "integer":
                    return DbDataType.Integer;
                case "bigint":
                    return DbDataType.IntegerBig;
                case "mediumint":
                    return DbDataType.IntegerMedium;
                case "smallint":
                    return DbDataType.IntegerSmall;
                case "tinyint":
                    return DbDataType.IntegerTiny;
                case "real":
                    return DbDataType.Real;
                case "text":
                    return DbDataType.Text;
                case "longtext":
                    return DbDataType.TextLong;
                case "mediumtext":
                    return DbDataType.TextMedium;
                case "time":
                    return DbDataType.Time;
                case "timestamp":
                    return DbDataType.TimeStamp;
                case "varchar":
                    return DbDataType.VarChar;
                default:
                    throw new NotImplementedException(string.Format(
                        Properties.Resources.NativeTypeUnknownException, definition));
            }
        }

        private async Task<Dictionary<string, Dictionary<string, string>>> GetIndexesAsync(
            MySqlConnection conn, CancellationToken cancellationToken)
        {
            var indexTable = await MyAgent.FetchUsingConnectionAsync(conn, @"
                    SELECT s.TABLE_NAME, s.COLUMN_NAME, s.INDEX_NAME, s.NON_UNIQUE
                    FROM INFORMATION_SCHEMA.STATISTICS s
                    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE c ON c.TABLE_SCHEMA = s.TABLE_SCHEMA
                    AND c.TABLE_NAME = s.TABLE_NAME AND c.COLUMN_NAME = s.COLUMN_NAME
                    WHERE s.INDEX_NAME <> 'PRIMARY' AND c.CONSTRAINT_NAME IS NULL AND s.TABLE_SCHEMA = DATABASE();",
                    cancellationToken).ConfigureAwait(false);

            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in indexTable.Rows)
            {
                var current = GetOrCreateTableDictionary(result, row.GetString(0).Trim());
                if (!current.ContainsKey(row.GetString(1).Trim()))
                    current.Add(row.GetString(1).Trim(), row.GetString(2).Trim());
            }

            return result;
        }

        private Dictionary<string, string> GetOrCreateTableDictionary(
            Dictionary<string, Dictionary<string, string>> baseDictionary, string tableName)
        {
            if (baseDictionary.ContainsKey(tableName)) return baseDictionary[tableName];

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            baseDictionary.Add(tableName, result);
            
            return result;
        }

        private async Task<Dictionary<string, Dictionary<string, ForeignKeyData>>> GetForeignKeysAsync(
            MySqlConnection conn, CancellationToken cancellationToken)
        {

            // because TABLE_CONSTRAINTS.CONSTRAINT_NAME and KEY_COLUMN_USAGE.CONSTRAINT_NAME
            // have different collations for MySql server 8, cannot join
            var fkRefList = (await MyAgent.FetchUsingConnectionAsync(conn, @"
                SELECT i.CONSTRAINT_NAME FROM information_schema.TABLE_CONSTRAINTS i 
                WHERE i.CONSTRAINT_TYPE = 'FOREIGN KEY' AND i.TABLE_SCHEMA = DATABASE();", cancellationToken))
                .Rows.Select(r => r.GetString(0)).ToList();

            var indexTable = await MyAgent.FetchUsingConnectionAsync(conn, @"
                SELECT TABLE_NAME, COLUMN_NAME, CONSTRAINT_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME 
                FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = DATABASE() AND NOT REFERENCED_TABLE_SCHEMA IS NULL;",
                cancellationToken).ConfigureAwait(false);

            var result = new Dictionary<string, Dictionary<string, ForeignKeyData>>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var row in indexTable.Rows)
            {
                if (fkRefList.Contains(row.GetString(2).Trim()))
                {
                    var current = GetOrCreateFKTableDictionary(result,
                        row.GetString(0).Trim());
                    if (!current.ContainsKey(row.GetString(1).Trim()))
                    {
                        var fkInfo = new ForeignKeyData
                        {
                            Name = row.GetString(2).Trim(),
                            RefTable = row.GetString(3).Trim(),
                            RefField = row.GetString(4).Trim()
                        };
                        current.Add(row.GetString(1).Trim(), fkInfo);
                    }
                }
            }

            foreach (var entry in result)
            {

                var showCreateTable = await MyAgent.FetchUsingConnectionAsync(conn,
                    $"SHOW CREATE TABLE {entry.Key};", cancellationToken)
                    .ConfigureAwait(false);
                var showCreateLines = showCreateTable.Rows[0].GetString(1).Split(new string[] { "," },
                    StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in showCreateLines)
                {
                    if (line.Trim().StartsWith("CONSTRAINT", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var column in entry.Value)
                        {
                            if (line.ContainsByConvention("`" + column.Value.Name + "`")
                                || line.ContainsByConvention(" " + column.Value.Name + " "))
                            {
                                column.Value.OnUpdate = ForeignKeyActionType.Restrict;
                                if (line.ContainsByConvention("ON UPDATE CASCADE"))
                                    column.Value.OnUpdate = ForeignKeyActionType.Cascade;
                                if (line.ContainsByConvention("ON UPDATE SET NULL"))
                                    column.Value.OnUpdate = ForeignKeyActionType.SetNull;

                                column.Value.OnDelete = ForeignKeyActionType.Restrict;
                                if (line.ContainsByConvention("ON DELETE CASCADE"))
                                    column.Value.OnDelete = ForeignKeyActionType.Cascade;
                                if (line.ContainsByConvention("ON DELETE SET NULL"))
                                    column.Value.OnDelete = ForeignKeyActionType.SetNull;

                                break;
                            }
                        }
                    }
                }
            }

            return result;

        }

        private Dictionary<string, ForeignKeyData> GetOrCreateFKTableDictionary(
            Dictionary<string, Dictionary<string, ForeignKeyData>> baseDictionary, string tableName)
        {
            if (baseDictionary.ContainsKey(tableName)) return baseDictionary[tableName];

            var result = new Dictionary<string, ForeignKeyData>(StringComparer.OrdinalIgnoreCase);
            
            baseDictionary.Add(tableName, result);
            
            return result;
        }

        private class ForeignKeyData
        {
            public string Name { get; set; }
            public string RefTable { get; set; }
            public string RefField { get; set; }
            public ForeignKeyActionType OnUpdate { get; set; }
            public ForeignKeyActionType OnDelete { get; set; }

            public void SetSchema(FieldSchema schema)
            {
                schema.IndexName = Name;
                schema.OnUpdateForeignKey = OnUpdate;
                schema.OnDeleteForeignKey = OnDelete;
                schema.RefTable = RefTable;
                schema.RefField = RefField;
            }
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
            if (Agent.IsTransactionInProgress) throw new InvalidOperationException(Properties.Resources.GetSchemaErrorsExceptionTransactionInProgress);

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
                    var applicableEngine = _engine;
                    if (applicableEngine.IsNullOrWhiteSpace()) applicableEngine = DefaultEngine;
                    var applicableCharset = _charset;
                    if (applicableCharset.IsNullOrWhiteSpace()) applicableCharset = DefaultCharset;

                    result.Add(GetDbSchemaError(SchemaErrorType.TableMissing,
                        string.Format(Properties.Resources.DbSchemaErrorTableMissing, gaugeTable.Name),
                        gaugeTable.Name, gaugeTable.GetCreateTableStatements(
                        Agent.CurrentDatabase, applicableEngine, applicableCharset, MyAgent).ToArray()));
                }
            }

            foreach (var actualTable in actualSchema.Tables)
            {
                var actualTableFound = gaugeSchema.Tables.Any(
                    gaugeTable => actualTable.Name.EqualsByConvention(gaugeTable.Name));

                if (!actualTableFound)
                {
                    result.Add(GetDbSchemaError(SchemaErrorType.TableObsolete,
                        string.Format(Properties.Resources.DbSchemaErrorTableObsolete, actualTable.Name),
                        actualTable.Name, actualTable.GetDropTableStatements(Agent.CurrentDatabase, MyAgent).ToArray()));
                }
            }

            return result.OrderBy(e => (int)e.ErrorType).ToList();
        }

        private List<SchemaError> GetDbTableSchemaErrors(TableSchema gaugeSchema, TableSchema actualSchema)
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
                        var statements = new List<string>();
                        var inconsistencyType = SchemaErrorType.FieldDefinitionObsolete;
                        var description = string.Empty;

                        if (!schemasMatch && !indexMatch)
                        {
                            statements.AddRange(actualField.GetDropIndexStatements(Agent.CurrentDatabase, actualSchema.Name, MyAgent));
                            statements.AddRange(gaugeField.GetAlterFieldStatements(Agent.CurrentDatabase, actualSchema.Name, MyAgent));
                            statements.AddRange(gaugeField.GetAddIndexStatements(Agent.CurrentDatabase, actualSchema.Name, MyAgent));
                            description = string.Format(Properties.Resources.DbSchemaErrorFieldAndIndexObsolete,
                                gaugeSchema.Name, actualField.Name);
                        }
                        else if (!schemasMatch)
                        {
                            statements.AddRange(gaugeField.GetAlterFieldStatements(Agent.CurrentDatabase, actualSchema.Name, MyAgent));
                            description = string.Format(Properties.Resources.DbSchemaErrorFieldObsolete,
                                gaugeSchema.Name, actualField.Name);
                        }
                        else if (!indexMatch)
                        {
                            statements.AddRange(actualField.GetDropIndexStatements(Agent.CurrentDatabase, actualSchema.Name, MyAgent));
                            statements.AddRange(gaugeField.GetAddIndexStatements(Agent.CurrentDatabase, actualSchema.Name, MyAgent));
                            inconsistencyType = SchemaErrorType.IndexObsolete;
                            description = string.Format(Properties.Resources.DbSchemaErrorIndexObsolete,
                                gaugeSchema.Name, actualField.Name);
                        }


                        if (!indexMatch || !schemasMatch)
                        {
                            result.Add(GetDbSchemaError(inconsistencyType, description, gaugeSchema.Name,
                                gaugeField.Name, statements.ToArray()));
                        }

                        gaugeFieldFound = true;
                        break;
                    }
                }

                if (!gaugeFieldFound)
                {
                    result.Add(GetDbSchemaError(SchemaErrorType.FieldMissing,
                        string.Format(Properties.Resources.DbSchemaErrorFieldMissing,
                        gaugeField.Name, gaugeSchema.Name), gaugeSchema.Name, gaugeField.Name,
                        gaugeField.GetAddFieldStatements(Agent.CurrentDatabase, gaugeSchema.Name, MyAgent).ToArray()));
                }
            }

            foreach (var actualField in actualSchema.Fields)
            {
                if (!gaugeSchema.Fields.Any(gaugeField => actualField.Name.EqualsByConvention(gaugeField.Name)))
                {
                    result.Add(GetDbSchemaError(SchemaErrorType.FieldObsolete,
                        string.Format(Properties.Resources.DbSchemaErrorFieldRedundant,
                        actualField.Name, actualSchema.Name), actualSchema.Name, actualField.Name,
                        actualField.GetDropFieldStatements(Agent.CurrentDatabase, actualSchema.Name, MyAgent).ToArray()));
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

            var createScript = new List<string>
                {
                    "CREATE DATABASE DoomyDatabaseName CHARACTER SET utf8;",
                    "USE DoomyDatabaseName;"
                };

            foreach (var table in schema.GetTablesInCreateOrder())
            {
                createScript.AddRange(table.GetCreateTableStatements("DoomyDatabaseName",
                    DefaultEngine, DefaultCharset, MyAgent));
            }

            return string.Join(Environment.NewLine, createScript.ToArray());
        }

        /// <summary>
        /// A method that should do the actual new database creation.
        /// </summary>
        /// <param name="schema">a DbSchema to use for the new database</param>
        /// <remarks>After creating a new database the <see cref="SqlAgentBase.CurrentDatabase">CurrentDatabase</see>
        /// property should be set to the new database name.</remarks>
        public override async Task CreateDatabaseAsync(Schema schema)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (Agent.IsTransactionInProgress)
                throw new InvalidOperationException(Properties.Resources.CreateDatabaseExceptionTransactionInProgress);
            if (Agent.CurrentDatabase.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                "CurrentDatabase property (name of the new database) is not set.");

            var applicableEngine = _engine.IsNullOrWhiteSpace() ? DefaultEngine : _engine;
            var applicableCharset = _charset.IsNullOrWhiteSpace() ? DefaultCharset : _charset;

            var createScript = new List<string>
                {
                    $"CREATE DATABASE {Agent.CurrentDatabase.Trim()} CHARACTER SET {applicableCharset};",
                    $"USE {Agent.CurrentDatabase.Trim()};"
                };

            foreach (var table in schema.GetTablesInCreateOrder())
            {
                createScript.AddRange(table.GetCreateTableStatements(Agent.CurrentDatabase,
                    applicableEngine, applicableCharset, MyAgent));
            }

            using (var conn = await MyAgent.OpenConnectionAsync(true))
            {
                using (var command = new MySqlCommand())
                {
                    command.Connection = conn;
                    command.CommandTimeout = Agent.QueryTimeOut;

                    string currentStatement = string.Empty;

                    try
                    {
                        foreach (var statement in createScript)
                        {
                            currentStatement = statement;
                            command.CommandText = statement;
                            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex.WrapSqlException(currentStatement);
                    }
                    finally
                    {
                        if (conn != null)
                        {
                            if (conn.State != ConnectionState.Closed)
                            {
                                try { await conn.CloseAsync().ConfigureAwait(false); }
                                catch (Exception) { }
                            }
                            try { conn.Dispose(); }
                            catch (Exception) { }
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public override async Task InitDatabaseAsync(Schema schema)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (Agent.IsTransactionInProgress)
                throw new InvalidOperationException(Properties.Resources.CreateDatabaseExceptionTransactionInProgress);
            if (Agent.CurrentDatabase.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                "CurrentDatabase property (name of the new database) is not set.");
            if (schema.Tables.Count < 1) throw new ArgumentException(
                "Database gauge schema empty.", nameof(schema));

            if (!(await this.Agent.DatabaseExistsAsync()))
            {
                await CreateDatabaseAsync(schema);
                return;
            }

            if (await this.Agent.DatabaseEmptyAsync())
            {
                var applicableEngine = _engine.IsNullOrWhiteSpace() ? DefaultEngine : _engine;
                var applicableCharset = _charset.IsNullOrWhiteSpace() ? DefaultCharset : _charset;

                var createScript = new List<string>();
                foreach (var table in schema.GetTablesInCreateOrder())
                {
                    createScript.AddRange(table.GetCreateTableStatements(Agent.CurrentDatabase,
                        applicableEngine, applicableCharset, MyAgent));
                }

                await Agent.ExecuteCommandBatchAsync(createScript.ToArray());

                return;
            }

            var dbErrors = await this.GetDbSchemaErrorsAsync(schema);
            if (dbErrors.Any())
            {
                var upgradeScript = new List<string>();
                foreach (var schemaError in dbErrors)
                {
                    if (schemaError.IsRepairable) 
                        upgradeScript.AddRange(schemaError.SqlStatementsToRepair);
                }

                if (upgradeScript.Any()) await Agent.ExecuteCommandBatchAsync(upgradeScript.ToArray());
            }
        }

        /// <summary>
        /// Drops (deletes) the database specified.
        /// </summary>
        public override Task DropDatabaseAsync()
        {
            if (Agent.IsTransactionInProgress)
                throw new InvalidOperationException(Properties.Resources.DropDatabaseExceptionTransactionInProgress);
            if (Agent.CurrentDatabase.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                "CurrentDatabase property (name of the database to delete) is not set.");

            return Agent.ExecuteCommandRawAsync($"DROP DATABASE {Agent.CurrentDatabase};", null);
        }

        #region Database Cloning Methods

        /// <summary>
        /// Copies table data from the current SqlAgent instance to the target SqlAgent instance.
        /// </summary>
        /// <param name="schema">a schema of the database to copy the data</param>
        /// <param name="targetManager">the target Sql schema manager to copy the data to</param>
        /// <remarks>Required for <see cref="ISchemaManager.CloneDatabaseAsync">CloneDatabaseAsync</see> infrastructure.
        /// Basically iterates tables, selects data, creates an IDataReader for the table and passes it to the 
        /// <see cref="InsertTableDataAsync">InsertTableDataAsync</see> method of the target SqlAgent.</remarks>
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
                    using (var command = new MySqlCommand())
                    {

                        command.Connection = conn;
                        command.CommandTimeout = Agent.QueryTimeOut;

                        progress?.Report(new CloneProgressArgs(CloneProgressArgs.Stage.FetchingRowCount, 
                            string.Empty, 0));

                        long totalRowCount = 0;
                        foreach (var table in schema.Tables)
                        {
                            currentStatement = $"SELECT COUNT(*) FROM {table.Name.Trim()};";
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

                            using (var reader = await command.ExecuteReaderAsync()
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
                    await conn.CloseAndDisposeAsync();
                }
            }
        }

        /// <summary>
        /// Disables foreign key checks for the current transaction.
        /// </summary>
        /// <remarks>Required for <see cref="ISchemaManager.CloneDatabaseAsync">CloneDatabaseAsync</see> infrastructure.</remarks>
        protected override async Task DisableForeignKeysForCurrentTransactionAsync()
        {
            if (!Agent.IsTransactionInProgress) throw new InvalidOperationException(
                Properties.Resources.DisableForeignKeysExceptionTransactionNull);
            await Agent.ExecuteCommandRawAsync("SET FOREIGN_KEY_CHECKS = 0;", null)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Inserts table data from the reader to the current SqlAgent instance,
        /// </summary>
        /// <param name="table">a schema of the table to insert the data to</param>
        /// <param name="reader">an IDataReader to read the table data from.</param>
        /// <remarks>Required for <see cref="ISchemaManager.CloneDatabaseAsync">CloneDatabaseAsync</see> infrastructure.
        /// The insert is performed using a transaction that is already initiated by the 
        /// <see cref="ISchemaManager.CloneDatabaseAsync">CloneDatabaseAsync</see>.</remarks>
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
