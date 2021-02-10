using System;

namespace A5Soft.DAL.Core.MicroOrm.Core
{
    /// <summary>
    /// a base class for description of how a business object property (field) is persisted in a database;
    /// concrete implementations per field type should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="T">a type of the business object that the field belongs to</typeparam>
    public abstract class OrmFieldMapBase<T> where T : class
    {

        /// <summary>
        /// create a new description how a business object property (field) is persisted in a database;
        /// </summary>
        /// <param name="dbFieldName">a name of the database table field that the property value is persisted in
        /// (required if persistence type includes insert or update;
        /// however, ORM will not be able to generate a select query, if the database field name is not specified)</param>
        /// <param name="propName">a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field (required)</param>
        /// <param name="persistenceType">a value indicating how a field value is persisted in the database</param>
        /// <param name="valueSetter">a method to set a value of the mapped field for a class instance
        /// (required for selects and init)</param>
        /// <param name="valueGetter">a method to get a value of the mapped field for a class instance
        /// (required for inserts and updates)</param>
        /// <param name="updateScope">an update scope that updates the property value in database.
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.</param>
        protected OrmFieldMapBase(string dbFieldName, string propName, FieldPersistenceType persistenceType,
            int? updateScope)
        {
            if (persistenceType.HasFlag(FieldPersistenceType.Insert | FieldPersistenceType.Update) 
                && dbFieldName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(dbFieldName));
            
            if (propName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(propName));

            DbFieldName = dbFieldName?.Trim() ?? string.Empty;
            PropName = propName.Trim();
            PersistenceType = persistenceType;
            UpdateScope = updateScope;
        }

        #region Properties

        /// <summary>
        /// a name of the database table field that the property value is persisted in
        /// </summary>
        /// <remarks>Only required (relevant) for insertable or updateable fields</remarks>
        public string DbFieldName { get; }

        /// <summary>
        /// a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object fetch
        /// </summary>
        public string PropName { get; }

        /// <summary>
        /// a value indicating how a field value is persisted in the database
        /// </summary>
        public FieldPersistenceType PersistenceType { get; }

        /// <summary>
        /// an update scope that updates the property value in database
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.
        /// If multiple scope combinations are used, the enum should be defined as [Flags].
        /// in that case: (a) a field should only be assigned to a single scope;
        /// (b) a bitwise check is used: (fieldScope & requestedScope) != 0;
        /// (c) <see cref="OrmIdentityMap{T}.ScopeIsFlag"/> should be set to true.
        /// </summary>
        public int? UpdateScope { get; }
                      
        #endregion

        #region Internal Mapping Methods

        /// <summary>
        /// Gets an <see cref="SqlParam">SQL query parameter</see> where DB field name is used as a parameter name
        /// </summary>
        /// <param name="instance">an instance of business object to get a value parameter for</param>
        internal abstract SqlParam GetParam(T instance);

        /// <summary>
        /// Sets an instance field value from the query result.
        /// </summary>
        /// <param name="instance">an instance of business object to set the field value for</param>
        /// <param name="row">row that contains query data</param>
        internal abstract void SetValue(T instance, LightDataRow row);

        /// <summary>
        /// Sets an instance field value from the query result.
        /// </summary>
        /// <param name="instance">an instance of business object to set the field value for</param>
        /// <param name="reader">query result set reader</param>
        internal abstract void SetValue(T instance, ILightDataReader reader);

        /// <summary>
        /// Gets a database field name and an associated property name.
        /// Use it to format select query in db_field_name AS PropertyName style.
        /// </summary>
        internal (string DbFieldName, string PropName) GetSelectField()
        {
            if (DbFieldName.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                $"Database field name is not set for the property {PropName} in class {typeof(T).FullName}.");
            return (DbFieldName, PropName);
        }

        /// <summary>
        /// Gets a value indicating whether the field shall be updated within the scopes given.
        /// </summary>
        /// <param name="scopes">scopes that shall be updated; null scopes => update all updateable fields</param>
        /// <param name="scopeIsFlag">whether the scope is flag enum</param>
        internal bool IsInUpdateScope(int? scopes, bool scopeIsFlag)
        {
            if (!PersistenceType.HasFlag(FieldPersistenceType.Update)) return false;
            return !scopes.HasValue || null == UpdateScope || (scopeIsFlag && ((scopes.Value & UpdateScope) != 0))
                || (!scopeIsFlag && scopes.Value == UpdateScope);
        }

        #endregion

    }
}
