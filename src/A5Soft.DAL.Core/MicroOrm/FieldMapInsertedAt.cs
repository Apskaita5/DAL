using System;
using A5Soft.DAL.Core.MicroOrm.Core;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// describes how a business object audit property (field) for creation timestamp is persisted in a database;
    /// should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="T">a type of the business object that the field belongs to</typeparam>
    public sealed class FieldMapInsertedAt<T> : ManagedOrmFieldMapBase<T, DateTime> where T : class
    {

        /// <summary>
        /// create a new description how a business object audit property (field) for creation timestamp is persisted in a database;
        /// </summary>
        /// <param name="dbFieldName">a name of the database table field that the property value is persisted in</param>
        /// <param name="propName">a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field</param>
        /// /// <param name="valueGetter">a function to get a current value of the field</param>
        /// <param name="valueSetter">a function to set a current value of the field; the value is stored in the column
        /// with a name specified in the PropName property</param>
        public FieldMapInsertedAt(string dbFieldName, string propName, Action<T, DateTime> valueSetter, 
            Func<T, DateTime> valueGetter)
            : base(dbFieldName, propName, FieldPersistenceType.Insert | FieldPersistenceType.Read,
                valueSetter, valueGetter, null) { }



        internal override SqlParam GetParam(T instance)
        {
            return SqlParam.Create(DbFieldName, ValueGetter(instance).ToUniversalTime());
        }

        internal override void SetValue(T instance, LightDataRow row)
        {
            ValueSetter(instance, DateTime.SpecifyKind(row.GetDateTime(PropName), DateTimeKind.Utc));
        }

        internal override void SetValue(T instance, ILightDataReader reader)
        {
            ValueSetter(instance, DateTime.SpecifyKind(reader.GetDateTime(PropName), DateTimeKind.Utc));
        }

        internal void InitValue(T instance, FieldMapUpdatedAt<T> updatedAtFieldMap)
        {
            if (updatedAtFieldMap.IsNull()) throw new ArgumentNullException(nameof(updatedAtFieldMap));

            var value = Extensions.GetCurrentTimeStamp();
            ValueSetter(instance, value);
            updatedAtFieldMap.InitValue(instance, value);
        }

    }
}
