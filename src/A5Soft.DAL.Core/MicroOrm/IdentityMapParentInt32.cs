using A5Soft.DAL.Core.MicroOrm.Core;
using System;

namespace A5Soft.DAL.Core.MicroOrm
{
    public sealed class IdentityMapParentInt32<T> : OrmIdentityMapBase<T> where T : class
    {

        public IdentityMapParentInt32(string tableName, string primaryKeyFieldName,
            string primaryKeyPropName, Func<T> factoryMethod, Func<T, int?> primaryKeyGetter,
            Action<T, int?> primaryKeySetter, Func<T, int?> updatedPrimaryKeyGetter,
            Action<T, int?> updatedPrimaryKeySetter, string fetchQueryToken = null,
            string fetchAllQueryToken = null, string initQueryToken = null, bool scopeIsFlag = false)
            : base(tableName, null, primaryKeyFieldName, primaryKeyPropName, 
                false, fetchQueryToken, null, fetchAllQueryToken, 
                initQueryToken, null, scopeIsFlag, factoryMethod)
        {
            PrimaryKeyGetter = primaryKeyGetter ?? throw new ArgumentNullException(nameof(primaryKeyGetter));
            PrimaryKeySetter = primaryKeySetter ?? throw new ArgumentNullException(nameof(primaryKeySetter));
            UpdatedPrimaryKeyGetter = updatedPrimaryKeyGetter ?? throw new ArgumentNullException(nameof(updatedPrimaryKeyGetter));
            UpdatedPrimaryKeySetter = updatedPrimaryKeySetter ?? throw new ArgumentNullException(nameof(updatedPrimaryKeySetter));
            PrimaryKeyUpdatable = true;
        }

        public IdentityMapParentInt32(string tableName, string primaryKeyFieldName,
            string primaryKeyPropName, Func<T> factoryMethod, Func<T, int?> primaryKeyGetter, 
            Action<T, int?> primaryKeySetter, string fetchQueryToken = null, string fetchAllQueryToken = null, 
            string initQueryToken = null, bool scopeIsFlag = false)
            : base(tableName, null, primaryKeyFieldName, primaryKeyPropName, 
                false, fetchQueryToken, null, fetchAllQueryToken, 
                initQueryToken, null, scopeIsFlag, factoryMethod)
        {
            PrimaryKeyGetter = primaryKeyGetter ?? throw new ArgumentNullException(nameof(primaryKeyGetter));
            PrimaryKeySetter = primaryKeySetter ?? throw new ArgumentNullException(nameof(primaryKeySetter));
            PrimaryKeyUpdatable = false;
        }


        /// <summary>
        /// Gets a primary key value.
        /// </summary>
        public Func<T, int?> PrimaryKeyGetter { get; }

        /// <summary>
        /// Sets a primary key value.
        /// </summary>
        public Action<T, int?> PrimaryKeySetter { get; }
       
        /// <summary>
        /// Gets an updated primary key value.
        /// </summary>
        public Func<T, int?> UpdatedPrimaryKeyGetter { get; }

        /// <summary>
        /// Sets an updated primary key value.
        /// </summary>
        public Action<T, int?> UpdatedPrimaryKeySetter { get; }

        public override bool PrimaryKeyUpdatable { get; }


        internal override SqlParam GetPrimaryKeyParamForInsert(T instance)
        {
            var getter = PrimaryKeyUpdatable ? UpdatedPrimaryKeyGetter : PrimaryKeyGetter;
            var value = getter(instance);
            if (!value.HasValue) throw new InvalidOperationException(
                $"Entity {typeof(T).FullName} doesn't have a primary key value assigned.");
            return SqlParam.Create(PrimaryKeyFieldName, value.Value);
        }

        internal override SqlParam GetPrimaryKeyParamForUpdateSet(T instance)
        {
            return GetPrimaryKeyParamForInsert(instance);
        }

        internal override SqlParam GetPrimaryKeyParamForUpdateWhere(T instance, string paramName)
        {
            var value = PrimaryKeyGetter(instance);
            if (!value.HasValue) throw new InvalidOperationException(
                $"Entity {typeof(T).FullName} doesn't have a primary key value assigned.");

            if (PrimaryKeyUpdatable) return SqlParam.Create(paramName, value.Value);
            return SqlParam.Create(PrimaryKeyFieldName, value.Value);
        }

        internal override void SetPrimaryKeyAutoIncrementValue(T instance, long nid)
        {
            throw new NotSupportedException();
        }

        internal override void LoadPrimaryKeyValue(T instance, LightDataRow row)
        {
            var value = row.GetInt32(PrimaryKeyPropName);
            PrimaryKeySetter(instance, value);
            if (PrimaryKeyUpdatable) UpdatedPrimaryKeySetter(instance, value);
        }

        internal override void LoadPrimaryKeyValue(T instance, ILightDataReader reader)
        {
            var value = reader.GetInt32(PrimaryKeyPropName);
            PrimaryKeySetter(instance, value);
            if (PrimaryKeyUpdatable) UpdatedPrimaryKeySetter(instance, value);
        }

        internal override void UpdatePrimaryKey(T instance)
        {
            if (PrimaryKeyUpdatable) PrimaryKeySetter(instance, UpdatedPrimaryKeyGetter(instance));
        }

        internal override void DeletePrimaryKey(T instance)
        {
            PrimaryKeySetter(instance, null);
            if (PrimaryKeyUpdatable) UpdatedPrimaryKeySetter(instance, null);
        }

        internal override object GetPrimaryKey(T instance)
        {
            return PrimaryKeyGetter(instance);
        }
    }
}
