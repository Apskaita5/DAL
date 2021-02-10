using System;
using System.Collections.Generic;
using System.Linq;

namespace A5Soft.DAL.Core.DbSchema
{
    /// <summary>
    /// Represents a canonical database specification.
    /// </summary>
    [Serializable]
    public sealed class Schema
    {
        #region Private Fields

        private string _description = string.Empty;
        private string _extensionGuid = string.Empty;
        private List<TableSchema> _tables = new List<TableSchema>();

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets or sets a description of the database.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value?.Trim() ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the tables of the database.
        /// </summary>
        /// <remarks>Setter should not be used, it is exclusively for XML serialization support.</remarks>
        public List<TableSchema> Tables
        {
            get { return _tables; }
            set { _tables = value ?? new List<TableSchema>(); }
        }

        /// <summary>
        /// Gets or sets the Guid of the extension that the schema belongs to. Empty for base schema.
        /// </summary>
        public string ExtensionGuid
        {
            get => _extensionGuid;
            set => _extensionGuid = value?.Trim() ?? string.Empty;
        }

        #endregion
        
        #region Constructors

        /// <summary>
        /// Creates a new (empty) database schema instance.
        /// </summary>
        public Schema() { }

        /// <summary>
        /// Creates an aggregated database schema from a base schema (that has null ExtensionGuid)
        /// and extension schemas (that have non null ExtensionGuid).
        /// </summary>
        /// <param name="schemas">all of the schemas available</param>
        /// <param name="forExtensions">identifiers (guid's) of the extensions to use
        /// (if null, will use all the available schemas)</param>
        public Schema(List<Schema> schemas, Guid[] forExtensions)
        {
            if (null == schemas) throw new ArgumentNullException(nameof(schemas));
            if (!schemas.Any()) throw new ArgumentException("No schemas to aggregate.", nameof(schemas));

            var baseCount = schemas.Count(s => s.ExtensionGuid.IsNullOrWhiteSpace());
            if (baseCount < 1) throw new Exception(Properties.Resources.DbSchema_BaseSchemaNotFound);
            if (baseCount > 1) throw new Exception(Properties.Resources.DbSchema_MultipleBaseSchemas);

            var baseSchema = schemas.First(s => s.ExtensionGuid.IsNullOrWhiteSpace());
            // just in case
            foreach (var table in baseSchema._tables) table.ExtensionGuid = null;

            _tables.AddRange(baseSchema._tables);
            _description = baseSchema._description?.Trim() ?? string.Empty;

            if (null != forExtensions && !forExtensions.Any()) return;

            foreach (var schema in schemas.Where(s => !s.ExtensionGuid.IsNullOrWhiteSpace()))
            {

                var clashingTables = _tables.Where(o => schema._tables.Any(
                    n => n.Name.Trim().Equals(o.Name.Trim(), StringComparison.OrdinalIgnoreCase)));
                if (clashingTables.Any())
                {
                    var clashingNames = clashingTables
                        .Select(t => string.Format(Properties.Resources.DbSchema_TableNameClash,
                        t.Name, schema._extensionGuid, t.ExtensionGuid.HasValue ? Properties.Resources.DbSchema_Extension
                            + " " + t.ExtensionGuid.Value.ToString() : Properties.Resources.DbSchema_BaseSchema));
                    throw new Exception(string.Join(Environment.NewLine, clashingNames.ToArray()));
                }

                if (!Guid.TryParse(schema._extensionGuid, out var currentGuid))
                    throw new Exception(string.Format(Properties.Resources.DbSchema_InvalidGuidForSchema, schema._description));

                // just in case
                foreach (var table in schema._tables) table.ExtensionGuid = currentGuid;

                if (null == forExtensions || forExtensions.Length < 1 ||
                    forExtensions.Any(g => g == currentGuid))
                    _tables.AddRange(schema._tables);
            }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Gets the list of all the data errors for the DbSchema instance as a per property dictionary 
        /// (not including it's child tables errors).
        /// </summary>
        /// <remarks>Only for consistency across schema objects.</remarks>
        public Dictionary<string, List<string>> GetDataErrors()
        {
            var result = new Dictionary<string, List<string>>();

            if (_tables.IsNull() || _tables.Count < 1)
                AddError(result, nameof(Tables), Properties.Resources.DbSchema_TableListEmpty);
            if (!_extensionGuid.IsNullOrWhiteSpace() && !Guid.TryParse(_extensionGuid, out _))
                AddError(result, nameof(ExtensionGuid), Properties.Resources.DbSchema_GuidInvalid);

            return result;
        }

        private void AddError(Dictionary<string, List<string>> dict, string key, string error)
        {
            if (!dict.ContainsKey(key)) dict.Add(key, new List<string>()); ;
            dict[key].Add(error);
        }

        /// <summary>
        /// Gets the description of all the data errors for the DbSchema instance (including it's child fields).
        /// </summary>
        public string GetDataErrorsString()
        {
            var dict = GetDataErrors();

            var childrenErrors = new List<string>();
            if (_tables != null)
            {
                childrenErrors.AddRange(_tables.Select(table => table.GetDataErrorsString()).
                    Where(tableErrors => !tableErrors.IsNullOrWhiteSpace()));
            }

            if (!dict.Any() && !childrenErrors.Any()) return string.Empty;

            var result = new List<string>();

            if (dict.Any())
            {
                result.Add(Properties.Resources.DbSchema_ErrorStringHeader);
                result.AddRange(dict.SelectMany(entry => entry.Value));
            }

            if (childrenErrors.Any())
            {
                if (result.Count > 0) result.Add(string.Empty);
                result.Add(Properties.Resources.DbSchema_ErrorStringTablesHeader);
                result.Add(string.Empty);
                result.AddRange(childrenErrors);
            }

            return string.Join(Environment.NewLine, result.ToArray());
        }

        /// <summary>
        /// Returns true if all index names within the schema are unique. Otherwise - false.
        /// </summary>
        public bool AllIndexesUnique()
        {
            var indexes = new List<string>();
            foreach (var table in _tables)
            {
                var table_indexes = table.Fields.Where(t => t.IndexType == IndexType.Simple
                    || t.IndexType == IndexType.Unique).Select(f => f.IndexName.Trim().ToUpperInvariant());
                indexes.AddRange(table_indexes);
            }
            var duplicates = indexes.Where(n => indexes.Count(m => m == n) > 1).Distinct();
            return !duplicates.Any();
        }
        
        #endregion

        /// <summary>
        /// Sets index names so that they are unique per database.
        /// Name format for foreign keys is table_field_fk.
        /// Name format for other indexes is table_field_idx.
        /// </summary>
        public void SetSafeIndexNames()
        {
            foreach (var item in _tables) item.SetSafeIndexNames();
        }

        /// <summary>
        /// Gets a list of DbTableSchema ordered by foreign key index 
        /// (so that the referenced tables are created before the tables that reference them).
        /// </summary>
        /// <returns>an ordered list of DbTableSchema</returns>
        public List<TableSchema> GetTablesInCreateOrder()
        {
            foreach (var tbl in _tables) tbl.MarkAsNotProcessed();
            var result = new List<TableSchema>();
            foreach (var tbl in _tables) result.AddRange(tbl.GetListOrderedByForeignKey(_tables));
            return result;
        }

    }
}
