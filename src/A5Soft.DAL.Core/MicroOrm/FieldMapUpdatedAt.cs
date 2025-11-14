using A5Soft.DAL.Core.MicroOrm.Core;
using System;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// describes how a business object audit property (field) for last update timestamp is persisted in a database;
    /// should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="T">a type of the business object that the field belongs to</typeparam>
    public sealed class FieldMapUpdatedAt<T> : ManagedOrmFieldMapBase<T, DateTime> where T : class
    {
        /// <summary>
        /// create a new description how a business object audit property (field) for last update timestamp is persisted in a database;
        /// </summary>
        /// <param name="dbFieldName">a name of the database table field that the property value is persisted in</param>
        /// <param name="propName">a name of the the property that the value is managed by; match LightDataColumn name 
        /// that is returned on business object field</param>
        /// /// <param name="valueGetter">a function to get a current value of the field</param>
        /// <param name="valueSetter">a function to set a current value of the field; the value is stored in the column
        /// with a name specified in the PropName property</param>
        public FieldMapUpdatedAt(string dbFieldName, string propName, Action<T, DateTime> valueSetter,
            Func<T, DateTime> valueGetter)
            : base(dbFieldName, propName, FieldPersistenceType.Read | FieldPersistenceType.Insert
                | FieldPersistenceType.Update, valueSetter, valueGetter, null) { }

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

        internal void InitValue(T instance)
        {
            ValueSetter(instance, Extensions.GetCurrentTimeStamp());
        }

        internal void InitValue(T instance, DateTime insertTimeStamp)
        {
            ValueSetter(instance, insertTimeStamp);
        }
    }
}
