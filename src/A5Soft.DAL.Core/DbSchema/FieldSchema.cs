using System;
using System.Collections.Generic;
using System.Linq;

namespace A5Soft.DAL.Core.DbSchema
{
    /// <summary>
    /// Represents a canonical database table field specification.
    /// </summary>
    [Serializable]
    public sealed class FieldSchema
    {

        #region Private Fields

        private readonly Guid _guid = Guid.NewGuid();
        private string _name = string.Empty;
        private DbDataType _dataType = DbDataType.Char;
        private int _length = 255;
        private bool _notNull = true;
        private bool _autoincrement = false;
        private bool _unsigned = true;
        private string _enumValues = string.Empty;
        private string _description = string.Empty;
        private FieldCollationType _collationType = FieldCollationType.Default;
        private IndexType _indexType = Core.DbSchema.IndexType.None;
        private string _indexName = string.Empty;
        private ForeignKeyActionType _onUpdateForeignKey = ForeignKeyActionType.Cascade;
        private ForeignKeyActionType _onDeleteForeignKey = ForeignKeyActionType.Restrict;
        private string _refTable = string.Empty;
        private string _refField = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the database table field.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets the canonical data type of the database table field.
        /// </summary>
        public DbDataType DataType
        {
            get { return _dataType; }
            set { _dataType = value; }
        }

        /// <summary>
        /// Gets or sets the length of the database table field, e.g. for CHAR field.
        /// Only applicable for some data types (char, varchar, etc.), have no effect for other types.
        /// Implementation of this field is dependent on SQL implementation.
        /// </summary>
        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether the database table field value should always be non null.
        /// </summary>
        public bool NotNull
        {
            get { return _notNull; }
            set { _notNull = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether the database table field value 
        /// should be set automaticaly by database autoincrement function.
        /// </summary>
        public bool Autoincrement
        {
            get { return _autoincrement; }
            set { _autoincrement = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether the database table field value is unsigned.
        /// Only applicable to integer types, have no effect for other types.
        /// </summary>
        public bool Unsigned
        {
            get { return _unsigned; }
            set { _unsigned = value; }
        }

        /// <summary>
        /// Gets or sets the comma separated list of enum values.
        /// Only applicable to enum type, have no effect for other types.
        /// </summary>
        public string EnumValues
        {
            get { return _enumValues; }
            set { _enumValues = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets the description of the database table field.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets a collation type of the database table field.
        /// </summary>
        public FieldCollationType CollationType
        {
            get { return _collationType; }
            set { _collationType = value; }
        }

        /// <summary>
        /// Gets or sets the type of the index that should be created for the database table field.
        /// </summary>
        /// <remarks>Multi column indexes are not supported.
        /// Some SQL implementations technically creates two indexes for foreign key, 
        /// however as for the canonical model they are considered as one.</remarks>
        public IndexType IndexType
        {
            get { return _indexType; }
            set { _indexType = value; }
        }

        /// <summary>
        /// Gets or sets the name of the index that should be created for the database table field.
        /// </summary>
        /// <remarks>Only applicable if the <see cref="IndexType"/> is not <see cref="Core.DbSchema.IndexType.None"/>,
        /// otherwise have no effect.</remarks>
        public string IndexName
        {
            get { return _indexName; }
            set { _indexName = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets the type of the action taken by the database when a foreign key (parent)
        /// is updated for the database table field.
        /// </summary>
        /// <remarks>Only applicable if the <see cref="IndexType"/> is <see cref="Core.DbSchema.IndexType.ForeignKey"/>,
        /// otherwise have no effect.</remarks>
        public ForeignKeyActionType OnUpdateForeignKey
        {
            get { return _onUpdateForeignKey; }
            set { _onUpdateForeignKey = value; }
        }

        /// <summary>
        /// Gets or sets the type of the action taken by the database when a foreign key (parent)
        /// is deleted for the database table field.
        /// </summary>
        /// <remarks>Only applicable if the <see cref="IndexType"/> is <see cref="Core.DbSchema.IndexType.ForeignKey"/>,
        /// otherwise have no effect.</remarks>
        public ForeignKeyActionType OnDeleteForeignKey
        {
            get { return _onDeleteForeignKey; }
            set { _onDeleteForeignKey = value; }
        }

        /// <summary>
        /// Gets or sets the database table that is referenced by the foreign key.
        /// </summary>
        public string RefTable
        {
            get { return _refTable; }
            set { _refTable = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets the database table field that is referenced by the foreign key.
        /// </summary>
        public string RefField
        {
            get { return _refField; }
            set { _refField = value?.Trim() ?? string.Empty; }
        }

        #endregion


        /// <summary>
        /// Initializes a new DbFieldSchema instance.
        /// </summary>
        public FieldSchema()
        {
        }


        /// <summary>
        /// Gets a canonical definition of the field.
        /// </summary>
        public string GetDefinition()
        {
            var result = $"{_name} {_dataType}";

            if (_dataType.HasLengthAttribute()) result = $"{result}({_length})";

            if (_dataType == DbDataType.Enum) result = $"{result}({_enumValues})";

            if (_notNull) result += " NOT NULL";

            if (_unsigned && _dataType.IsDbDataTypeInteger()) result += " UNSIGNED";

            if (_autoincrement && _dataType.IsDbDataTypeInteger()) result += " AUTOINCREMENT";

            if (_indexType == IndexType.Primary) result += " PRIMARY KEY";

            if (_dataType.HasCollationAttribute())
            {
                if (_collationType == FieldCollationType.ASCII_Binary) result += " COLLATE ascii_bin";
                else if (_collationType == FieldCollationType.ASCII_CaseInsensitive)
                    result += " COLLATE ascii_ci";
            }

            if (_indexType == IndexType.Simple) result = $"{result} INDEX {_indexName}";
            if (_indexType == IndexType.Unique) result = $"{result} UNIQUE INDEX {_indexName}";
            if (_indexType == IndexType.ForeignKey || _indexType == IndexType.ForeignPrimary)
                result =
                    $"{result} FOREIGN KEY {_indexName} REFERENCES {_refTable}({_refField}) ON UPDATE {_onUpdateForeignKey.ToString().ToUpperInvariant()} ON DELETE {_onDeleteForeignKey.ToString().ToUpperInvariant()}";

            return result;
        }


        /// <summary>
        /// Gets the list of all the data errors for the DbFieldSchema instance as a per property dictionary.
        /// </summary>
        public Dictionary<string, List<string>> GetDataErrors()
        {
            var result = new Dictionary<string, List<string>>();

            if (_name.IsNullOrWhiteSpace())
                AddError(result, nameof(Name),
                    Properties.Resources.DbFieldSchema_FieldNameNull);

            if (_name.IndexOf(" ", StringComparison.OrdinalIgnoreCase) >= 0)
                AddError(result, nameof(Name),
                    Properties.Resources.DbFieldSchema_FieldNameContainsBlankSpaces);

            if (_length < 0)
                AddError(result, nameof(Length),
                    Properties.Resources.DbFieldSchema_FieldLengthNegative);

            if (_autoincrement && !_dataType.IsDbDataTypeInteger())
                AddError(result, nameof(Autoincrement),
                    Properties.Resources.DbFieldSchema_AutoincrementInvalid);

            if (_dataType == DbDataType.Enum && _enumValues.IsNullOrWhiteSpace())
                AddError(result, nameof(EnumValues),
                    Properties.Resources.DbFieldSchema_EnumValuesNull);

            if (_dataType == DbDataType.Enum && !_enumValues.Contains(","))
                AddError(result, nameof(EnumValues),
                    Properties.Resources.DbFieldSchema_EnumValuesTooShort);

            if (_indexType != IndexType.None && _indexType != IndexType.Primary && _indexName.IsNullOrWhiteSpace())
                AddError(result, nameof(IndexName), Properties.Resources.DbFieldSchema_IndexNameNull);

            if ((_indexType == IndexType.ForeignKey || _indexType == IndexType.ForeignPrimary) &&
                _refTable.IsNullOrWhiteSpace())
                AddError(result, nameof(RefTable),
                    Properties.Resources.DbFieldSchema_RefTableNull);

            if ((_indexType == IndexType.ForeignKey || _indexType == IndexType.ForeignPrimary)
                && _refField.IsNullOrWhiteSpace())
                AddError(result, nameof(RefField),
                    Properties.Resources.DbFieldSchema_RefFieldNull);

            return result;
        }

        private void AddError(Dictionary<string, List<string>> dict, string key, string error)
        {
            if (!dict.ContainsKey(key)) dict.Add(key, new List<string>());
            ;
            dict[key].Add(error);
        }


        /// <summary>
        /// Gets the description of all the data errors for the DbFieldSchema instance.
        /// </summary>
        public string GetDataErrorsString()
        {
            var dict = GetDataErrors();

            if (!dict.Any()) return string.Empty;

            var result = new List<string>
            {
                string.Format(Properties.Resources.DbFieldSchema_ErrorStringHeader, _name, _dataType.ToString(),
                    _length.ToString())
            };
            result.AddRange(dict.SelectMany(entry => entry.Value));

            return string.Join(Environment.NewLine, result.ToArray());
        }

        /// <summary>
        /// Sets index names so that they are unique per database.
        /// Name format for foreign keys is table_field_fk.
        /// Name format for other indexes is table_field_idx.
        /// </summary>
        public void SetSafeIndexName(string tableName)
        {
            if (_indexType == IndexType.ForeignKey || _indexType == IndexType.ForeignPrimary)
                _indexName = $"{tableName.Trim().ToLower()}_{_name.Trim().ToLower()}_fk";
            if (_indexType == IndexType.Simple || _indexType == IndexType.Unique)
                _indexName = $"{tableName.Trim().ToLower()}_{_name.Trim().ToLower()}_idx";
        }


        /// <summary>
        /// Technical method to support databinding. Returns a Guid 
        /// that is created for every new DbFieldSchema instance (not persisted).
        /// </summary>
        /// <returns></returns>
        public object GetIdValue()
        {
            return _guid;
        }
    }
}
