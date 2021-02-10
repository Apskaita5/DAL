using System;

namespace A5Soft.DAL.Core
{
    /// <summary>
    /// Represents an abstract SQL query parameter data.
    /// </summary>
    [Serializable]
    public sealed class SqlParam
    {
 
        /// <summary>
        /// Gets the parameter name. 
        /// The name should be set without SQL implementation specific prefix.
        /// </summary>
        public string Name { get; }
    
        /// <summary>
        /// Gets the parameter value. If the value is set to null, <see cref="ValueType">ValueType</see> property should also be set.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets the parameter value type.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// Gets a value indicating whether to modify SQL query by replacing parameter name with value
        /// instead of using built in SQL parameter functionality.
        /// </summary>
        public bool ReplaceInQuery { get; }
            
        
        private SqlParam(string name, object value, Type valueType, bool replaceInQuery)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));
            if (null == valueType) throw new ArgumentNullException(nameof(valueType));

            Name = name;
            ValueType = valueType;
            Value = value;
            ReplaceInQuery = replaceInQuery;
        }
               


        /// <summary>
        /// Gets a parameter value for a specific SqlAgent.
        /// </summary>
        /// <param name="sqlAgent">an SqlAgent that requests value and holds formatting preferences</param>
        public object GetValue(ISqlAgent sqlAgent)
        {
            if (sqlAgent.IsNull()) throw new ArgumentNullException(nameof(sqlAgent));

            if (Value.IsNull()) return null;

            if (ValueType == typeof(bool) && sqlAgent.BooleanStoredAsTinyInt)
            {
                if ((bool)Value) return 1;
                return 0;
            }
            else if (ValueType == typeof(Guid))
            {
                if (sqlAgent.GuidStoredAsBlob) return ((Guid)Value).ToByteArray();
                else return ((Guid)Value).ToString("N");
            }

            return Value;
        }

        public override string ToString()
        {
            return $"{Name}={Value?.ToString() ?? "null"}";
        }


        /// <summary>
        /// Creates a new SQL query parameter.
        /// </summary>
        /// <typeparam name="T">a type of the parameter value</typeparam>
        /// <param name="name">a name of the parameter (should be set without SQL implementation specific prefix)</param>
        /// <param name="value">a value of the parameter</param>
        /// <param name="replaceInQuery">whether to modify SQL query by replacing parameter name with value
        /// instead of using built in SQL parameter functionality</param>
        /// <returns>a new SQL query parameter</returns>
        public static SqlParam Create<T>(string name, T value, bool replaceInQuery = false)
        {
            var originalType = typeof(T);
            if (originalType == typeof(object) && !object.ReferenceEquals(value, null))
                originalType = value.GetType();
            return new SqlParam(name, ResolveValue(value), ResolveType(originalType), replaceInQuery);
        }


        /// <summary>
        /// Handles nullable types.
        /// </summary>
        private static Type ResolveType(Type originalType)
        {
            if (originalType == typeof(byte?))
            {
                return typeof(byte);
            }
            else if (originalType == typeof(sbyte?))
            {
                return typeof(sbyte?);
            }
            else if (originalType == typeof(short?))
            {
                return typeof(short);
            }
            else if (originalType == typeof(ushort?))
            {
                return typeof(ushort);
            }
            else if (originalType == typeof(int?))
            {
                return typeof(int);
            }
            else if (originalType == typeof(uint?))
            {
                return typeof(uint);
            }
            else if (originalType == typeof(long?))
            {
                return typeof(long);
            }
            else if (originalType == typeof(ulong?))
            {
                return typeof(ulong);
            }
            else if (originalType == typeof(char?))
            {
                return typeof(char);
            }
            else if (originalType == typeof(DateTime?))
            {
                return typeof(DateTime);
            }
            else if (originalType == typeof(bool?))
            {
                return typeof(bool);
            }
            else if (originalType == typeof(double?))
            {
                return typeof(double);
            }
            else if (originalType == typeof(float?))
            {
                return typeof(float);
            }
            else if (originalType == typeof(decimal?))
            {
                return typeof(decimal);
            }
            else if (originalType == typeof(Guid?))
            {
                return typeof(Guid);
            }

            return originalType;
        }

        /// <summary>
        /// Handles nullable types.
        /// </summary>
        private static object ResolveValue(object originalObject)
        {
            if (originalObject.IsNull()) return null;

            if (originalObject is byte byteVal)
            {
                return byteVal;
            }
            else if (originalObject is sbyte sbyteVal)
            {
                return sbyteVal;
            }
            else if (originalObject is short shortVal)
            {
                return shortVal;
            }
            else if (originalObject is ushort ushortVal)
            {
                return ushortVal;
            }
            else if (originalObject is int intVal)
            {
                return intVal;
            }
            else if (originalObject is uint uintVal)
            {
                return uintVal;
            }
            else if (originalObject is long longVal)
            {
                return longVal;
            }
            else if (originalObject is ulong ulongVal)
            {
                return ulongVal;
            }
            else if (originalObject is char charVal)
            {
                return charVal;
            }
            else if (originalObject is DateTime dateVal)
            {
                return dateVal;
            }
            else if (originalObject is bool boolVal)
            {
                return boolVal;
            }
            else if (originalObject is double doubleVal)
            {
                return doubleVal;
            }
            else if (originalObject is float floatVal)
            {
                return floatVal;
            }
            else if (originalObject is decimal decimalVal)
            {
                return decimalVal;
            }
            else if (originalObject is Guid guidVal)
            {
                return guidVal;
            }
            else
            {
                return originalObject;
            }
        }

    }
}
