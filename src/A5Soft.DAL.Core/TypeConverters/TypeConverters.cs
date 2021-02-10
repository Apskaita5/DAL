using System;
using System.Globalization;
using System.Linq;
using System.Text;
using A5Soft.DAL.Core.Properties;

namespace A5Soft.DAL.Core.TypeConverters
{
    /// <summary>
    /// A collection of methods for aggressive type conversions.
    /// (because values returned by an SQL provider sometimes does not directly correspond to expected types)
    /// </summary>
    public static class TypeConverters
    {

        private static readonly string[] _defaultDateTimeFormats =
            new string[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "HH:mm:ss" };
        private static readonly int[] _possibleGuidStringLengths =
            new int[] { 32, 36, 38, 61, 63 };

        #region TryConvert

        /// <summary>
        /// Tries to convert (coerce) object value to boolean.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric, returns "> 0".
        /// If the underlying value type is string, tries to parse value as "true" or "false" (case insensitive)
        /// or to parse Int64 value using InvariantCulture and do "> 0" conversion. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding 
        /// and parse as string (or Int64).</remarks>
        public static bool TryConvertBool(this object value, out bool result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is bool boolVal)
            {
                result = boolVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longResult))
            {
                result = longResult > 0;
                return true;
            }
            if (value.TryConvertUInt64Internal(out ulong ulongResult))
            {
                result = ulongResult > 0;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace())
            {
                if (strValue.Trim().ToUpperInvariant() == "TRUE")
                {
                    result = true;
                    return true;
                }
                if (strValue.Trim().ToUpperInvariant() == "FALSE")
                {
                    result = false;
                    return true;
                }
                if (long.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out long longValue))
                {
                    result = longValue > 0;
                    return true;
                }
                if (ulong.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out ulong ulongValue))
                {
                    result = ulongValue > 0;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to byte.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric and its value does not overflow, returns the value as byte.
        /// If the underlying value type is string, tries to parse value as numeric
        /// and returns the value as byte if it does not overflow. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding, 
        /// parse as byte and return the value.</remarks>
        public static bool TryConvertByte(this object value, out byte result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is byte byteVal)
            {
                result = byteVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longResult) 
                && longResult <= byte.MaxValue && longResult >= byte.MinValue)
            {
                result = (byte)longResult;
                return true;
            }

            if (value.TryConvertUInt64Internal(out ulong ulongResult)
                && ulongResult <= byte.MaxValue)
            {
                result = (byte)ulongResult;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                byte.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out byte byteResult))
            {
                result = byteResult;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to sbyte.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric and its value does not overflow, returns the value as sbyte.
        /// If the underlying value type is string, tries to parse value as numeric
        /// and returns the value as sbyte if it does not overflow. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding, 
        /// parse as sbyte and return the value.</remarks>
        public static bool TryConvertSByte(this object value, out sbyte result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is sbyte sbyteVal)
            {
                result = sbyteVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longResult)
                && longResult <= sbyte.MaxValue && longResult >= sbyte.MinValue)
            {
                result = (sbyte)longResult;
                return true;
            }

            if (value.TryConvertUInt64Internal(out ulong ulongResult)
                && ulongResult <= (int)sbyte.MaxValue)
            {
                result = (sbyte)ulongResult;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                sbyte.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out sbyte sbyteResult))
            {
                result = sbyteResult;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to char.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is char, returns the value.
        /// If the underlying value type is string and contains any chars except for spaces,
        /// returns the first non space char.
        /// If the underlying value type is string and contains only space chars,
        /// returns the space char.
        /// If the underlying value type is byte array, tries to convert to string using UTF8 encoding.</remarks>
        public static bool TryConvertChar(this object value, out char result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is char charVal)
            {
                result = charVal;
                return true;
            }

            if (TryReadString(value, out string strValue) && null != strValue && strValue.Length > 0)
            {
                if (strValue.Trim().Length > 0) result = strValue.Trim().ToCharArray()[0];
                else result = strValue.ToCharArray()[0];
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to Guid.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is byte array and its length is 16, returns new Guid(array).
        /// If the underlying value type is string tries to parse it as a Guid.
        /// If the underlying value type is byte array and its length is NOT 16,
        /// tries to convert to string using UTF8 encoding and then parse as Guid.</remarks>
        public static bool TryConvertGuid(this object value, out Guid result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is Guid guidVal)
            {
                result = guidVal;
                return true;
            }

            if (value is byte[] arrayVal && arrayVal.Length == 16)
            {
                result = new Guid(arrayVal);
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                Array.IndexOf(_possibleGuidStringLengths, strValue.Trim().Length) > -1)
            {
                result = new Guid(strValue.Trim());
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to int16.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric and its value does not overflow, returns the value as int16.
        /// If the underlying value type is string, tries to parse value as numeric
        /// and returns the value as int16 if it does not overflow. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding, 
        /// parse as int16 and return the value.</remarks>
        public static bool TryConvertInt16(this object value, out short result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is short shortVal)
            {
                result = shortVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longResult)
                && longResult <= short.MaxValue && longResult >= short.MinValue)
            {
                result = (short)longResult;
                return true;
            }

            if (value.TryConvertUInt64Internal(out ulong ulongResult)
                && ulongResult <= (int)short.MaxValue)
            {
                result = (short)ulongResult;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                short.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out short shortResult))
            {
                result = shortResult;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to uint16.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric and its value does not overflow, returns the value as uint16.
        /// If the underlying value type is string, tries to parse value as numeric
        /// and returns the value as uint16 if it does not overflow. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding, 
        /// parse as uint16 and return the value.</remarks>
        public static bool TryConvertUInt16(this object value, out ushort result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is ushort shortVal)
            {
                result = shortVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longResult)
                && longResult <= ushort.MaxValue && longResult >= ushort.MinValue)
            {
                result = (ushort)longResult;
                return true;
            }

            if (value.TryConvertUInt64Internal(out ulong ulongResult)
                && ulongResult <= (int)ushort.MaxValue)
            {
                result = (ushort)ulongResult;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                ushort.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out ushort shortResult))
            {
                result = shortResult;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to int32.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric and its value does not overflow, returns the value as int32.
        /// If the underlying value type is string, tries to parse value as numeric
        /// and returns the value as int32 if it does not overflow. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding, 
        /// parse as int32 and return the value.</remarks>
        public static bool TryConvertInt32(this object value, out int result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is int intVal)
            {
                result = intVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longResult)
                && longResult <= int.MaxValue && longResult >= int.MinValue)
            {
                result = (int)longResult;
                return true;
            }

            if (value.TryConvertUInt64Internal(out ulong ulongResult)
                && ulongResult <= int.MaxValue)
            {
                result = (int)ulongResult;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                int.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out int intResult))
            {
                result = intResult;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to uint32.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric and its value does not overflow, returns the value as uint32.
        /// If the underlying value type is string, tries to parse value as numeric
        /// and returns the value as uint32 if it does not overflow. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding, 
        /// parse as uint32 and return the value.</remarks>
        public static bool TryConvertUInt32(this object value, out uint result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is uint uintVal)
            {
                result = uintVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longResult)
                && longResult <= uint.MaxValue && longResult >= uint.MinValue)
            {
                result = (uint)longResult;
                return true;
            }

            if (value.TryConvertUInt64Internal(out ulong ulongResult)
                && ulongResult <= uint.MaxValue)
            {
                result = (uint)ulongResult;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                uint.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out uint uintResult))
            {
                result = uintResult;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to int64.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric and its value does not overflow, returns the value as int64.
        /// If the underlying value type is string, tries to parse value as numeric
        /// and returns the value as int64 if it does not overflow. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding, 
        /// parse as int64 and return the value.</remarks>
        public static bool TryConvertInt64(this object value, out long result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is long longVal)
            {
                result = longVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longResult))
            {
                result = longResult;
                return true;
            }

            if (value.TryConvertUInt64Internal(out ulong ulongResult)
                && ulongResult <= long.MaxValue)
            {
                result = (long)ulongResult;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                long.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out long longRes))
            {
                result = longRes;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to uint64.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric and its value does not overflow, returns the value as uint64.
        /// If the underlying value type is string, tries to parse value as numeric
        /// and returns the value as uint64 if it does not overflow. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding, 
        /// parse as uint64 and return the value.</remarks>
        public static bool TryConvertUInt64(this object value, out ulong result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is ulong ulongVal)
            {
                result = ulongVal;
                return true;
            }

            if (value.TryConvertUInt64Internal(out ulong ulongResult))
            {
                result = ulongResult;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                ulong.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out ulong ulongRes))
            {
                result = ulongRes;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to float.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is float, returns the value.
        /// If the underlying value type is double and its value does not overflow, returns the value as float.
        /// If the underlying value type is decimal, converts it to float using decimal.ToSingle method.
        /// If the underlying value type is string, tries to parse value as float or double. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding 
        /// and parse as float or double.</remarks>
        public static bool TryConvertFloat(this object value, out float result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is float floatVal)
            {
                result = floatVal;
                return true;
            }

            if (value is double doubleVal && doubleVal <= float.MaxValue && doubleVal >= float.MinValue)
            {
                result = (float)doubleVal;
                return true;
            }

            if (value is decimal decimalValue)
            {
                result = decimal.ToSingle(decimalValue);
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace())
            {
                if (float.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue))
                {
                    result = floatValue;
                    return true;
                }
                if (double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue)
                    && doubleValue <= float.MaxValue && doubleValue >= float.MinValue)
                {
                    result = (float)doubleValue;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to double.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is double, returns the value.
        /// If the underlying value type is float, returns the value as double.
        /// If the underlying value type is decimal, converts it to double using decimal.ToDouble method.
        /// If the underlying value type is string, tries to parse value as double. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding 
        /// and parse as double.</remarks>
        public static bool TryConvertDouble(this object value, out double result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is double doubleVal)
            {
                result = doubleVal;
                return true;
            }

            if (value is float floatVal)
            {
                result = (double)floatVal;
                return true;
            }

            if (value is decimal decimalValue)
            {
                result = decimal.ToDouble(decimalValue);
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
            {
                result = doubleValue;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to decimal.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is decimal, returns the value.
        /// If the underlying value type is float and its value does not overflow,
        /// converts the value using Convert.ToDecimal method.
        /// If the underlying value type is double and its value does not overflow,
        /// converts the value using Convert.ToDecimal method.
        /// If the underlying value type is string, tries to parse value as decimal. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding 
        /// and parse as decimal.</remarks>
        public static bool TryConvertDecimal(this object value, out decimal result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is decimal decimalVal)
            {
                result = decimalVal;
                return true;
            }

            if (value is float floatVal && floatVal <= decimal.ToSingle(decimal.MaxValue) 
                && floatVal >= decimal.ToSingle(decimal.MinValue))
            {
                result = Convert.ToDecimal(floatVal);
                return true;
            }

            if (value is double doubleVal && doubleVal <= decimal.ToDouble(decimal.MaxValue)
                && doubleVal >= decimal.ToDouble(decimal.MinValue))
            {
                result = Convert.ToDecimal(doubleVal);
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace() &&
                decimal.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalValue))
            {
                result = decimalValue;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to DateTime.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is DateTime, returns the value.
        /// If the underlying value type is int64, returns new DateTime(ticks).
        /// If the underlying value type is string, tries to parse value as DateTime
        /// using default formats ("yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "HH:mm:ss"). 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding 
        /// and parse as DateTime.</remarks>
        public static bool TryConvertDateTime(this object value, out DateTime result)
        {
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is DateTime dateVal)
            {
                result = dateVal;
                return true;
            }

            if (value is long ticksVal)
            {
                result = new DateTime(ticksVal);
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace()
                && TryParseDateTime(strValue, _defaultDateTimeFormats, out DateTime dateValue))
            {
                result = dateValue;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to DateTime.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <param name="formats">DateTime format strings to use when parsing string values (cannot be null or empty)</param>
        /// <remarks>If the underlying value type is DateTime, returns the value.
        /// If the underlying value type is int64, returns new DateTime(ticks).
        /// If the underlying value type is string, tries to parse value as DateTime using the formats specified. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding 
        /// and parse as DateTime.</remarks>
        public static bool TryConvertDateTime(this object value, string[] formats, out DateTime result)
        {
            if (null == formats || formats.Length < 1) throw new ArgumentNullException(nameof(formats));
            if (formats.Any(f => f.IsNullOrWhiteSpace())) throw new ArgumentException(
                Resources.TypeConverters_TryConvertDateTime_Empty_Format_Strings, nameof(formats));

            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is DateTime dateVal)
            {
                result = dateVal;
                return true;
            }

            if (value is long ticksVal)
            {
                result = new DateTime(ticksVal);
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace()
                && TryParseDateTime(strValue, formats, out DateTime dateValue))
            {
                result = dateValue;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to byte array.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>Only succedes if the underlying value type is byte[] or the value is null.</remarks>
        public static bool TryConvertByteArray(this object value, out byte[] result)
        {
            if (value.IsNull())
            {
                result = null;
                return true;
            }

            if (value is byte[] arrayVal)
            {
                result = (byte[])arrayVal.Clone(); 
                return true;
            }
        
            result = null;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to the specified Enum type.
        /// </summary>
        /// <typeparam name="T">a type of the enum to convert to</typeparam>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is the specified Enum type, returns the value.
        /// If the underlying value type is integer and an enum value is defined for its value,
        /// returns the respective enum value.
        /// If the underlying value type is string, tries to parse value as the enum or as an integer. 
        /// if the underlying value type is byte[], tries to convert to string using UTF8 encoding 
        /// and parse the string as enum or integer.</remarks>
        public static bool TryConvertEnum<T>(this object value, out T result) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(string.Format(
                Properties.Resources.TypeConverters_ValueIsNotEnumeration, typeof(T).FullName));

            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value is T tVal)
            {
                result = tVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longVal) && longVal <= int.MaxValue
                && longVal >= int.MinValue && Enum.IsDefined(typeof(T), (int)longVal))
            {
                result = (T)(object)(int)longVal;
                return true;
            }

            if (TryReadString(value, out string strValue) && !strValue.IsNullOrWhiteSpace())
            {
                if (Enum.TryParse(strValue.Trim(), true, out result)) return true;
                if (int.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue)
                    && Enum.IsDefined(typeof(T), intValue))
                {
                    result = (T)(object)intValue;
                    return true;
                }
            }

            result = default;
            return false;
        }

        #endregion

        #region Nullable converters

        /// <summary>
        /// Converts (coerces) object value to boolean. Throws an exception if the object value 
        /// cannot be (reasonably) converted to boolean type. 
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertBool"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static bool? GetBooleanNullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertBool(out bool result)) return result;

            throw new ConversionException(value, typeof(bool));
        }

        /// <summary>
        /// Converts (coerces) object value to byte. Throws an exception if the object value 
        /// cannot be (reasonably) converted to byte type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertByte"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static byte? GetByteNullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertByte(out byte result)) return result;

            throw new ConversionException(value, typeof(byte));
        }

        /// <summary>
        /// Converts (coerces) object value to sbyte. Throws an exception if the object value 
        /// cannot be (reasonably) converted to sbyte type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertSByte"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static sbyte? GetSByteNullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertSByte(out sbyte result)) return result;

            throw new ConversionException(value, typeof(sbyte));
        }

        /// <summary>
        /// Converts (coerces) object value to char. Throws an exception if the object value 
        /// cannot be (reasonably) converted to char type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertChar"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static char? GetCharNullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertChar(out char result)) return result;

            throw new ConversionException(value, typeof(char));
        }

        /// <summary>
        /// Converts (coerces) object value to Guid. Throws an exception if the object value 
        /// cannot be (reasonably) converted to Guid type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertGuid"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static Guid? GetGuidNullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertGuid(out Guid result)) return result;

            throw new ConversionException(value, typeof(Guid));
        }

        /// <summary>
        /// Converts (coerces) object value to short. Throws an exception if the object value 
        /// cannot be (reasonably) converted to short type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertInt16"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static short? GetInt16Nullable(this object value)
        {
            if (value.IsNull()) return new short?();

            if (value.TryConvertInt16(out short result)) return result;

            throw new ConversionException(value, typeof(short));
        }

        /// <summary>
        /// Converts (coerces) object value to int. Throws an exception if the object value 
        /// cannot be (reasonably) converted to int type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertInt32"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static int? GetInt32Nullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertInt32(out int result)) return result;

            throw new ConversionException(value, typeof(int));
        }

        /// <summary>
        /// Converts (coerces) object value to long. Throws an exception if the object value 
        /// cannot be (reasonably) converted to long type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertInt64"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static long? GetInt64Nullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertInt64(out long result)) return result;

            throw new ConversionException(value, typeof(long));
        }

        /// <summary>
        /// Converts (coerces) object value to float. Throws an exception if the object value 
        /// cannot be (reasonably) converted to float type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertFloat"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static float? GetFloatNullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertFloat(out float result)) return result;

            throw new ConversionException(value, typeof(float));
        }

        /// <summary>
        /// Converts (coerces) object value to double. Throws an exception if the object value 
        /// cannot be (reasonably) converted to double type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertDouble"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static double? GetDoubleNullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertDouble(out double result)) return result;

            throw new ConversionException(value, typeof(double));
        }

        /// <summary>
        /// Converts (coerces) object value to decimal. Throws an exception if the object value 
        /// cannot be (reasonably) converted to decimal type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertDecimal"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static decimal? GetDecimalNullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertDecimal(out decimal result)) return result;

            throw new ConversionException(value, typeof(decimal));
        }

        /// <summary>
        /// Converts (coerces) object value to datetime. Throws an exception if the object value 
        /// cannot be (reasonably) converted to datetime type.
        /// Default datetime formats: "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "HH:mm:ss"
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="o:TryConvertDateTime"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static DateTime? GetDateTimeNullable(this object value)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertDateTime(out DateTime result)) return result;

            throw new ConversionException(value, typeof(DateTime));
        }

        /// <summary>
        /// Converts (coerces) object value to datetime using formats provided. 
        /// Throws an exception if the object value cannot be converted to datetime type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="formats">datetime formats to use if parsing a string value</param>
        /// <remarks>Uses <see cref="o:TryConvertDateTime"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static DateTime? GetDateTimeNullable(this object value, string[] formats)
        {
            if (value.IsNull()) return null;

            if (value.TryConvertDateTime(formats, out DateTime result)) return result;

            throw new ConversionException(value, typeof(DateTime));
        }

        /// <summary>
        /// Converts (coerces) object value to DateTimeOffset. Throws an exception if the object value 
        /// cannot be (reasonably) converted to DateTimeOffset type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="o:GetDateTimeNullable">GetDateTimeNullable</see> method to get a DateTime
        /// value and initializes a DateTimeOffset value with it.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static DateTimeOffset? GetDateTimeOffsetNullable(this object value)
        {
            var dateResult = value.GetDateTimeNullable();
            if (!dateResult.HasValue) return null;
            return new DateTimeOffset(DateTime.SpecifyKind(dateResult.Value, DateTimeKind.Utc));
        }

        /// <summary>
        /// Converts (coerces) object value to type T enum. Throws an exception if the object value 
        /// cannot be (reasonably) converted to type T enum. 
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertEnum{T}"/> method.</remarks>
        /// <exception cref="ArgumentException">Type T is not an enumeration.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static T? GetEnumNullable<T>(this object value) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(string.Format(
                Properties.Resources.TypeConverters_ValueIsNotEnumeration, typeof(T).FullName));

            if (value.IsNull()) return null;

            if (value.TryConvertEnum(out T result)) return result;

            throw new ConversionException(value, typeof(T));
        }

        #endregion

        #region Non Nullable Converters

        /// <summary>
        /// Converts (coerces) object value to boolean. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to boolean type. 
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetBooleanNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static bool GetBoolean(this object value)
        {
            return value.GetBooleanNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to byte. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to byte type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetByteNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static byte GetByte(this object value)
        {
            return value.GetByteNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to sbyte. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to sbyte type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetSByteNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static sbyte GetSByte(this object value)
        {
            return value.GetSByteNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to char. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to char type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetCharNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static char GetChar(this object value)
        {
            return value.GetCharNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to Guid. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to Guid type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetGuidNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static Guid GetGuid(this object value)
        {
            return value.GetGuidNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to short. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to short type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetInt16Nullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static short GetInt16(this object value)
        {
            return value.GetInt16Nullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to int. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to int type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetInt32Nullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static int GetInt32(this object value)
        {
            return value.GetInt32Nullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to long. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to long type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetInt64Nullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static long GetInt64(this object value)
        {
            return value.GetInt64Nullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to float. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to float type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetFloatNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static float GetFloat(this object value)
        {
            return value.GetFloatNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to double. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to double type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetDoubleNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static double GetDouble(this object value)
        {
            return value.GetDoubleNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to string. Never throws exceptions as any type
        /// is convertible to string via ToString method.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Returns null if the underlying value is null.
        /// Tries to convert to string using UTF8 encoding if the underlying value type is byte array.
        /// Invokes ToString in other cases.</remarks>
        public static string GetString(this object value)
        {
            if (value.IsNull()) return null;

            if (value is string strVal) return strVal;

            if (value is byte[] arrayVal && TryReadString(arrayVal, out string stringValue)) 
                return stringValue;

            return value.ToString();
        }

        /// <summary>
        /// Converts (coerces) object value to decimal. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to decimal type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetDecimalNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static decimal GetDecimal(this object value)
        {
            return value.GetDecimalNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to datetime. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to datetime type.
        /// Default datetime formats: "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "HH:mm:ss"
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="o:GetDateTimeNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static DateTime GetDateTime(this object value)
        {
            return value.GetDateTimeNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to datetime. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to datetime type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="formats">datetime formats to use if parsing a string value</param>
        /// <remarks>Uses <see cref="o:GetDateTimeNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static DateTime GetDateTime(this object value, string[] formats)
        {
            return value.GetDateTimeNullable(formats) ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts (coerces) object value to DateTimeOffset. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to DateTimeOffset type.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetDateTimeOffsetNullable"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static DateTimeOffset GetDateTimeOffset(this object value)
        {
            return value.GetDateTimeOffsetNullable() ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Converts object value to byte[]. Throws an exception if the object value 
        /// cannot be (reasonably) converted to byte[] type, i.e. is not byte[].
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="TryConvertByteArray"/> method.</remarks>
        /// <exception cref="FormatException">The value is not a byte array.</exception>
        public static byte[] GetByteArray(this object value)
        {
            if (value.TryConvertByteArray(out byte[] result)) return result;
            throw new ConversionException(value, typeof(byte[]));
        }

        /// <summary>
        /// Converts (coerces) object value to type T enum. Throws an exception if the object value 
        /// is null or cannot be (reasonably) converted to type T enum. 
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="value">an object value to convert</param>
        /// <remarks>Uses <see cref="GetEnumNullable{T}"/> method.</remarks>
        /// <exception cref="ArgumentException">Type T is not an enumeration.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public static T GetEnum<T>(this object value) where T : struct
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException(string.Format(Properties.Resources.TypeConverters_ValueIsNotEnumeration,
                    typeof(T).FullName));

            return value.GetEnumNullable<T>() ?? throw new ArgumentNullException();
        }
          
        #endregion



        /// <summary>
        /// Tries to convert (coerce) object value to int64.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is numeric (except for uint64), returns value as long.</remarks>
        private static bool TryConvertInt64Internal(this object value, out long result)
        {
            if (value.IsNull())
            {
                result = 0;
                return false;
            }

            if (value is long longVal)
            {
                result = longVal;
                return true;
            }

            if (value is sbyte sbyteVal)
            {
                result = sbyteVal;
                return true;
            }
            if (value is byte byteVal)
            {
                result = byteVal;
                return true;
            }
            if (value is short shortVal)
            {
                result = shortVal;
                return true;
            }
            if (value is ushort ushortVal)
            {
                result = ushortVal;
                return true;
            }
            if (value is int intVal)
            {
                result = intVal;
                return true;
            }
            if (value is uint uintVal)
            {
                result = uintVal;
                return true;
            }
            if (value is ulong ulongVal && ulongVal <= long.MaxValue)
            {
                result = (long)ulongVal;
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>
        /// Tries to convert (coerce) object value to uint64.
        /// </summary>
        /// <param name="value">an object value to convert</param>
        /// <param name="result">converted value if success</param>
        /// <remarks>If the underlying value type is uint64, returns the value.
        /// If the underlying value type is numeric and non negative, returns value as ulong.</remarks>
        private static bool TryConvertUInt64Internal(this object value, out ulong result)
        {
            if (value.IsNull())
            {
                result = 0;
                return false;
            }

            if (value is ulong ulongVal)
            {
                result = ulongVal;
                return true;
            }

            if (value.TryConvertInt64Internal(out long longResult) && longResult >= 0)
            {
                result = (ulong) longResult;
                return true;
            }

            result = 0;
            return false;
        }

        private static bool TryReadString(byte[] source, out string result)
        {
            if (null == source || source.Length < 1)
            {
                result = null;
                return false;
            }
            try
            {
                result = Encoding.UTF8.GetString(source);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        private static bool TryReadString(object source, out string result)
        {
            if (source is string strVal)
            {
                result = strVal;
                return true;
            }

            if (source is byte[] arrayVal) return TryReadString(arrayVal, out result);

            result = null;
            return false;
        }

        private static bool TryParseDateTime(string source, string[] formats, out DateTime result)
        {
            if (null == formats || formats.Length < 1) throw new ArgumentNullException(nameof(formats));

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(source, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out result)) return true;
            }

            result = DateTime.MinValue;
            return false;
        }

    }
}
