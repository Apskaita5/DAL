using System;
using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core
{
    public interface ILightDataReader
    {
        Task<bool> ReadAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets the name for the field to find.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        string GetName(int i);

        /// <summary>
        /// Gets the data type information for the specified field. See <see cref="LightDataColumn.NativeDataType"/>.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        string GetDataTypeName(int i);

        /// <summary>
        /// Gets the Type information corresponding to the type of Object that would be returned from GetValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        Type GetFieldType(int i);

        /// <summary>
        /// Return the value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        object GetValue(int i);

        /// <summary>
        /// Return the index of the named field.
        /// </summary>
        /// <param name="name">The name of the field to find.</param>
        int GetOrdinal(string name);

        /// <summary>
        /// Closes underlying connection (unless within a transaction).
        /// Shall always be invoked after fetch is completed.
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertBool"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        bool GetBoolean(int i);

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        byte GetByte(int i);

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        sbyte GetSByte(int i);

        /// <summary>
        /// Not implemented method of IDataRecord.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="fieldOffset"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferoffset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length);

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        char GetChar(int i);

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        Guid GetGuid(int i);

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        short GetInt16(int i);

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        int GetInt32(int i);

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        long GetInt64(int i);

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        float GetFloat(int i);

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        double GetDouble(int i);

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.GetString"/> method
        /// (that never throws as all types are convertible to string).</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        string GetString(int i);

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        decimal GetDecimal(int i);

        /// <summary>
        /// Gets the data stored in the column specified as DateTime.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="o:TypeConverters.TryConvertDateTime"/> method.
        /// Uses either default formats or formats specified by 
        /// <see cref="LightDataTable.DateTimeFormats">parent table</see>.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        DateTime GetDateTime(int i);

        /// <summary>
        /// Gets the data stored in the column specified as DateTimeOffset.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="o:GetDateTime"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        DateTimeOffset GetDateTimeOffset(int i);

        /// <summary>
        /// Gets the data stored in the column specified as byte array.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByteArray"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        byte[] GetByteArray(int i);

        /// <summary>
        /// Gets the data stored in the column specified as an enumeration of type T. 
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertEnum{T}"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format 
        /// or string/int value is not defined for the enumeration.</exception>
        /// <exception cref="ArgumentException">Type T is not an enumeration.</exception>
        T GetEnum<T>(int i) where T : struct;

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertBool"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        bool? GetNullableBoolean(int i);

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        byte? GetByteNullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        sbyte? GetSByteNullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        char? GetCharNullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        Guid? GetGuidNullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        short? GetInt16Nullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        int? GetInt32Nullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        long? GetInt64Nullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        float? GetFloatNullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        double? GetDoubleNullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        decimal? GetDecimalNullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as DateTime.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="o:TypeConverters.TryConvertDateTime"/> method.
        /// Uses either default formats or formats specified by 
        /// <see cref="LightDataTable.DateTimeFormats">parent table</see>.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        DateTime? GetDateTimeNullable(int i);

        /// <summary>
        /// Gets the data stored in the column specified as an enumeration of type T. 
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertBool"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format 
        /// or string/int value is not defined for the enumeration.</exception>
        /// <exception cref="ArgumentException">Type T is not an enumeration.</exception>
        T? GetEnumNullable<T>(int i) where T : struct;

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        bool GetBoolean(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        byte GetByte(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        sbyte GetSByte(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        char GetChar(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        Guid GetGuid(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        short GetInt16(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        int GetInt32(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        long GetInt64(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        float GetFloat(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        double GetDouble(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.GetString"/> method
        /// (that never throws as all types are convertible to string).</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        string GetString(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        decimal GetDecimal(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as DateTime.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="o:TypeConverters.TryConvertDateTime"/> method.
        /// Uses either default formats or formats specified by 
        /// <see cref="LightDataTable.DateTimeFormats">parent table</see>.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        DateTime GetDateTime(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as DateTimeOffset.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="o:GetDateTime"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        DateTimeOffset GetDateTimeOffset(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as byte[].
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByteArray"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        byte[] GetByteArray(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as an enumeration of type T. 
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertEnum{T}"/> method.</remarks>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format 
        /// or string/int value is not defined for the enumeration.</exception>
        /// <exception cref="ArgumentException">Type T is not an enumeration.</exception>
        T GetEnum<T>(string columnName) where T : struct;

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertBool"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        bool? GetBooleanNullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        byte? GetByteNullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        sbyte? GetSByteNullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        char? GetCharNullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        Guid? GetGuidNullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        short? GetInt16Nullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        int? GetInt32Nullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        long? GetInt64Nullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        float? GetFloatNullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        double? GetDoubleNullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        decimal? GetDecimalNullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as DateTime.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="o:TryConvertDateTime"/> method.
        /// Tries to parse the value using either default formats or formats specified by 
        /// <see cref="LightDataTable.DateTimeFormats">parent table</see>
        /// if the underlying value type is string.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        DateTime? GetDateTimeNullable(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as an enumeration of type T. 
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertEnum{T}"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        T? GetEnumNullable<T>(string columnName) where T : struct;

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartible data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryGetBoolean"/> method.</remarks>
        bool TryGetBoolean(int i, out bool result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        bool TryGetByte(int i, out byte result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        bool TryGetSByte(int i, out sbyte result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        bool TryGetChar(int i, out char result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or Guid.Empty 
        /// if the conversion failed.</param>   
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        bool TryGetGuid(int i, out Guid result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        bool TryGetInt16(int i, out short result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>    
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        bool TryGetInt32(int i, out int result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        bool TryGetInt64(int i, out Int64 result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        bool TryGetFloat(int i, out float result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        bool TryGetDouble(int i, out double result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>    
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        bool TryGetDecimal(int i, out decimal result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as DateTime.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TypeConverters.TryConvertDateTime"/> method.
        /// Uses either default formats or formats specified by 
        /// <see cref="LightDataTable.DateTimeFormats">parent table</see>.</remarks>
        bool TryGetDateTime(int i, out DateTime result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as byte[].
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or null 
        /// if the conversion failed.</param>    
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByteArray"/> method.</remarks>
        bool TryGetByteArray(int i, out byte[] result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as an enum value of type T.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentException">Type T is not an enumeration.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertEnum{T}"/> method.</remarks>
        bool TryGetEnum<T>(int i, out T result, bool ignoreFormatException = false) where T : struct;

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertBool"/> method.</remarks>
        bool TryGetBoolean(string columnName, out bool result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        bool TryGetByte(string columnName, out byte result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>      
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        bool TryGetSByte(string columnName, out sbyte result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        bool TryGetChar(string columnName, out char result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or Guid.Empty 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        bool TryGetGuid(string columnName, out Guid result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        bool TryGetInt16(string columnName, out short result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        bool TryGetInt32(string columnName, out int result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>    
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        bool TryGetInt64(string columnName, out Int64 result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        bool TryGetFloat(string columnName, out float result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        bool TryGetDouble(string columnName, out double result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        bool TryGetDecimal(string columnName, out decimal result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as DateTime.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TypeConverters.TryConvertDateTime"/> method.
        /// Uses either default formats or formats specified by 
        /// <see cref="LightDataTable.DateTimeFormats">parent table</see>.</remarks>
        bool TryGetDateTime(string columnName, out DateTime result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as byte[].
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or null 
        /// if the conversion failed.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByteArray"/> method.</remarks>
        bool TryGetByteArray(string columnName, out byte[] result, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as an enum value of type T.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="result">When this method returns, contains the converted value equivalent 
        /// of the value contained in the column, if the conversion succeeded, or default value 
        /// if the conversion failed.</param>    
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentException">Type T is not an enumeration.</exception>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        bool TryGetEnum<T>(string columnName, out T result, bool ignoreFormatException = false) where T : struct;

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param> 
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetBoolean"/> method.</remarks>
        bool GetBooleanOrDefault(int i, bool defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>  
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetByte"/> method.</remarks>
        byte GetByteOrDefault(int i, byte defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>        
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetSByte"/> method.</remarks>
        sbyte GetSByteOrDefault(int i, sbyte defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param> 
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetChar"/> method.</remarks>
        char GetCharOrDefault(int i, char defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>    
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetGuid"/> method.</remarks>
        Guid GetGuidOrDefault(int i, Guid defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>  
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetInt16"/> method.</remarks>
        short GetInt16OrDefault(int i, short defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetInt32"/> method.</remarks>
        int GetInt32OrDefault(int i, int defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>     
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetInt64"/> method.</remarks>
        Int64 GetInt64OrDefault(int i, Int64 defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param> 
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetFloat"/> method.</remarks>
        float GetFloatOrDefault(int i, float defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param> 
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetDouble"/> method.</remarks>
        double GetDoubleOrDefault(int i, double defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:GetString"/> method.</remarks>
        string GetStringOrDefault(int i, string defaultValue);

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// If the conversion fails, returns string.Empty.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:GetString"/> method.</remarks>
        string GetStringOrDefault(int i);

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>   
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetDecimal"/> method.</remarks>
        decimal GetDecimalOrDefault(int i, decimal defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as DateTime.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>     
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TypeConverters.TryConvertDateTime"/> method.
        /// Uses either default formats or formats specified by 
        /// <see cref="LightDataTable.DateTimeFormats">parent table</see>.</remarks>
        DateTime GetDateTimeOrDefault(int i, DateTime defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as byte[].
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>   
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetByteArray"/> method.</remarks>
        byte[] GetByteArrayOrDefault(int i, byte[] defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as an enum value of type T.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>         
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetEnum"/> method.</remarks>
        T GetEnumOrDefault<T>(int i, T defaultValue, bool ignoreFormatException = false) where T : struct;

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>    
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetBoolean"/> method.</remarks>
        bool GetBooleanOrDefault(string columnName, bool defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetByte"/> method.</remarks>
        byte GetByteOrDefault(string columnName, byte defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>   
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetSByte"/> method.</remarks>
        sbyte GetSByteOrDefault(string columnName, sbyte defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>  
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetChar"/> method.</remarks>
        char GetCharOrDefault(string columnName, char defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>     
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetGuid"/> method.</remarks>
        Guid GetGuidOrDefault(string columnName, Guid defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>   
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetInt16"/> method.</remarks>
        short GetInt16OrDefault(string columnName, short defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>    
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetInt32"/> method.</remarks>
        int GetInt32OrDefault(string columnName, int defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>  
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>   
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetInt64"/> method.</remarks>
        Int64 GetInt64OrDefault(string columnName, Int64 defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>  
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetFloat"/> method.</remarks>
        float GetFloatOrDefault(string columnName, float defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>   
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param> 
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetDouble"/> method.</remarks>
        double GetDoubleOrDefault(string columnName, double defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>      
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:GetString">TryGetString</see> method.</remarks>
        string GetStringOrDefault(string columnName, string defaultValue);

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// If the conversion fails, returns string.Empty.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:GetString">TryGetString</see> method.</remarks>
        string GetStringOrDefault(string columnName);

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param> 
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetDecimal"/> method.</remarks>
        decimal GetDecimalOrDefault(string columnName, decimal defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as DateTime.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param> 
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetDateTime"/> method.
        /// Uses either default formats or formats specified by 
        /// <see cref="LightDataTable.DateTimeFormats">parent table</see>.</remarks>
        DateTime GetDateTimeOrDefault(string columnName, DateTime defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as byte[].
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param> 
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param> 
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetByteArray"/> method.</remarks>
        byte[] GetByteArrayOrDefault(string columnName, byte[] defaultValue, bool ignoreFormatException = false);

        /// <summary>
        /// Gets the data stored in the column specified as an enum value of type T.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="columnName">A name of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <param name="ignoreFormatException">whether to ignore FormatException, i.e. when the
        /// conversion fails not due to null values but due to incompartimble data formats, 
        /// e.g. double instead of DateTime</param>    
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:TryGetEnum"/> method.</remarks>
        T GetEnumOrDefault<T>(string columnName, T defaultValue, bool ignoreFormatException = false) where T : struct;
    }
}