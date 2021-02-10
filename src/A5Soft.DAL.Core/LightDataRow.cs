using A5Soft.DAL.Core.Serialization;
using A5Soft.DAL.Core.TypeConverters;
using System;
using System.Data;
using System.Reflection;

namespace A5Soft.DAL.Core
{
    [Serializable]
    public sealed class LightDataRow : IDataRecord
    {

        private readonly Object[] _data = null;
        private readonly LightDataTable _table = null;


        #region Properties

        /// <summary>
        /// Gets the LightDataTable for which this row has a schema. (deattached rows are not allowed)
        /// </summary>
        public LightDataTable Table
        {
            get { return _table; }
        }

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        public int FieldCount
        {
            get
            {
                if (null == _data) return 0;
                return _data.Length;
            }
        }

        /// <summary>
        /// Gets or sets the data stored in the specified LightDataColumn.
        /// </summary>
        /// <param name="column">A LightDataColumn that contains the data.</param>
        /// <exception cref="ArgumentNullException">The column is null.</exception>
        /// <exception cref="ArgumentException">The column does not belong to this table.</exception>
        /// <exception cref="InvalidCastException">The item data type is invalid. Must have Clone method.</exception>
        /// <exception cref="ArgumentException">The data types of the value and the column do not match.</exception>
        /// <exception cref="InvalidOperationException">An edit tried to change the value of a read-only column.</exception>
        /// <remarks>Always gets (sets) a copy/clone of the value, not a reference. 
        /// Otherwise an indirect reference would be created to the table 
        /// which is unacceptable for a data transfer object.</remarks>
        public object this[LightDataColumn column]
        {
            get
            {
                if (column.IsNull()) throw new ArgumentNullException(nameof(column));
                if (!Object.ReferenceEquals(_table, column.Table))
                    throw new ArgumentException(Properties.Resources.LightDataRow_ColumnDoesNotBelongToTable);

                return CloneValue(_data[column.Ordinal]);
            }
            set
            {
                if (column.IsNull()) throw new ArgumentNullException(nameof(column));
                if (!Object.ReferenceEquals(_table, column.Table))
                    throw new ArgumentException(Properties.Resources.LightDataRow_ColumnDoesNotBelongToTable);
                if (column.ReadOnly)
                    throw new InvalidOperationException(Properties.Resources.LightDataRow_CannotEditReadOnlyColumn);

                SetValue(column.Ordinal, value);
            }
        }

        /// <summary>
        /// Gets or sets the data stored in the column specified by index.
        /// </summary>
        /// <param name="columnIndex">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The columnIndex argument is out of range.</exception>
        /// <exception cref="InvalidCastException">The item data type is invalid. Must have Clone method.</exception>
        /// <exception cref="ArgumentException">The data types of the value and the column do not match.</exception>
        /// <exception cref="InvalidOperationException">An edit tried to change the value of a read-only column.</exception>
        /// <remarks>Always gets (sets) a copy/clone of the value, not a reference. 
        /// Otherwise an indirect reference would be created to the table 
        /// which is unacceptable for a data transfer object.</remarks>
        public object this[int columnIndex]
        {
            get
            {
                EnsureIndexIsValid(columnIndex);
                return CloneValue(_data[columnIndex]);
            }
            set
            {
                EnsureIndexIsValid(columnIndex);
                if (_table.Columns[columnIndex].ReadOnly)
                    throw new InvalidOperationException(Properties.Resources.LightDataRow_CannotEditReadOnlyColumn);
                SetValue(columnIndex, value);
            }
        }

        /// <summary>
        /// Gets or sets the data stored in the column specified by name.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <exception cref="ArgumentException">The column specified by columnName cannot be found.</exception>
        /// <exception cref="InvalidCastException">The item data type is invalid. Must have Clone method.</exception>
        /// <exception cref="ArgumentException">The data types of the value and the column do not match.</exception>
        /// <exception cref="InvalidOperationException">An edit tried to change the value of a read-only column.</exception>
        /// <remarks>Always gets (sets) a copy/clone of the value, not a reference. 
        /// Otherwise an indirect reference would be created to the table 
        /// which is unacceptable for a data transfer object.</remarks>
        public object this[string columnName]
        {
            get
            {
                var column = _table.Columns[columnName];
                if (column.IsNull())
                    throw new ArgumentException(string.Format(Properties.Resources.LightDataRow_ColumnDoesNotExist, columnName));
                return this[column];
            }
            set
            {
                var column = _table.Columns[columnName];
                if (column.IsNull())
                    throw new ArgumentException(string.Format(Properties.Resources.LightDataRow_ColumnDoesNotExist, columnName));
                SetValue(column.Ordinal, value);
            }
        } 
        
        #endregion

        #region Constructors

        /// <summary>
        /// Creates new empty data row for a table.
        /// </summary>
        /// <param name="table">table to create a new row for</param>
        /// <remarks>For internal use only.</remarks>
        /// <exception cref="ArgumentNullException">Parameter table is not specified.</exception>
        /// <exception cref="InvalidOperationException">Parent table has no columns.</exception>
        internal LightDataRow(LightDataTable table)
        {
            if (table.IsNull()) throw new ArgumentNullException(nameof(table));
            if (table.Columns.Count < 1)
                throw new InvalidOperationException(Properties.Resources.LightDataRow_TableHasNoColumns);

            _table = table;

            _data = new Object[table.Columns.Count - 1];
        }

        /// <summary>
        /// Creates new data row for a table and fills it with values using data reader.
        /// </summary>
        /// <param name="table">table to create a new row for</param>
        /// <param name="reader">data reader to use</param>
        /// <remarks>For internal use only.</remarks>
        /// <exception cref="ArgumentNullException">Parameter table is not specified.</exception>
        /// <exception cref="ArgumentNullException">Parameter reader is not specified.</exception>
        /// <exception cref="InvalidOperationException">Parent table has no columns.</exception>
        /// <exception cref="ArgumentException">Reader field count does not match table column count.</exception>
        internal LightDataRow(LightDataTable table, IDataReader reader)
        {
            _table = table;

            _data = new object[reader.FieldCount];

            for (int i = 0; i < reader.FieldCount; i++)
            {
                _data[i] = reader.GetValue(i);
            }
        }

        /// <summary>
        /// Creates new data row for a table and fills it with values in the specified array.
        /// </summary>
        /// <param name="table">table to create a new row for</param>
        /// <param name="values">values to set</param>
        /// <exception cref="ArgumentNullException">Parameter table is not specified.</exception>
        /// <exception cref="ArgumentNullException">Parameter values is not specified.</exception>
        /// <exception cref="InvalidOperationException">Parent table has no columns.</exception>
        /// <exception cref="ArgumentException">Value array field count does not match table column count.</exception>
        internal LightDataRow(LightDataTable table, Object[] values)
        {
            if (table.IsNull()) throw new ArgumentNullException(nameof(table));
            if (null == values || values.Length < 1)
                throw new ArgumentNullException(nameof(values));
            if (table.Columns.Count < 1)
                throw new InvalidOperationException(Properties.Resources.LightDataRow_TableHasNoColumns);
            if (table.Columns.Count != values.Length)
                throw new ArgumentException(Properties.Resources.LightDataRow_ArrayLengthInvalid);

            _table = table;

            _data = new object[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                _data[i] = values[i];
            }
        }

        internal LightDataRow(LightDataTable table, LightDataRowProxy proxy)
        {
            if (table.IsNull()) throw new ArgumentNullException(nameof(table));
            if (proxy.IsNull() || null == proxy.Values || proxy.Values.Length < 1)
                throw new ArgumentNullException(nameof(proxy));

            _data = new object[proxy.Values.Length];

            for (int i = 0; i < proxy.Values.Length; i++)
            {
                _data[i] = proxy.Values[i];
            }
        }

        #endregion
       
        #region Public Methods

        /// <summary>
        /// Gets the name for the field to find.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        public string GetName(int i)
        {
            EnsureIndexIsValid(i);
            return _table.Columns[i].ColumnName;
        }

        /// <summary>
        /// Gets the data type information for the specified field. See <see cref="LightDataColumn.NativeDataType"/>.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        public string GetDataTypeName(int i)
        {
            EnsureIndexIsValid(i);
            return _table.Columns[i].NativeDataType;
        }

        /// <summary>
        /// Gets the Type information corresponding to the type of Object that would be returned from GetValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        public Type GetFieldType(int i)
        {
            EnsureIndexIsValid(i);
            if (null != _data[i]) return _data[i].GetType();
            return _table.Columns[i].DataType;
        }

        /// <summary>
        /// Return the value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        public object GetValue(int i)
        {
            EnsureIndexIsValid(i);
            return this[i];
        }

        /// <summary>
        /// Not implemented method of IDataRecord.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the index of the named field.
        /// </summary>
        /// <param name="name">The name of the field to find.</param>
        public int GetOrdinal(string name)
        {
            return _table.Columns.IndexOf(name);
        }

        /// <summary>
        /// Not implemented method of IDataRecord.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets a value that indicates whether the specified column contains a null or a DbNull value.
        /// </summary>
        /// <param name="column">A LightDataColumn.</param>
        /// <exception cref="ArgumentNullException">The column is null.</exception>
        /// <exception cref="ArgumentException">The column does not belong to this table.</exception>
        public bool IsNull(LightDataColumn column)
        {
            if (column.IsNull()) throw new ArgumentNullException(nameof(column));
            if (!Object.ReferenceEquals(_table, column.Table))
                throw new ArgumentException(Properties.Resources.LightDataRow_ColumnDoesNotBelongToTable);

            return _data[column.Ordinal].IsNull();
        }

        /// <summary>
        /// Gets a value that indicates whether the column at the specified index contains a null or a DbNull value.
        /// </summary>
        /// <param name="columnIndex">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The columnIndex argument is out of range.</exception>
        public bool IsNull(int columnIndex)
        {
            EnsureIndexIsValid(columnIndex);
            return _data[columnIndex].IsNull();
        }

        /// <summary>
        /// Gets a value that indicates whether the named column contains a null or a DbNull value.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <exception cref="ArgumentException">The column specified by columnName cannot be found.</exception>
        public bool IsNull(string columnName)
        {
            var column = _table.Columns[columnName];
            if (column.IsNull())
                throw new ArgumentException(string.Format(Properties.Resources.LightDataRow_ColumnDoesNotExist, columnName));
            return _data[column.Ordinal].IsNull();
        }

        /// <summary>
        /// Gets a value that indicates whether the specified column contains a DbNull value.
        /// </summary>
        /// <param name="column">A LightDataColumn.</param>
        /// <exception cref="IndexOutOfRangeException">The index argument is out of range.</exception>
        public bool IsDBNull(LightDataColumn column)
        {
            if (column.IsNull()) throw new ArgumentNullException(nameof(column));
            if (!Object.ReferenceEquals(_table, column.Table))
                throw new ArgumentException(Properties.Resources.LightDataRow_ColumnDoesNotBelongToTable);

            var value = _data[column.Ordinal];
            return (null != value && value == DBNull.Value);
        }

        /// <summary>
        /// Gets a value that indicates whether the specified column contains a DbNull value.
        /// </summary>
        /// <param name="columnIndex">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The index argument is out of range.</exception>
        public bool IsDBNull(int columnIndex)
        {
            EnsureIndexIsValid(columnIndex);
            var value = _data[columnIndex];
            return (null != value && value == DBNull.Value);
        }

        /// <summary>
        /// Gets a value that indicates whether the specified column contains a DbNull value.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The index argument is out of range.</exception>
        public bool IsDBNull(string columnName)
        {
            if (columnName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(columnName));

            var column = _table.Columns[columnName];
            if (column.IsNull())
                throw new ArgumentException(string.Format(Properties.Resources.LightDataRow_ColumnDoesNotExist, columnName));

            var value = _data[column.Ordinal];
            return (null != value && value == DBNull.Value);
        }

        #endregion

        #region Type Converters

        #region By Index

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertBool"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public bool GetBoolean(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertBool(out bool result)) return result;
            throw new ConversionException(value, typeof(bool), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public byte GetByte(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertByte(out byte result)) return result;
            throw new ConversionException(value, typeof(byte), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public sbyte GetSByte(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertSByte(out sbyte result)) return result;
            throw new ConversionException(value, typeof(sbyte), i);
        }

        /// <summary>
        /// Not implemented method of IDataRecord.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="fieldOffset"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferoffset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public char GetChar(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertChar(out char result)) return result;
            throw new ConversionException(value, typeof(char), i);
        }

        /// <summary>
        /// Not implemented method of IDataRecord.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="fieldoffset"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferoffset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public Guid GetGuid(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertGuid(out Guid result)) return result;
            throw new ConversionException(value, typeof(Guid), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public short GetInt16(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertInt16(out short result)) return result;
            throw new ConversionException(value, typeof(short), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public int GetInt32(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertInt32(out int result)) return result;
            throw new ConversionException(value, typeof(int), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public long GetInt64(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertInt64(out long result)) return result;
            throw new ConversionException(value, typeof(long), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public float GetFloat(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertFloat(out float result)) return result;
            throw new ConversionException(value, typeof(float), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public double GetDouble(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertDouble(out double result)) return result;
            throw new ConversionException(value, typeof(double), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.GetString"/> method
        /// (that never throws as all types are convertible to string).</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        public string GetString(int i)
        {
            return GetValueForCast(i, false).GetString();
        }

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public decimal GetDecimal(int i)
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertDecimal(out decimal result)) return result;
            throw new ConversionException(value, typeof(decimal), i);
        }

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
        public DateTime GetDateTime(int i)
        {
            var value = GetValueForCast(i, true);

            if (_table.DateTimeFormats.IsNull() || _table.DateTimeFormats.Count < 1)
            {
                if (value.TryConvertDateTime(out DateTime result)) return result;
            }
            else
            {
                if (value.TryConvertDateTime(_table.DateTimeFormats.ToArray(), out DateTime result)) return result;
            }
            throw new ConversionException(value, typeof(decimal), i); 
        }

        /// <summary>
        /// Gets the data stored in the column specified as DateTimeOffset.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="o:GetDateTime"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public DateTimeOffset GetDateTimeOffset(int i)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(GetDateTime(i), DateTimeKind.Utc));
        }

        /// <summary>
        /// Gets the data stored in the column specified as byte array.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByteArray"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public byte[] GetByteArray(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.TryConvertByteArray(out byte[] result)) return result;
            throw new ConversionException(value, typeof(byte[]), i);
        }

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
        public T GetEnum<T>(int i) where T : struct
        {
            var value = GetValueForCast(i, true);
            if (value.TryConvertEnum(out T result)) return result;
            throw new ConversionException(value, typeof(T), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertBool"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public bool? GetNullableBoolean(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertBool(out bool result)) return result;
            throw new ConversionException(value, typeof(bool), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public byte? GetByteNullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertByte(out byte result)) return result;
            throw new ConversionException(value, typeof(byte), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public sbyte? GetSByteNullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertSByte(out sbyte result)) return result;
            throw new ConversionException(value, typeof(sbyte), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public char? GetCharNullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertChar(out char result)) return result;
            throw new ConversionException(value, typeof(char), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public Guid? GetGuidNullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertGuid(out Guid result)) return result;
            throw new ConversionException(value, typeof(Guid), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public short? GetInt16Nullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertInt16(out short result)) return result;
            throw new ConversionException(value, typeof(short), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public int? GetInt32Nullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertInt32(out int result)) return result;
            throw new ConversionException(value, typeof(int), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public long? GetInt64Nullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertInt64(out long result)) return result;
            throw new ConversionException(value, typeof(long), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public float? GetFloatNullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertFloat(out float result)) return result;
            throw new ConversionException(value, typeof(float), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public double? GetDoubleNullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertDouble(out double result)) return result;
            throw new ConversionException(value, typeof(double), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public decimal? GetDecimalNullable(int i)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertDecimal(out decimal result)) return result;
            throw new ConversionException(value, typeof(decimal), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as DateTime.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="o:TypeConverters.TryConvertDateTime"/> method.
        /// Uses either default formats or formats specified by 
        /// <see cref="LightDataTable.DateTimeFormats">parent table</see>.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public DateTime? GetDateTimeNullable(int i)
        {
            var value = GetValueForCast(i, false);

            if (value.IsNull()) return null;

            if (_table.DateTimeFormats.IsNull() || _table.DateTimeFormats.Count < 1)
            {
                if (value.TryConvertDateTime(out DateTime result)) return result;
            }
            else
            {
                if (value.TryConvertDateTime(_table.DateTimeFormats.ToArray(), out DateTime result)) return result;
            }
            throw new ConversionException(value, typeof(DateTime), i);
        }

        /// <summary>
        /// Gets the data stored in the column specified as an enumeration of type T. 
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="i">The zero-based index of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertBool"/> method.</remarks>
        /// <exception cref="FormatException">The object value is not in an appropriate format 
        /// or string/int value is not defined for the enumeration.</exception>
        /// <exception cref="ArgumentException">Type T is not an enumeration.</exception>
        public T? GetEnumNullable<T>(int i) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(string.Format(
                Properties.Resources.TypeConverters_ValueIsNotEnumeration, typeof(T).FullName));

            var value = GetValueForCast(i, false);
            if (value.IsNull()) return null;
            if (value.TryConvertEnum(out T result)) return result;

            throw new ConversionException(value, typeof(T), i);
        }

        #endregion

        #region By Column Name

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public bool GetBoolean(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertBool(out bool result)) return result;
            throw new ConversionException(value, typeof(bool), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public byte GetByte(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertByte(out byte result)) return result;
            throw new ConversionException(value, typeof(byte), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public sbyte GetSByte(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertSByte(out sbyte result)) return result;
            throw new ConversionException(value, typeof(sbyte), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public char GetChar(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertChar(out char result)) return result;
            throw new ConversionException(value, typeof(char), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public Guid GetGuid(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertGuid(out Guid result)) return result;
            throw new ConversionException(value, typeof(Guid), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public short GetInt16(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertInt16(out short result)) return result;
            throw new ConversionException(value, typeof(short), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public int GetInt32(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertInt32(out int result)) return result;
            throw new ConversionException(value, typeof(int), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public long GetInt64(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertInt64(out long result)) return result;
            throw new ConversionException(value, typeof(long), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public float GetFloat(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertFloat(out float result)) return result;
            throw new ConversionException(value, typeof(float), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public double GetDouble(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertDouble(out double result)) return result;
            throw new ConversionException(value, typeof(double), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.GetString"/> method
        /// (that never throws as all types are convertible to string).</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        public string GetString(string columnName)
        {
            return GetValueForCast(columnName, false).GetString();
        }

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public decimal GetDecimal(string columnName)
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertDecimal(out decimal result)) return result;
            throw new ConversionException(value, typeof(decimal), columnName);
        }

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
        public DateTime GetDateTime(string columnName)
        {
            var value = GetValueForCast(columnName, true);

            if (_table.DateTimeFormats.IsNull() || _table.DateTimeFormats.Count < 1)
            {
                if (value.TryConvertDateTime(out DateTime result)) return result;
            }
            else
            {
                if (value.TryConvertDateTime(_table.DateTimeFormats.ToArray(), out DateTime result)) return result;
            }
            throw new ConversionException(value, typeof(decimal), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as DateTimeOffset.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="o:GetDateTime"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Failed to convert null value.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public DateTimeOffset GetDateTimeOffset(string columnName)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(GetDateTime(columnName), DateTimeKind.Utc));
        }

        /// <summary>
        /// Gets the data stored in the column specified as byte[].
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByteArray"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public byte[] GetByteArray(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.TryConvertByteArray(out byte[] result)) return result;
            throw new ConversionException(value, typeof(byte[]), columnName);
        }

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
        public T GetEnum<T>(string columnName) where T : struct
        {
            var value = GetValueForCast(columnName, true);
            if (value.TryConvertEnum(out T result)) return result;
            throw new ConversionException(value, typeof(T), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as boolean.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertBool"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public bool? GetBooleanNullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertBool(out bool result)) return result;
            throw new ConversionException(value, typeof(bool), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as byte.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public byte? GetByteNullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertByte(out byte result)) return result;
            throw new ConversionException(value, typeof(byte), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as sbyte.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertSByte"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public sbyte? GetSByteNullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertSByte(out sbyte result)) return result;
            throw new ConversionException(value, typeof(sbyte), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as char.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertChar"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public char? GetCharNullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertChar(out char result)) return result;
            throw new ConversionException(value, typeof(char), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Guid.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertGuid"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public Guid? GetGuidNullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertGuid(out Guid result)) return result;
            throw new ConversionException(value, typeof(Guid), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int16.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt16"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public short? GetInt16Nullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertInt16(out short result)) return result;
            throw new ConversionException(value, typeof(short), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int32.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt32"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public int? GetInt32Nullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertInt32(out int result)) return result;
            throw new ConversionException(value, typeof(int), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as Int64.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertInt64"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public long? GetInt64Nullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertInt64(out long result)) return result;
            throw new ConversionException(value, typeof(long), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as float.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertFloat"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public float? GetFloatNullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertFloat(out float result)) return result;
            throw new ConversionException(value, typeof(float), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as double.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDouble"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public double? GetDoubleNullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertDouble(out double result)) return result;
            throw new ConversionException(value, typeof(double), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as decimal.
        /// </summary>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertDecimal"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public decimal? GetDecimalNullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertDecimal(out decimal result)) return result;
            throw new ConversionException(value, typeof(decimal), columnName);
        }

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
        public DateTime? GetDateTimeNullable(string columnName)
        {
            var value = GetValueForCast(columnName, false);

            if (value.IsNull()) return null;

            if (_table.DateTimeFormats.IsNull() || _table.DateTimeFormats.Count < 1)
            {
                if (value.TryConvertDateTime(out DateTime result)) return result;
            }
            else
            {
                if (value.TryConvertDateTime(_table.DateTimeFormats.ToArray(), out DateTime result)) return result;
            }
            throw new ConversionException(value, typeof(DateTime), columnName);
        }

        /// <summary>
        /// Gets the data stored in the column specified as an enumeration of type T. 
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="columnName">A name of the column.</param>
        /// <remarks>Uses <see cref="TypeConverters.TryConvertEnum{T}"/> method.</remarks>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <exception cref="FormatException">The object value is not in an appropriate format.</exception>
        public T? GetEnumNullable<T>(string columnName) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(string.Format(
                Properties.Resources.TypeConverters_ValueIsNotEnumeration, typeof(T).FullName));

            var value = GetValueForCast(columnName, false);
            if (value.IsNull()) return null;
            if (value.TryConvertEnum(out T result)) return result;
            throw new ConversionException(value, typeof(T), columnName);
        }

        #endregion

        #region Try Get By Index

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
        public bool TryGetBoolean(int i, out bool result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertBool(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(bool), i);

            result = default;
            return false;
        }

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
        public bool TryGetByte(int i, out byte result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertByte(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(byte), i);

            result = default;
            return false;
        }

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
        public bool TryGetSByte(int i, out sbyte result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertSByte(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(sbyte), i);

            result = default;
            return false;
        }

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
        public bool TryGetChar(int i, out char result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertChar(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(char), i);

            result = default;
            return false;
        }

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
        public bool TryGetGuid(int i, out Guid result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertGuid(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(Guid), i);

            result = default;
            return false;
        }

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
        public bool TryGetInt16(int i, out short result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertInt16(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(short), i);

            result = default;
            return false;
        }

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
        public bool TryGetInt32(int i, out int result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertInt32(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(int), i);

            result = default;
            return false;
        }

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
        public bool TryGetInt64(int i, out Int64 result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertInt64(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(long), i);

            result = default;
            return false;
        }

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
        public bool TryGetFloat(int i, out float result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertFloat(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(float), i);

            result = default;
            return false;
        }

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
        public bool TryGetDouble(int i, out double result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertDouble(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(double), i);

            result = default;
            return false;
        }

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
        public bool TryGetDecimal(int i, out decimal result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertDecimal(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(decimal), i);

            result = default;
            return false;
        }

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
        public bool TryGetDateTime(int i, out DateTime result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);

            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (_table.DateTimeFormats.IsNull() || _table.DateTimeFormats.Count < 1)
            {
                if (value.TryConvertDateTime(out result)) return true;
            }
            else
            {
                if (value.TryConvertDateTime(_table.DateTimeFormats.ToArray(), out result)) return true;
            }
            if (!ignoreFormatException) throw new ConversionException(value, typeof(DateTime), i);

            result = default;
            return false;
        }
                      
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
        public bool TryGetByteArray(int i, out byte[] result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = null;
                return false;
            }

            if (value.TryConvertByteArray(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(byte[]), i);

            result = null;
            return false;
        }

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
        public bool TryGetEnum<T>(int i, out T result, bool ignoreFormatException = false) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(string.Format(
                Properties.Resources.TypeConverters_ValueIsNotEnumeration, typeof(T).FullName));

            var value = GetValueForCast(i, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertEnum(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(T), i);

            result = default;
            return false;
        }

        #endregion

        #region Try Get By Column Name

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
        public bool TryGetBoolean(string columnName, out bool result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertBool(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(bool), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetByte(string columnName, out byte result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertByte(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(byte), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetSByte(string columnName, out sbyte result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertSByte(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(sbyte), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetChar(string columnName, out char result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertChar(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(char), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetGuid(string columnName, out Guid result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertGuid(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(Guid), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetInt16(string columnName, out short result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertInt16(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(short), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetInt32(string columnName, out int result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertInt32(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(int), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetInt64(string columnName, out Int64 result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertInt64(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(long), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetFloat(string columnName, out float result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertFloat(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(float), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetDouble(string columnName, out double result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertDouble(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(double), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetDecimal(string columnName, out decimal result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertDecimal(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(decimal), columnName);

            result = default;
            return false;
        }

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
        public bool TryGetDateTime(string columnName, out DateTime result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);

            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (_table.DateTimeFormats.IsNull() || _table.DateTimeFormats.Count < 1)
            {
                if (value.TryConvertDateTime(out result)) return true;
            }
            else
            {
                if (value.TryConvertDateTime(_table.DateTimeFormats.ToArray(), out result)) return true;
            }
            
            if (!ignoreFormatException) throw new ConversionException(value, typeof(DateTime), columnName);

            result = default;
            return false;
        }
                                 
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
        public bool TryGetByteArray(string columnName, out byte[] result, bool ignoreFormatException = false)
        {
            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = null;
                return false;
            }

            if (value.TryConvertByteArray(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(byte[]), columnName);

            result = null;
            return false;
        }

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
        public bool TryGetEnum<T>(string columnName, out T result, bool ignoreFormatException = false) where T : struct 
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(string.Format(
                Properties.Resources.TypeConverters_ValueIsNotEnumeration, typeof(T).FullName));

            var value = GetValueForCast(columnName, false);
            if (value.IsNull())
            {
                result = default;
                return false;
            }

            if (value.TryConvertEnum(out result)) return true;

            if (!ignoreFormatException) throw new ConversionException(value, typeof(T), columnName);

            result = default;
            return false;
        }

        #endregion

        #region Get Or Default By Index

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
        public bool GetBooleanOrDefault(int i, bool defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetBoolean(i, out bool value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public byte GetByteOrDefault(int i, byte defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetByte(i, out byte value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public sbyte GetSByteOrDefault(int i, sbyte defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetSByte(i, out sbyte value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public char GetCharOrDefault(int i, char defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetChar(i, out char value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public Guid GetGuidOrDefault(int i, Guid defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetGuid(i, out Guid value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public short GetInt16OrDefault(int i, short defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetInt16(i, out short value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public int GetInt32OrDefault(int i, int defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetInt32(i, out int value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public Int64 GetInt64OrDefault(int i, Int64 defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetInt64(i, out long value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public float GetFloatOrDefault(int i, float defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetFloat(i, out float value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public double GetDoubleOrDefault(int i, double defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetDouble(i, out double value, ignoreFormatException)) return value;
            return defaultValue;
        }

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// If the conversion fails, returns defaultValue.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <param name="defaultValue">The value to return if the conversion fails.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:GetString"/> method.</remarks>
        public string GetStringOrDefault(int i, string defaultValue)
        {
            var result = GetString(i);
            return result.IsNullOrWhiteSpace() ? defaultValue : result;
        }

        /// <summary>
        /// Gets the data stored in the column specified as string.
        /// If the conversion fails, returns string.Empty.
        /// </summary>
        /// <param name="i">The zero-based index of the column.</param>
        /// <exception cref="IndexOutOfRangeException">The column index is out of range.</exception>
        /// <remarks>Uses <see cref="o:GetString"/> method.</remarks>
        public string GetStringOrDefault(int i)
        {
            var result = GetString(i);
            return result ?? string.Empty;
        }

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
        public decimal GetDecimalOrDefault(int i, decimal defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetDecimal(i, out decimal value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public DateTime GetDateTimeOrDefault(int i, DateTime defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetDateTime(i, out DateTime value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public byte[] GetByteArrayOrDefault(int i, byte[] defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetByteArray(i, out byte[] value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public T GetEnumOrDefault<T>(int i, T defaultValue, bool ignoreFormatException = false) where T : struct
        {
            if (TryGetEnum(i, out T value, ignoreFormatException)) return value;
            return defaultValue;
        }

        #endregion

        #region Get Or Default By Column Name

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
        public bool GetBooleanOrDefault(string columnName, bool defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetBoolean(columnName, out bool value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public byte GetByteOrDefault(string columnName, byte defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetByte(columnName, out byte value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public sbyte GetSByteOrDefault(string columnName, sbyte defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetSByte(columnName, out sbyte value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public char GetCharOrDefault(string columnName, char defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetChar(columnName, out char value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public Guid GetGuidOrDefault(string columnName, Guid defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetGuid(columnName, out Guid value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public short GetInt16OrDefault(string columnName, short defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetInt16(columnName, out short value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public int GetInt32OrDefault(string columnName, int defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetInt32(columnName, out int value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public Int64 GetInt64OrDefault(string columnName, Int64 defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetInt64(columnName, out long value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public float GetFloatOrDefault(string columnName, float defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetFloat(columnName, out float value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public double GetDoubleOrDefault(string columnName, double defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetDouble(columnName, out double value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public string GetStringOrDefault(string columnName, string defaultValue)
        {
            var result = GetString(columnName);
            return result.IsNullOrWhiteSpace() ? defaultValue : result;
        }

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
        public string GetStringOrDefault(string columnName)
        {
            var result = GetString(columnName);
            return result ?? string.Empty;
        }

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
        public decimal GetDecimalOrDefault(string columnName, decimal defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetDecimal(columnName, out decimal value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public DateTime GetDateTimeOrDefault(string columnName, DateTime defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetDateTime(columnName, out DateTime value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public byte[] GetByteArrayOrDefault(string columnName, byte[] defaultValue, bool ignoreFormatException = false)
        {
            if (TryGetByteArray(columnName, out byte[] value, ignoreFormatException)) return value;
            return defaultValue;
        }

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
        public T GetEnumOrDefault<T>(string columnName, T defaultValue, bool ignoreFormatException = false) where T : struct
        {
            if (TryGetEnum(columnName, out T value, ignoreFormatException)) return value;
            return defaultValue;
        }

        #endregion

        private object GetValueForCast(int i, bool throwOnNull)
        {
            EnsureIndexIsValid(i);
            if (throwOnNull && _data[i].IsNull()) throw new ArgumentNullException(
                string.Format(Properties.Resources.LightDataRow_CannotCastTypeOnNull,
                    _table.Columns[i].ColumnName, i));
            return _data[i];
        }

        private object GetValueForCast(string columnName, bool throwOnNull)
        {
            var index = GetOrdinal(columnName);

            if (index < 0) throw new Exception(
                string.Format(Properties.Resources.LightDataRow_ColumnDoesNotExist, columnName));
            if (throwOnNull && _data[index].IsNull()) throw new ArgumentNullException(
                string.Format(Properties.Resources.LightDataRow_CannotCastTypeOnNull,
                    columnName, index));
            return _data[index];
        }

        #endregion

        private void SetValue(int columnIndex, object value)
        {
            if (value.IsNull())
            {
                _data[columnIndex] = null;
                return;
            }
            if (_table.Columns[columnIndex].DataType.IsAssignableFrom(value.GetType()))
            {
                _data[columnIndex] = CloneValue(value);
                return;
            }

            throw new ArgumentException(string.Format(Properties.Resources.LightDataRow_DataTypeMismatchException,
                value.GetType().FullName, _table.Columns[columnIndex].DataType.FullName));
        }

        private void EnsureIndexIsValid(int i)
        {
            if (i < 0 || (i + 1) > _data.Length)
                throw new IndexOutOfRangeException(string.Format(Properties.Resources.IndexValueOutOfRange, i.ToString()));
        }

        private static Object CloneValue(Object value)
        {
            if (value.IsNull()) return null;

            if (value.GetType().IsValueType) return value;

            if (value is string strVal) return new string(strVal.ToCharArray());

            if (value is Byte[] arrayVal) return arrayVal.Clone();

            try
            {
                return value.GetType().InvokeMember("Clone", BindingFlags.Instance
                    | BindingFlags.Public, null, value, null);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(string.Format(Properties.Resources.LightDataRow_ReferenceTypeWithoutCloneMethod,
                    value.GetType().FullName), ex);
            }
        }

        internal LightDataRowProxy GetLightDataRowProxy()
        {
            var values = new Object[_data.Length];
            for (int i = 0; i < _data.Length; i++)
            {
                values[i] = _data[i];
            }

            var result = new LightDataRowProxy { Values = values };

            return result;
        }

    }
}
