﻿using A5Soft.DAL.Core.MicroOrm.Core;
using System;

namespace A5Soft.DAL.Core.MicroOrm
{
    public sealed class IdentityMapChildInt32Autoincrement<T> : OrmIdentityMapBase<T> where T : class
    {

        public IdentityMapChildInt32Autoincrement(string tableName, string primaryKeyFieldName,
            string primaryKeyPropName, string parentIdFieldName, Func<T> factoryMethod, Func<T, int?> primaryKeyGetter,
            Action<T, int?> primaryKeySetter, string fetchQueryToken = null, string fetchByParentIdQueryToken = null,
            string fetchAllQueryToken = null, string initQueryToken = null, bool scopeIsFlag = false)
            : base(tableName, parentIdFieldName, primaryKeyFieldName, primaryKeyPropName, 
                true, fetchQueryToken, fetchByParentIdQueryToken, fetchAllQueryToken, 
                initQueryToken, null, scopeIsFlag, factoryMethod)
        {
            if (parentIdFieldName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(parentIdFieldName));
            PrimaryKeyGetter = primaryKeyGetter ?? throw new ArgumentNullException(nameof(primaryKeyGetter));
            PrimaryKeySetter = primaryKeySetter ?? throw new ArgumentNullException(nameof(primaryKeySetter));
        }


        /// <summary>
        /// Gets a primary key value.
        /// </summary>
        public Func<T, int?> PrimaryKeyGetter { get; }

        /// <summary>
        /// Sets a primary key value.
        /// </summary>
        public Action<T, int?> PrimaryKeySetter { get; }

        public override bool PrimaryKeyUpdatable => false;


        internal override SqlParam GetPrimaryKeyParamForInsert(T instance)
        {
            throw new NotSupportedException();
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
            return SqlParam.Create(PrimaryKeyFieldName, value.Value);
        }

        internal override void SetPrimaryKeyAutoIncrementValue(T instance, long nid)
        {
            PrimaryKeySetter(instance, (int)nid);
        }

        internal override void LoadPrimaryKeyValue(T instance, LightDataRow row)
        {
            PrimaryKeySetter(instance, row.GetInt32(PrimaryKeyPropName));
        }

        internal override void LoadPrimaryKeyValue(T instance, ILightDataReader reader)
        {
            PrimaryKeySetter(instance, reader.GetInt32(PrimaryKeyPropName));
        }

        internal override void UpdatePrimaryKey(T instance)
        {
            throw new NotSupportedException();
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
