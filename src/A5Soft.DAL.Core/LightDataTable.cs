using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using A5Soft.DAL.Core.Serialization;

namespace A5Soft.DAL.Core
{
    [Serializable]
    public sealed class LightDataTable
    {

        private readonly List<string> _dateTimeFormats = new List<string>();
        private string _tableName = string.Empty;


        /// <summary>
        /// Gets the collection of columns that belong to this table.
        /// </summary>
        /// <value>A LightDataColumnCollection that contains the collection of LightDataColumn objects for the table. 
        /// An empty collection is returned if no LightDataColumn objects exist.</value>
        /// <remarks>The LightDataColumnCollection determines the schema of a table by defining the data type of each column.</remarks>
        public LightDataColumnCollection Columns { get; }

        /// <summary>
        /// Gets the collection of rows that belong to this table.
        /// </summary>
        /// <value>A LightDataRowCollection that contains the collection of LightDataRow objects for the table. 
        /// An empty collection is returned if no LightDataRow objects exist.</value>
        public LightDataRowCollection Rows { get; }

        /// <summary>
        /// Gets or sets the name of the LightDataTable instance.
        /// </summary>
        public string TableName
        {
            get { return _tableName; }
            set { _tableName = value ?? string.Empty; }
        }

        /// <summary>
        /// Gets a list of possible string DateTime formats.
        /// Should be set by the data provider (e.g. ISqlAgent) in order to support string to date conversion.
        /// </summary>
        public List<string> DateTimeFormats
        {
            get { return _dateTimeFormats; }
        }


        /// <summary>
        /// Initializes a new LightDataTable instance.
        /// </summary>
        public LightDataTable()
        {
            Columns = new LightDataColumnCollection(this);
            Rows = new LightDataRowCollection(this);
        }

        /// <summary>
        /// Initializes a new LightDataTable instance and fills it with schema and values using data reader specified.
        /// </summary>
        public LightDataTable(IDataReader reader) : this()
        {
            if (reader.IsNull()) throw new ArgumentNullException(nameof(reader));

            if (!reader.Read())
            {
                Columns.Add(reader);
                return;
            }

            Columns.Add(reader);
            Rows.Add(reader);
        }

        /// <summary>
        /// Initializes a new LightDataTable instance and fills it with schema and values using proxy data.
        /// </summary>
        /// <param name="proxy">a serialization proxy to load the data from</param>
        internal LightDataTable(LightDataTableProxy proxy) : this()
        {
             if (proxy.IsNull()) throw new ArgumentNullException(nameof(proxy));

             _tableName = proxy.TableName;

            _dateTimeFormats = proxy.DateTimeFormats ?? new List<string>();

            foreach (var column in proxy.Columns)
            {
                Columns.Add(new LightDataColumn(column));
            }

            foreach (var row in proxy.Rows)
            {
                Rows.Add(new LightDataRow(this, row));
            }
        }


        public static async Task<LightDataTable> CreateAsync(DbDataReader reader, CancellationToken ct = default)
        {
            if (reader.IsNull()) throw new ArgumentNullException(nameof(reader));

            var result = new LightDataTable();

            if (!await reader.ReadAsync(ct))
            {
                result.Columns.Add(reader);
                return result;
            }

            result.Columns.Add(reader);
            await result.Rows.AddAsync(reader, ct);

            return result;
        }

        

        internal LightDataTableProxy GetLightDataTableProxy()
        {
            return new LightDataTableProxy
            {
                Columns = Columns.ToProxyList(),
                Rows = Rows.ToProxyList(),
                TableName = _tableName
            };
        }

        /// <summary>
        /// Gets a deep copy of the table.
        /// </summary>
        /// <returns></returns>
        public LightDataTable Clone()
        {
            return new LightDataTable(this.GetLightDataTableProxy());
        }

    }
}
