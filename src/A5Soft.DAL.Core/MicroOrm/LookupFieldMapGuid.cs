using A5Soft.DAL.Core.MicroOrm.Core;
using System;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// A field map for a lookup with a Guid primary key.
    /// </summary>
    /// <typeparam name="TEntity">type of the entity that the field belongs to</typeparam>
    /// <typeparam name="TLookup">type of the lookup for the field</typeparam>
    public class LookupFieldMapGuid<TEntity, TLookup> : OrmLookupFieldMapBase<TEntity>
        where TEntity : class
        where TLookup : class
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
        public LookupFieldMapGuid(string dbFieldName, string propName, Action<TEntity, TLookup> valueSetter,
            Func<TEntity, Guid?> valueGetter, FieldPersistenceType persistenceType, int? updateScope = null)
            : base(dbFieldName, propName, persistenceType, updateScope)
        {
            ValueGetter = valueGetter ?? throw new ArgumentNullException(nameof(valueGetter));
            ValueSetter = valueSetter ?? throw new ArgumentNullException(nameof(valueSetter));
        }

        /// <summary>
        /// for readonly field
        /// </summary>
        /// <param name="dbFieldName">a name of the database table field that the property value is persisted in</param>
        /// <param name="propName">a name of the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field</param>
        /// <param name="valueSetter">a function to set a current (loaded from DB) value of the field</param>
        /// <param name="isInitializable">a value indicating whether the property value should be set (initialized) 
        /// from the init query result</param>
        public LookupFieldMapGuid(string dbFieldName, string propName, Action<TEntity, TLookup> valueSetter,
            bool isInitializable = false)
            : base(dbFieldName, propName,
                isInitializable ? FieldPersistenceType.Read | FieldPersistenceType.Init : FieldPersistenceType.Read,
                null)
        {
            ValueGetter = null;
            ValueSetter = valueSetter ?? throw new ArgumentNullException(nameof(valueSetter));
        }

        /// <summary>
        /// for readonly aggregate field (that does not have a column in the parent table)
        /// </summary>
        /// <param name="propName">a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field</param>
        /// <param name="valueSetter">a function to set a current (loaded from DB) value of the field</param>
        /// <param name="isInitializable">a value indicating whether the property value should be set (initialized) 
        /// from the init query result</param>
        public LookupFieldMapGuid(string propName, Action<TEntity, TLookup> valueSetter, bool isInitializable = false)
            : base(string.Empty, propName,
                isInitializable ? FieldPersistenceType.Read | FieldPersistenceType.Init : FieldPersistenceType.Read,
                null)
        {
            ValueSetter = valueSetter ?? throw new ArgumentNullException(nameof(valueSetter));
        }


        /// <summary>
        /// Gets a method to get a value of the mapped field for a class instance.
        /// </summary>
        private Func<TEntity, Guid?> ValueGetter { get; }

        /// <summary>
        /// Gets a method to set a value of the mapped field for a class instance.
        /// </summary>
        private Action<TEntity, TLookup> ValueSetter { get; }


        internal override SqlParam GetParam(TEntity instance)
        {
            return SqlParam.Create(DbFieldName, ValueGetter(instance));
        }

        internal override async Task SetValueAsync(TEntity instance, LightDataRow row, OrmServiceLookupResolver resolver)
        {
            ValueSetter(instance, await resolver.FetchLookupAsync<TLookup>(row.GetGuidNullable(PropName)));
        }

        internal override async Task SetValueAsync(TEntity instance, ILightDataReader reader, OrmServiceLookupResolver resolver)
        {
            ValueSetter(instance, await resolver.FetchLookupAsync<TLookup>(reader.GetGuidNullable(PropName)));
        }
    }
}
