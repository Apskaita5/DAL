using A5Soft.DAL.Core.MicroOrm.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace A5Soft.DAL.Core.MicroOrm
{
    public sealed class IdentityMapChildGuid<T> : OrmIdentityMapBase<T> where T : class
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityMapChildGuid{T}"/> class with specified parameters.
        /// </summary>
        /// <param name="tableName">The name of the database table.</param>
        /// <param name="primaryKeyFieldName">The name of the primary key field.</param>
        /// <param name="primaryKeyPropName">The name of the primary key property.</param>
        /// <param name="parentIdFieldName">The name of the parent ID field in the database.</param>
        /// <param name="factoryMethod">A factory method to create instances of type T.</param>
        /// <param name="primaryKeyGetter">A function to get the primary key of type T.</param>
        /// <param name="primaryKeySetter">An action to set the primary key of type T.</param>
        /// <param name="fetchQueryToken">An optional query token for fetching data.</param>
        /// <param name="fetchByParentIdQueryToken">An optional query token for fetching data by parent ID.</param>
        /// <param name="fetchAllQueryToken">An optional query token for fetching all data.</param>
        /// <param name="initQueryToken">An optional initialization query token.</param>
        /// <param name="scopeIsFlag">Indicates whether the scope is a flag.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parentIdFieldName"/>,
        /// <paramref name="primaryKeyGetter"/>, or <paramref name="primaryKeySetter"/> are null.</exception>
        /// <returns>
        /// A new instance of <see cref="IdentityMapChildGuid{T}"/>.
        /// </returns>
        public IdentityMapChildGuid(string tableName, string primaryKeyFieldName,
                    string primaryKeyPropName, string parentIdFieldName, Func<T> factoryMethod, Func<T, Guid?> primaryKeyGetter,
                    Action<T, Guid?> primaryKeySetter, string fetchQueryToken = null, string fetchByParentIdQueryToken = null,
                    string fetchAllQueryToken = null, string initQueryToken = null, bool scopeIsFlag = false)
                    : base(tableName, parentIdFieldName, primaryKeyFieldName, primaryKeyPropName,
                        false, fetchQueryToken, fetchByParentIdQueryToken, fetchAllQueryToken,
                        initQueryToken, null, scopeIsFlag, factoryMethod)
        {
            if (parentIdFieldName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(parentIdFieldName));
            PrimaryKeyGetter = primaryKeyGetter ?? throw new ArgumentNullException(nameof(primaryKeyGetter));
            PrimaryKeySetter = primaryKeySetter ?? throw new ArgumentNullException(nameof(primaryKeySetter));
            PrimaryKeyUpdatable = false;
        }


        /// <summary>
        /// Gets a primary key value.
        /// </summary>
        public Func<T, Guid?> PrimaryKeyGetter { get; }

        /// <summary>
        /// Sets a primary key value.
        /// </summary>
        public Action<T, Guid?> PrimaryKeySetter { get; }

        public override bool PrimaryKeyUpdatable { get; }


        internal override SqlParam GetPrimaryKeyParamForInsert(T instance)
        {
            if (!PrimaryKeyGetter(instance).HasValue) PrimaryKeySetter(instance, Guid.NewGuid());
            var value = PrimaryKeyGetter(instance).Value;
            return SqlParam.Create(PrimaryKeyFieldName, value);
        }

        internal override SqlParam GetPrimaryKeyParamForUpdateSet(T instance)
        {
            throw new NotSupportedException();
        }

        internal override SqlParam GetPrimaryKeyParamForUpdateWhere(T instance, string paramName)
        {
            var value = PrimaryKeyGetter(instance);
            if (!value.HasValue) throw new InvalidOperationException(
                $"Entity {typeof(T).FullName} doesn't have a primary key, i.e. its a new entity.");
            if (value.Value == Guid.Empty) throw new InvalidOperationException(
                $"Guid primary key cannot be empty (all zeros) (entity {typeof(T).FullName}).");

            return SqlParam.Create(PrimaryKeyFieldName, value.Value);
        }

        internal override void SetPrimaryKeyAutoIncrementValue(T instance, long nid)
        {
            throw new NotSupportedException();
        }

        internal override void LoadPrimaryKeyValue(T instance, LightDataRow row)
        {
            var value = row.GetGuid(PrimaryKeyPropName);
            PrimaryKeySetter(instance, value);
        }

        internal override void LoadPrimaryKeyValue(T instance, ILightDataReader reader)
        {
            var value = reader.GetGuid(PrimaryKeyPropName);
            PrimaryKeySetter(instance, value);
        }

        internal override void UpdatePrimaryKey(T instance)
        {
            // do nothing, guid id is not entered by a user 
        }

        internal override void DeletePrimaryKey(T instance)
        {
            PrimaryKeySetter(instance, null);
        }

        internal override object GetPrimaryKey(T instance)
        {
            return PrimaryKeyGetter(instance);
        }

    }
}
