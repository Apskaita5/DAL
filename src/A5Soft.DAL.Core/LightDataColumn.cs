﻿using System;
using System.Linq;
using A5Soft.DAL.Core.Serialization;

namespace A5Soft.DAL.Core
{
    /// <summary>
    /// Represents the schema of a column in a LightDataTable.
    /// </summary>
    [Serializable]
    public sealed class LightDataColumn
    {

        public const string DefaultColumnName = "ColumnName";

        public readonly Type[] SupportedTypes = new Type[] { typeof(short),typeof(int),typeof(long),
            typeof(ushort), typeof(uint), typeof(ulong), typeof(byte), typeof(sbyte), typeof(string),
            typeof(bool), typeof(char), typeof(DateTime), typeof(decimal), typeof(double),
            typeof(float), typeof(Guid), typeof(TimeSpan), typeof(byte[])};

        #region Private Fields

        private string _caption = string.Empty;
        private string _columnName = string.Empty;
        private readonly Type _dataType;
        private string _nativeDataType = string.Empty;
        private bool _readOnly;
        private LightDataTable _table;

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets or sets the caption for the column. Could be used to display table header.
        /// </summary>
        /// <value>The caption of the column. If not set, returns the <see cref="ColumnName">ColumnName</see> value.</value>
        /// <remarks>You can use the Caption property to display a descriptive or friendly name for a DataColumn.</remarks>
        public string Caption
        {
            get
            {
                return _caption.IsNullOrWhiteSpace() ? this.ColumnName : _caption;
            }
            set
            {
                _caption = value?.Trim() ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        /// <remarks>Column names within a table should be unique.
        /// Column name cannot be null or an empty string.</remarks>
        public string ColumnName
        {
            get
            {
                return _columnName?.Trim() ?? string.Empty;
            }
            set
            {
                if (value.IsNullOrWhiteSpace())
                    throw new ArgumentNullException(nameof(value));

                if (null != _table && _table.Columns.Any(col => !Object.ReferenceEquals(this, col)
                        && value.Trim().ToUpperInvariant() == col.ColumnName.Trim().ToUpperInvariant()))
                {
                    throw new ArgumentException(Properties.Resources.LightDataColumn_NameNotUnique);
                }

                _columnName = value.Trim();
            }
        }

        /// <summary>
        /// Gets the type of data stored in the column.
        /// </summary>
        /// <remarks></remarks>
        public Type DataType
        {
            get
            {
                return _dataType;
            }
        }

        /// <summary>
        /// Gets or sets the native data type of the column.
        /// </summary>
        /// <remarks>Used to store native SQL data type information.</remarks>
        public string NativeDataType
        {
            get { return _nativeDataType?.Trim() ?? string.Empty; }
            set { _nativeDataType = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets the (zero-based) position of the column in the LightDataColumnCollection collection.
        /// </summary>
        /// <value>The position of the column. Gets -1 if the column is not a member of a collection.</value>
        public Int32 Ordinal
        {
            get
            {
                if (_table.IsNull()) return -1;
                return _table.Columns.IndexOf(this);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the column allows for changes 
        /// as soon as a row has been added to the table.
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return _readOnly;
            }
            set
            {
                _readOnly = value;
            }
        }

        /// <summary>
        /// Gets the LightDataTable to which the column belongs to.
        /// </summary>
        public LightDataTable Table
        {
            get
            {
                return _table;
            }
            internal set
            {
                _table = value;
            }
        }

        #endregion
         
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the LightDataColumn class using the specified column name and data type.
        /// </summary>
        /// <param name="columnName">A string that represents the name of the column to be created. 
        /// If set to null or an empty string (""), a default name will be specified when added 
        /// to the columns collection.</param>
        /// <param name="dataType">A supported DataType of the column to be created.</param>
        public LightDataColumn(string columnName, Type dataType)
        {
            _dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            _columnName = columnName.IsNullOrWhiteSpace() ? DefaultColumnName : columnName;
            _caption = columnName;
        }

        /// <summary>
        /// Initializes a new instance of the LightDataColumn class using the specified column name.
        /// </summary>
        /// <param name="columnName">A string that represents the name of the column to be created. 
        /// If set to null or an empty string (""), a default name will be specified when added 
        /// to the columns collection.</param>
        public LightDataColumn(string columnName) :
            this(columnName, typeof(object))
        { }

        internal LightDataColumn(LightDataColumnProxy proxy)
        {
            _caption = proxy.Caption;
            _columnName = proxy.ColumnName;
            _dataType = Type.GetType(proxy.DataType);
            _nativeDataType = proxy.NativeDataType;
            _readOnly = proxy.ReadOnly;
        } 
        
        #endregion


        internal LightDataColumnProxy GetLightDataColumnProxy()
        {
            return new LightDataColumnProxy
            {
                Caption = _caption,
                ColumnName = _columnName,
                DataType = _dataType.AssemblyQualifiedName,
                NativeDataType = _nativeDataType,
                ReadOnly = _readOnly
            };
        }

    }
}
