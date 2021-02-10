using System;
using A5Soft.DAL.Core.MicroOrm.Core;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// describes how a business object string property (field) is persisted in a database;
    /// should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="T">a type of the business object that the field belongs to</typeparam>
    public sealed class FieldMapString<T> : ManagedOrmFieldMapBase<T, string> where T : class
    {
        private readonly string _defaultValue;
        private readonly bool _trimValue;


        /// <summary>
        /// for editable field
        /// </summary>
        /// <param name="dbFieldName">a name of the database table field that the property value is persisted in</param>
        /// <param name="propName">a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field</param>
        /// /// <param name="valueGetter">a function to get a current value of the field</param>
        /// <param name="valueSetter">a function to set a current value of the field; the value is stored in the column
        /// with a name specified in the PropName property</param>
        /// <param name="persistenceType">a value indicating how a field value is persisted in the database</param>
        /// <param name="updateScope">an update scope that updates the property value in database.
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.</param>
        /// <param name="defaultValue">a default value to use if the underlying value is null</param>
        /// <param name="trimValue">a value indicating whether to apply Trim() to the underlying value</param>
        public FieldMapString(string dbFieldName, string propName, Action<T, string> valueSetter,
            Func<T, string> valueGetter, FieldPersistenceType persistenceType, int? updateScope = null,
            string defaultValue = "", bool trimValue = true)
            : base(dbFieldName, propName, persistenceType, valueSetter, valueGetter, updateScope)
        {
            _defaultValue = defaultValue;
            _trimValue = trimValue;
        }

        /// <summary>
        /// for readonly field
        /// </summary>
        /// <param name="dbFieldName">a name of the database table field that the property value is persisted in</param>
        /// <param name="propName">a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field</param>
        /// <param name="valueSetter">a function to set a current (loaded from DB) value of the field</param>
        /// <param name="isInitializable">a value indicating whether the property value should be set (initialized) 
        /// from the init query result</param>
        /// <param name="defaultValue">a default value to use if the underlying value is null</param>
        /// <param name="trimValue">a value indicating whether to apply Trim() to the underlying value</param>
        public FieldMapString(string dbFieldName, string propName, Action<T, string> valueSetter,
            bool isInitializable = false, string defaultValue = "", bool trimValue = true)
            : base(dbFieldName, propName,
                isInitializable ? FieldPersistenceType.Read | FieldPersistenceType.Init : FieldPersistenceType.Read,
                valueSetter, null, null)
        {
            _defaultValue = defaultValue;
            _trimValue = trimValue;
        }

        /// <summary>
        /// for readonly aggregate field (that does not have a column in the parent table)
        /// </summary>
        /// <param name="propName">a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field</param>
        /// <param name="valueSetter">a function to set a current (loaded from DB) value of the field</param>
        /// <param name="isInitializable">a value indicating whether the property value should be set (initialized) 
        /// from the init query result</param>
        /// <param name="defaultValue">a default value to use if the underlying value is null</param>
        /// <param name="trimValue">a value indicating whether to apply Trim() to the underlying value</param>
        public FieldMapString(string propName, Action<T, string> valueSetter, bool isInitializable = false,
            string defaultValue = "", bool trimValue = true)
            : base(string.Empty, propName,
                isInitializable ? FieldPersistenceType.Read | FieldPersistenceType.Init : FieldPersistenceType.Read,
                valueSetter, null, null)
        {
            _defaultValue = defaultValue;
            _trimValue = trimValue;
        }


        internal override SqlParam GetParam(T instance)
        {
            return SqlParam.Create(DbFieldName, _trimValue ? (ValueGetter(instance)?.Trim() ?? _defaultValue)
                : (ValueGetter(instance) ?? _defaultValue));
        }

        internal override void SetValue(T instance, LightDataRow row)
        {
            ValueSetter(instance, _trimValue ? (row.GetString(PropName)?.Trim() ?? _defaultValue)
                : (row.GetString(PropName) ?? _defaultValue));
        }

        internal override void SetValue(T instance, ILightDataReader reader)
        {
            ValueSetter(instance, _trimValue ? (reader.GetString(PropName)?.Trim() ?? _defaultValue)
                : (reader.GetString(PropName) ?? _defaultValue));
        }

    }
}
