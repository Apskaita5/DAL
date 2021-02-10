using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using A5Soft.DAL.Core;
using A5Soft.DAL.Core.DbSchema;
using A5Soft.DAL.Core.MicroOrm;
using MySqlConnector;

namespace A5Soft.DAL.MySql
{
    /// <summary>
    /// Provides extension methods for the canonical DbSchema objects to translate them
    /// into the native form.
    /// </summary>
    internal static class Extensions
    {
        internal const string MySqlImplementationId = "mysql";
        internal const string ParamPrefix = "?";


        /// <summary>
        /// Returns true if the canonical DbDataTypes specified are equivalent for the MySQL engine,
        /// otherwise - returns false.
        /// </summary>
        /// <param name="type1">the first type to compare</param>
        /// <param name="type2">the second type to compare</param>
        internal static bool IsEquivalentTo(this DbDataType type1, DbDataType type2)
        {
            if ((type1 == DbDataType.Double || type1 == DbDataType.Float)
                && (type2 == DbDataType.Double || type2 == DbDataType.Float))
                return true;
            return (type1 == type2);
        }

        /// <summary>
        /// Returns true if the schemas of the specified fields are equivalent for the MySQL engine,
        /// otherwise - returns false. Does not compare fields indexes.
        /// </summary>
        /// <param name="field1">the first field to compare</param>
        /// <param name="field2">the second field to compare</param>
        internal static bool FieldSchemaMatch(this FieldSchema field1, FieldSchema field2)
        {
            if (field1.IsNull()) throw new ArgumentNullException(nameof(field1));
            if (field2.IsNull()) throw new ArgumentNullException(nameof(field2));

            if (!field1.Name.EqualsByConvention(field2.Name))
                throw new ArgumentException(Properties.Resources.InvalidFieldComparisonException);

            if (!field1.DataType.IsEquivalentTo(field2.DataType)) return false;

            if (field1.NotNull != field2.NotNull) return false;

            if ((field1.DataType.IsDbDataTypeInteger() || field1.DataType == DbDataType.Decimal)
                && field1.Unsigned != field2.Unsigned) return false;

            if (field1.DataType.IsDbDataTypeInteger() && field1.Autoincrement != field2.Autoincrement) return false;

            if ((field1.DataType == DbDataType.Char || field1.DataType == DbDataType.VarChar
                 || field1.DataType == DbDataType.Decimal) && field1.Length != field2.Length) return false;

            if ((field1.IndexType.IsPrimaryKey() && !field2.IndexType.IsPrimaryKey())
                || (!field1.IndexType.IsPrimaryKey() && field2.IndexType.IsPrimaryKey())) return false;

            if (field1.DataType == DbDataType.Enum && field1.EnumValues.Trim().ToLower().Replace(" ", "")
                != field2.EnumValues.Trim().ToLower().Replace(" ", "")) return false;

            if ((field1.DataType == DbDataType.Char || field1.DataType == DbDataType.VarChar
                 || field1.DataType == DbDataType.Enum || field1.DataType == DbDataType.Text
                 || field1.DataType == DbDataType.TextLong || field1.DataType == DbDataType.TextMedium)
                 && field1.CollationType != field2.CollationType)
                return false;

            return true;
        }

        private static bool IsPrimaryKey(this IndexType indexType)
        {
            return indexType == IndexType.ForeignPrimary || indexType == IndexType.Primary;
        }

        private static bool IsForeignKey(this IndexType indexType)
        {
            return indexType == IndexType.ForeignPrimary || indexType == IndexType.ForeignKey;
        }

        /// <summary>
        /// Returns true if the indexes of the specified fields are equivalent for the MySQL engine,
        /// otherwise - returns false.
        /// </summary>
        /// <param name="field1">the first field to compare</param>
        /// <param name="field2">the second field to compare</param>
        internal static bool FieldIndexMatch(this FieldSchema field1, FieldSchema field2)
        {
            if (field1.IsNull()) throw new ArgumentNullException(nameof(field1));
            if (field2.IsNull()) throw new ArgumentNullException(nameof(field2));
                              
            if (!field1.Name.EqualsByConvention(field2.Name))
                throw new ArgumentException(Properties.Resources.InvalidFieldComparisonException);

            if (field1.IndexType != field2.IndexType) return false;

            if (!field1.IndexType.IsForeignKey()) return true;

            if (!field1.RefField.EqualsByConvention(field2.RefField) ||
                !field1.RefTable.EqualsByConvention(field2.RefTable) ||
                field1.OnDeleteForeignKey != field2.OnDeleteForeignKey ||
                field1.OnUpdateForeignKey != field2.OnUpdateForeignKey)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the mysql native name for the canonical data type for the field schema specified.
        /// </summary>
        /// <param name="schema">the field schema to return the native data type for</param>
        internal static string GetNativeDataType(this FieldSchema schema)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));

            switch (schema.DataType)
            {
                case DbDataType.Float:
                    return "FLOAT";
                case DbDataType.Real:
                    return "REAL";
                case DbDataType.Double:
                    return "DOUBLE";
                case DbDataType.Decimal:
                    return "DECIMAL";
                case DbDataType.IntegerTiny:
                    return "TINYINT";
                case DbDataType.IntegerSmall:
                    return "SMALLINT";
                case DbDataType.IntegerMedium:
                    return "MEDIUMINT";
                case DbDataType.Integer:
                    return "INT";
                case DbDataType.IntegerBig:
                    return "BIGINT";
                case DbDataType.TimeStamp:
                    return "TIMESTAMP";
                case DbDataType.Date:
                    return "DATE";
                case DbDataType.DateTime:
                    return "DATETIME";
                case DbDataType.Time:
                    return "TIME";
                case DbDataType.Char:
                    return "CHAR";
                case DbDataType.VarChar:
                    return "VARCHAR";
                case DbDataType.Text:
                    return "TEXT";
                case DbDataType.TextMedium:
                    return "MEDIUMTEXT";
                case DbDataType.TextLong:
                    return "LONGTEXT";
                case DbDataType.BlobTiny:
                    return "TINYBLOB";
                case DbDataType.Blob:
                    return "BLOB";
                case DbDataType.BlobMedium:
                    return "MEDIUMBLOB";
                case DbDataType.BlobLong:
                    return "LONGBLOB";
                case DbDataType.Enum:
                    return "ENUM";
                default:
                    throw new NotImplementedException(
                        string.Format(Properties.Resources.EnumValueNotImplementedException,
                        schema.DataType.ToString()));
            }

        }

        /// <summary>
        /// Gets the mysql native foreign index change action type.
        /// </summary>
        /// <param name="actionType">the canonical foreign index change action type to translate</param>
        internal static string GetNativeActionType(this ForeignKeyActionType actionType)
        {
            switch (actionType)
            {
                case ForeignKeyActionType.Restrict:
                    return "RESTRICT";
                case ForeignKeyActionType.Cascade:
                    return "CASCADE";
                case ForeignKeyActionType.SetNull:
                    return "SET NULL";
                default:
                    throw new NotImplementedException(string.Format(Properties.Resources.EnumValueNotImplementedException,
                        actionType.ToString()));
            }
        }

        /// <summary>
        /// Gets the mysql native field schema definition.
        /// </summary>
        /// <param name="schema">the canonical field schema to translate</param>
        internal static string GetFieldDefinition(this FieldSchema schema, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            var result = $"{schema.Name.ToConventional(agent)} {schema.GetNativeDataType()}";

            if (schema.DataType == DbDataType.Char || schema.DataType == DbDataType.VarChar)
            {
                result = $"{result}({schema.Length})";
            }
            if (schema.DataType == DbDataType.Decimal)
            {
                result = $"{result}({(schema.Length + 15)}, {schema.Length})";
            }
            if (schema.DataType == DbDataType.Enum)
            {
                var enumValues = schema.EnumValues.Split(new string[] { "," }, 
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => $"'{v.ToConventional(agent)}'");
                result = $"{result}({string.Join(", ", enumValues)})";
            }

            if (schema.DataType == DbDataType.Char || schema.DataType == DbDataType.Enum
                || schema.DataType == DbDataType.Text || schema.DataType == DbDataType.TextLong
                || schema.DataType == DbDataType.TextMedium || schema.DataType == DbDataType.VarChar)
            {
                if (schema.CollationType == FieldCollationType.ASCII_Binary)
                {
                    result += " CHARACTER SET 'ascii' COLLATE 'ascii_bin'";
                }
                else if (schema.CollationType == FieldCollationType.ASCII_CaseInsensitive)
                {
                    result += " CHARACTER SET 'ascii' COLLATE 'ascii_general_ci'";
                }
            }

            if (schema.Unsigned && (schema.DataType.IsDbDataTypeInteger() ||
                schema.DataType.IsDbDataTypeFloat() || schema.DataType == DbDataType.Decimal))
            {
                result += " UNSIGNED";
            }

            if (schema.NotNull)
            {
                result += " NOT NULL";
            }

            if (schema.Autoincrement && schema.DataType.IsDbDataTypeInteger())
            {
                result += " AUTO_INCREMENT";
            }

            if (schema.IndexType.IsPrimaryKey())
            {
                result += " PRIMARY KEY";
            }

            return result;
        }

        /// <summary>
        /// Gets a list of statements required to add a new database table field using the field schema specified.
        /// (also fixes datetime defaults and adds index if required)
        /// </summary>
        /// <param name="schema">a canonical schema of the new database table field</param>
        /// <param name="dbName">the database to add the field for</param>
        /// <param name="tblName">the database table to add the field for</param>
        internal static List<string> GetAddFieldStatements(this FieldSchema schema,
            string dbName, string tblName, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (dbName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(dbName));
            if (tblName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(tblName));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            dbName = dbName.Trim();
            tblName = tblName.ToConventional(agent);
            var fieldName = schema.Name.ToConventional(agent);

            var result = new List<string>()
                {
                    $"ALTER TABLE `{dbName}`.`{tblName}` ADD COLUMN {schema.GetFieldDefinition(agent)};"
                };

            if (schema.NotNull && (schema.DataType == DbDataType.Date))
            {
                result.Add($"UPDATE `{dbName}`.`{tblName}` SET {fieldName}='{DateTime.UtcNow:yyyy'-'MM'-'dd}';");
            }
            if (schema.NotNull && (schema.DataType == DbDataType.Time ||
                schema.DataType == DbDataType.DateTime || schema.DataType == DbDataType.TimeStamp))
            {
                result.Add($"UPDATE `{dbName}`.`{tblName}` SET {fieldName}='{DateTime.UtcNow:yyyy'-'MM'-'dd HH':'mm':'ss}';");
            }

            result.AddRange(schema.GetAddIndexStatements(dbName, tblName, agent));

            return result;
        }

        /// <summary>
        /// Gets a list of statements required to alter the database table field schema 
        /// to match the specified gauge schema.(does not fixes indexes)
        /// </summary>
        /// <param name="schema">the gauge field schema to apply</param>
        /// <param name="dbName">the database to alter the field for</param>
        /// <param name="tblName">the database table to alter the field for</param>
        internal static List<string> GetAlterFieldStatements(this FieldSchema schema,
            string dbName, string tblName, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (dbName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(dbName));
            if (tblName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(tblName));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            return new List<string>()
                {
                    $"ALTER TABLE `{dbName.Trim()}`.`{tblName.ToConventional(agent)}` MODIFY COLUMN {schema.GetFieldDefinition(agent).Replace("PRIMARY KEY", "")};"
                };
        }

        /// <summary>
        /// Gets a list of statements required to drop the database table field.
        /// </summary>
        /// <param name="schema">the field schema to drop</param>
        /// <param name="dbName">the database to drop the field for</param>
        /// <param name="tblName">the database table to drop the field for</param>
        internal static List<string> GetDropFieldStatements(this FieldSchema schema,
            string dbName, string tblName, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (dbName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(dbName));
            if (tblName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(tblName));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            return new List<string>()
                {
                    $"ALTER TABLE `{dbName.Trim()}`.`{tblName.ToConventional(agent)}` DROP COLUMN {schema.Name.ToConventional(agent)};"
                };
        }

        /// <summary>
        /// Gets the mysql native index schema definition.
        /// </summary>
        /// <param name="schema">the canonical field schema to translate</param>
        /// <remarks>MySql creates an index for each foreign key.</remarks>
        internal static string GetIndexDefinition(this FieldSchema schema, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            // no backing index for primary key
            if (schema.IndexType == IndexType.None || schema.IndexType == IndexType.Primary
                || schema.IndexType == IndexType.ForeignPrimary)
                return string.Empty;

            if (schema.IndexType == IndexType.Unique)
            {
                return $"UNIQUE KEY `{schema.IndexName.ToConventional(agent)}` (`{schema.Name.ToConventional(agent)}`)";
            }

            return $"KEY `{schema.IndexName.ToConventional(agent)}` (`{schema.Name.ToConventional(agent)}`)";
        }

        /// <summary>
        /// Gets the mysql native foreign key schema definition.
        /// </summary>
        /// <param name="schema">the canonical field schema to translate</param>
        internal static string GetForeignKeyDefinition(this FieldSchema schema, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            if (!schema.IndexType.IsForeignKey()) return string.Empty;

            var indexName = schema.IndexName.ToConventional(agent);
            var fieldName = schema.Name.ToConventional(agent);
            var refTable = schema.RefTable.ToConventional(agent);
            var refField = schema.RefField.ToConventional(agent);
            var onDelete = schema.OnDeleteForeignKey.GetNativeActionType();
            var onUpdate = schema.OnUpdateForeignKey.GetNativeActionType();

            return $"CONSTRAINT `{indexName}` FOREIGN KEY `{indexName}`(`{fieldName}`) REFERENCES `{refTable}`(`{refField}`) ON DELETE {onDelete} ON UPDATE {onUpdate}";
        }

        /// <summary>
        /// Gets a list of statements required to add a new index using the field schema specified.
        /// (also adds a foreign key if required)
        /// </summary>
        /// <param name="schema">a canonical database table field schema to apply</param>
        /// <param name="dbName">the database to add the index for</param>
        /// <param name="tblName">the database table to add the index for</param>
        internal static List<string> GetAddIndexStatements(this FieldSchema schema,
            string dbName, string tblName, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (dbName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(dbName));
            if (tblName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(tblName));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            if (schema.IndexType == IndexType.None || schema.IndexType == IndexType.Primary)
                return new List<string>();

            dbName = dbName.Trim();
            tblName = tblName.ToConventional(agent);
            var indexName = schema.IndexName.ToConventional(agent);
            var fieldName = schema.Name.ToConventional(agent);
            var refTable = schema.RefTable.ToConventional(agent);
            var refField = schema.RefField.ToConventional(agent);
            var onDelete = schema.OnDeleteForeignKey.GetNativeActionType();
            var onUpdate = schema.OnUpdateForeignKey.GetNativeActionType();

            string result;

            if (schema.IndexType.IsForeignKey())
            {
                result = $"ALTER TABLE `{dbName}`.`{tblName}` ADD CONSTRAINT `{indexName}` FOREIGN KEY `{indexName}`(`{fieldName}`) REFERENCES `{refTable}`(`{refField}`) ON DELETE {onDelete} ON UPDATE {onUpdate};";
            }
            else
            {
                var uniqueStr = "";
                if (schema.IndexType == IndexType.Unique) uniqueStr = "UNIQUE ";

                result = $"CREATE {uniqueStr}INDEX `{indexName}` ON `{dbName}`.`{tblName}` (`{fieldName}`);";
            }

            return new List<string>() { result };
        }

        /// <summary>
        /// Gets a list of statements required to drop the index.(also drops a foreign key if required)
        /// </summary>
        /// <param name="schema">the field schema containing the index to drop</param>
        /// <param name="dbName">the database to drop the index for</param>
        /// <param name="tblName">the database table to drop the index for</param>
        internal static List<string> GetDropIndexStatements(this FieldSchema schema,
            string dbName, string tblName, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (dbName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(dbName));
            if (tblName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(tblName));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            if (schema.IndexType == IndexType.None || schema.IndexType == IndexType.Primary)
                return new List<string>();

            dbName = dbName.Trim();
            tblName = tblName.ToConventional(agent);
            var indexName = schema.IndexName.ToConventional(agent);

            string result;

            if (schema.IndexType == IndexType.ForeignKey || schema.IndexType == IndexType.ForeignPrimary)
            {
                result = $"ALTER TABLE `{dbName}`.`{tblName}` DROP FOREIGN KEY `{indexName}`;";
            }
            else
            {
                result = $"DROP INDEX `{indexName}` ON `{dbName}`.`{tblName}`;";
            }

            return new List<string>() { result };
        }


        /// <summary>
        /// Gets a list of statements required to add a new database table using the schema specified.
        /// </summary>
        /// <param name="schema">a canonical schema of the new database table</param>
        /// <param name="dbName">the database to add the table for</param>
        /// <param name="engine">the SQL engine to use for the table</param>
        /// <param name="charset">the default charset for the table</param>
        internal static List<string> GetCreateTableStatements(this TableSchema schema, string dbName,
            string engine, string charset, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (dbName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(dbName));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            var lines = schema.Fields.Select(field => field.GetFieldDefinition(agent)).ToList();

            lines.AddRange(from field in schema.Fields
                           where field.IndexType == IndexType.Simple
                           || field.IndexType == IndexType.Unique
                           || field.IndexType == IndexType.ForeignKey
                           select field.GetIndexDefinition(agent));

            lines.AddRange(from field in schema.Fields
                           where field.IndexType.IsForeignKey()
                           select field.GetForeignKeyDefinition(agent));

            return new List<string>()
                {
                    $"CREATE TABLE {dbName.Trim()}.{schema.Name.ToConventional(agent)}({string.Join(", ", lines.ToArray())}) ENGINE={engine?.Trim()}  DEFAULT CHARSET={charset?.Trim()};"
                };
        }

        /// <summary>
        /// Gets a list of statements required to drop the database table.
        /// </summary>
        /// <param name="schema">the table schema to drop</param>
        /// <param name="dbName">the database to drop the table for</param>
        internal static List<string> GetDropTableStatements(this TableSchema schema,
            string dbName, SqlAgentBase agent)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (dbName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(dbName));
            if (agent.IsNull()) throw new ArgumentNullException(nameof(agent));

            return new List<string>() { $"DROP TABLE {dbName.Trim()}.{schema.Name.ToConventional(agent)};" };
        }



        /// <summary>
        /// Returns true if the string value is null or empty or consists from whitespaces only.
        /// </summary>
        /// <param name="value">a string value to evaluate</param>
        internal static bool IsNullOrWhiteSpace(this string value)
        {
            return (null == value || string.IsNullOrEmpty(value.Trim()));
        }

        /// <summary>
        /// Returns a value indicating that the object (value) is null. Required due to potential operator overloads
        /// that cause unpredictable behaviour of standard null == value test.
        /// </summary>
        /// <typeparam name="T">a type of the object to test</typeparam>
        /// <param name="value">an object to test against null</param>
        internal static bool IsNull<T>(this T value) where T : class
        {
            return ReferenceEquals(value, null) || DBNull.Value == value;
        }

        internal static Exception WrapSqlException(this Exception target)
        {
            var actualEx = target;
            if (!target.IsNull() && target is AggregateException aggregateEx)
                actualEx = aggregateEx.Flatten().InnerExceptions[0];

            if (actualEx is SqlException sqlEx) return sqlEx;

            if (actualEx is MySqlException mySqlEx) 
                return mySqlEx.WrapMySqlException(string.Empty);

            return target;
        }

        internal static Exception WrapSqlException(this Exception target, string statement,
            SqlParam[] parameters = null)
        {
            var actualEx = target;
            if (!target.IsNull() && target is AggregateException aggregateEx)
                actualEx = aggregateEx.Flatten().InnerExceptions[0];

            if (actualEx is SqlException sqlEx) return sqlEx;

            if (actualEx is MySqlException mySqlEx) return mySqlEx.WrapMySqlException(
                $"{statement} Parameters: {parameters.GetDescription()}");

            return target;
        }

        internal static Exception WrapSqlException(this Exception target, Exception rollbackException)
        {
            var actualEx = target.WrapSqlException();
            var actualRollbackEx = rollbackException.WrapSqlException();

            if (actualEx is SqlException sqlEx) return sqlEx.GetRollbackException(actualRollbackEx);
            
            return SqlException.GetRollbackException(actualEx, actualRollbackEx);
        }

        private static Exception WrapMySqlException(this MySqlException target, string statementDescription)
        {
            return new SqlException(string.Format(Properties.Resources.SqlExceptionMessageWithStatement,
                target.ErrorCode.ToString(), target.ErrorCode, target.HResult, target.Number, 
                target.SqlState, target.Message, Environment.NewLine, statementDescription),
                (int)target.ErrorCode, statementDescription, target);
        }

        /// <summary>
        /// If MySqlConnection is not null, closes it (if open) and disposes it.
        /// </summary>
        /// <param name="conn">MySqlConnection to close and dispose</param>
        internal static async Task CloseAndDisposeAsync(this MySqlConnection conn)
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
