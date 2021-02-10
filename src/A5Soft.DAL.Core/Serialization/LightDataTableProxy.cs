using System;
using System.Collections.Generic;

namespace A5Soft.DAL.Core.Serialization
{
    /// <summary>
    /// A data proxy class used to serialize LightDataTable.
    /// </summary>
    [Serializable]
    internal sealed class LightDataTableProxy
    {

        public List<LightDataColumnProxy> Columns { get; set; }

        public List<LightDataRowProxy> Rows { get; set; }

        public string TableName { get; set; }

        public List<string> DateTimeFormats { get; set; }

    }
}
