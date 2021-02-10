using System;
using System.Collections.Generic;
using System.Linq;

namespace A5Soft.DAL.Core.DbSchema
{
    /// <summary>
    /// Represents a canonical database table specification.
    /// </summary>
    [Serializable]
    public sealed class TableSchema
    {

        #region Private Fields

        private bool _IsProcessed = false;

        private string _name = string.Empty;
        private string _description = string.Empty;
        private Guid? _extensionGuid = null;
        private List<FieldSchema> _fields = new List<FieldSchema>();

        #endregion
         
        #region Properties

        /// <summary>
        /// Gets or sets the name of the database table.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets a description of the database table.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets the fields of the database table.
        /// </summary>
        /// <remarks>Setter should not be used, it is exclusively for XML serialization support.</remarks>
        public List<FieldSchema> Fields
        {
            get { return _fields; }
            set { _fields = value ?? new List<FieldSchema>(); }
        }
                            
        /// <summary>
        /// Gets or sets a Guid of an extension that the table belongs to. Null for base schema.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public Guid? ExtensionGuid
        {
            get => _extensionGuid;
            internal set => _extensionGuid = value;
        } 
        
        #endregion


        /// <summary>
        /// Initializes a new DbTableSchema instance.
        /// </summary>
        public TableSchema() { }


        /// <summary>
        /// Gets the list of all the data errors for the DbFieldSchema instance as a per property dictionary 
        /// (not including it's child fields errors).
        /// </summary>
        public Dictionary<string, List<string>> GetDataErrors()
        {
            var result = new Dictionary<string, List<string>>();

            if (_name.IsNullOrWhiteSpace()) AddError(result, nameof(Name), 
                Properties.Resources.DbTableSchema_TableNameNull);

            if (_name.IndexOf(" ", StringComparison.OrdinalIgnoreCase) >= 0) AddError(result, nameof(Name), 
                Properties.Resources.DbTableSchema_TableNameContainsEmptySpaces);

            if (_fields.IsNull() || _fields.Count < 1) AddError(result, nameof(Fields), 
                Properties.Resources.DbTableSchema_FieldListEmpty);

            return result;
        }

        private void AddError(Dictionary<string, List<string>> dict, string key, string error)
        {
            if (!dict.ContainsKey(key)) dict.Add(key, new List<string>()); ;
            dict[key].Add(error);
        }

        /// <summary>
        /// Gets the description of all the data errors for the DbTableSchema instance (including it's child fields).
        /// </summary>
        public string GetDataErrorsString()
        {
            var dict = GetDataErrors();

            var childrenErrors = new List<string>();
            if (_fields != null)
            {
                childrenErrors.AddRange(_fields.Select(field => field.GetDataErrorsString()).
                    Where(fieldErrors => !fieldErrors.IsNullOrWhiteSpace()));
            }

            if (!dict.Any() && !childrenErrors.Any()) return string.Empty;

            var result = new List<string>();

            if (dict.Any())
            {
                result.Add(string.Format(Properties.Resources.DbTableSchema_ErrorStringHeader, _name));
                result.AddRange(dict.SelectMany(entry => entry.Value));
            }

            if (childrenErrors.Any())
            {
                if (result.Any()) result.Add(string.Empty);
                result.Add(string.Format(Properties.Resources.DbTableSchema_ErrorStringFieldsHeader, _name));
                result.AddRange(childrenErrors);
            }

            return string.Join(Environment.NewLine, result.ToArray());
        }

        /// <summary>
        /// Sets index names so that they are unique per database.
        /// Name format for foreign keys is table_field_fk.
        /// Name format for other indexes is table_field_idx.
        /// </summary>
        public void SetSafeIndexNames()
        {
            foreach (var item in _fields) item.SetSafeIndexName(_name);
        }


        internal void MarkAsNotProcessed()
        {
            _IsProcessed = false;
        }

        internal List<TableSchema> GetListOrderedByForeignKey(List<TableSchema> fullList)
        {

            var result = new List<TableSchema>();

            if (_IsProcessed) return result;

            _IsProcessed = true;

            foreach (var col in _fields)
            {
                if (col.IndexType == IndexType.ForeignKey || col.IndexType == IndexType.ForeignPrimary)
                {
                    foreach (var tbl in fullList)
                    {
                        if (tbl._name.Trim().ToLower() == col.RefTable.Trim().ToLower())
                        {
                            result.AddRange(tbl.GetListOrderedByForeignKey(fullList));
                            break;
                        }
                    }
                }
            }

            result.Add(this);

            return result;
        }

    }
}
