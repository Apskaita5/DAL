using System;

namespace A5Soft.DAL.Core.Serialization
{
    /// <summary>
    /// A data proxy class used to serialize LightDataColumn.
    /// </summary>
    [Serializable]
    internal sealed class LightDataColumnProxy
    {

        public string Caption { get; set; }

        public string ColumnName { get; set; }

        public string DataType { get; set; }

        public string NativeDataType { get; set; }

        public bool ReadOnly { get; set; }

    }
}
