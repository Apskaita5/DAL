using System;
using A5Soft.DAL.Core.MicroOrm.Core;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// describes how a business object DateTime? property (field) is persisted in a database;
    /// should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="T">a type of the business object that the field belongs to</typeparam>
    public sealed class FieldMapNullableDateTime<T> : ManagedOrmFieldMapBase<T, DateTime?> where T : class
    {

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
        public FieldMapNullableDateTime(string dbFieldName, string propName, Action<T, DateTime?> valueSetter,
            Func<T, DateTime?> valueGetter, FieldPersistenceType persistenceType, int? updateScope = null)
            : base(dbFieldName, propName, persistenceType, valueSetter, valueGetter, updateScope) { }

        /// <summary>
        /// for readonly field
        /// </summary>
        /// <param name="dbFieldName">a name of the database table field that the property value is persisted in</param>
        /// <param name="propName">a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field</param>
        /// <param name="valueSetter">a function to set a current (loaded from DB) value of the field</param>
        /// <param name="isInitializable">a value indicating whether the property value should be set (initialized) 
        /// from the init query result</param>
        public FieldMapNullableDateTime(string dbFieldName, string propName, Action<T, DateTime?> valueSetter,
            bool isInitializable = false)
            : base(dbFieldName, propName,
                isInitializable ? FieldPersistenceType.Read | FieldPersistenceType.Init : FieldPersistenceType.Read,
                valueSetter, null, null) { }

        /// <summary>
        /// for readonly aggregate field (that does not have a column in the parent table)
        /// </summary>
        /// <param name="propName">a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field</param>
        /// <param name="valueSetter">a function to set a current (loaded from DB) value of the field</param>
        /// <param name="isInitializable">a value indicating whether the property value should be set (initialized) 
        /// from the init query result</param>
        public FieldMapNullableDateTime(string propName, Action<T, DateTime?> valueSetter, bool isInitializable = false)
            : base(string.Empty, propName,
                isInitializable ? FieldPersistenceType.Read | FieldPersistenceType.Init : FieldPersistenceType.Read,
                valueSetter, null, null) { }


        internal override void SetValue(T instance, LightDataRow row)
        {
            ValueSetter(instance, row.GetDateTimeNullable(PropName));
        }

        internal override void SetValue(T instance, ILightDataReader reader)
        {
            ValueSetter(instance, reader.GetDateTimeNullable(PropName));
        }

    }
}
