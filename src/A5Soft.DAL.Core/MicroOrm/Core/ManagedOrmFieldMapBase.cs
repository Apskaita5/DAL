using System;

namespace A5Soft.DAL.Core.MicroOrm.Core
{
    /// <summary>
    /// a base class for description of how a business object property (field) is persisted in a database;
    /// concrete implementations per field type should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="T">a type of the business object that the field belongs to</typeparam>
    /// <typeparam name="C">a type of the underlying field value</typeparam>
    public abstract class ManagedOrmFieldMapBase<T, C> : OrmFieldMapBase<T> where T : class
    {
        protected ManagedOrmFieldMapBase(string dbFieldName, string propName,
            FieldPersistenceType persistenceType, Action<T, C> valueSetter,
            Func<T, C> valueGetter, int? updateScope)
            : base(dbFieldName, propName, persistenceType, updateScope)
        {
            if (persistenceType.HasFlag(FieldPersistenceType.Insert | FieldPersistenceType.Update)
                && null == valueGetter) throw new ArgumentNullException(nameof(valueGetter));
            if (persistenceType.HasFlag(FieldPersistenceType.Read | FieldPersistenceType.Init)
                && null == valueSetter) throw new ArgumentNullException(nameof(valueSetter));

            ValueGetter = valueGetter;
            ValueSetter = valueSetter;
        }


        /// <summary>
        /// Gets a method to get a value of the mapped field for a class instance.
        /// </summary>
        protected Func<T, C> ValueGetter { get; }

        /// <summary>
        /// Gets a method to set a value of the mapped field for a class instance.
        /// </summary>
        protected Action<T, C> ValueSetter { get; }

              
        internal override SqlParam GetParam(T instance)
        {
            return SqlParam.Create(DbFieldName, ValueGetter(instance));
        }
    }
}
