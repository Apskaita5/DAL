using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace A5Soft.DAL.Core.DbSchema
{
    public static class Extensions
    {
        private static readonly DbDataType[] _integerDbDataTypes = new DbDataType[]
            {DbDataType.Integer, DbDataType.IntegerBig, DbDataType.IntegerMedium,
            DbDataType.IntegerSmall, DbDataType.IntegerTiny};
        private static readonly DbDataType[] _floatDbDataTypes = new DbDataType[]
            {DbDataType.Float, DbDataType.Double};
        private static readonly DbDataType[] _lengthDbDataTypes = new DbDataType[]
            {DbDataType.Char, DbDataType.VarChar, DbDataType.Decimal};
        private static readonly DbDataType[] _collatedDbDataTypes = new DbDataType[]
            {DbDataType.VarChar, DbDataType.Char, DbDataType.Text, DbDataType.TextLong, 
                DbDataType.TextMedium};

        /// <summary>
        /// Gets a value indicating whether the specified DbDataType value is an integer type.
        /// </summary>
        /// <param name="dbDataType">The DbDataType value to check.</param>
        public static bool IsDbDataTypeInteger(this DbDataType dbDataType)
        {
            return (_integerDbDataTypes.Contains(dbDataType));
        }

        /// <summary>
        /// Gets a value indicating whether the specified DbDataType value is a floating point type.
        /// </summary>
        /// <param name="dbDataType">The DbDataType value to check.</param>
        public static bool IsDbDataTypeFloat(this DbDataType dbDataType)
        {
            return (_floatDbDataTypes.Contains(dbDataType));
        }

        /// <summary>
        /// Gets a value indicating whether the specified DbDataType value has a (relevant) length attribute.
        /// </summary>
        /// <param name="dbDataType">The DbDataType value to check.</param>
        public static bool HasLengthAttribute(this DbDataType dbDataType)
        {
            return (_lengthDbDataTypes.Contains(dbDataType));
        }

        /// <summary>
        /// Gets a value indicating whether the specified DbDataType value has a (relevant) collation.
        /// </summary>
        /// <param name="dbDataType">The DbDataType value to check.</param>
        public static bool HasCollationAttribute(this DbDataType dbDataType)
        {
            return (_collatedDbDataTypes.Contains(dbDataType));
        }


    }
}
