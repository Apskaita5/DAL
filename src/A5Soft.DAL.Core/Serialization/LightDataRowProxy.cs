using System;

namespace A5Soft.DAL.Core.Serialization
{
    /// <summary>
    /// A data proxy class used to serialize LightDataRow.
    /// </summary>
    [Serializable]
    internal sealed class LightDataRowProxy
    {

        public Object[] Values { get; set; }

    }
}
