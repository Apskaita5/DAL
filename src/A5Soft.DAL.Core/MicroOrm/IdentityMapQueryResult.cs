using System;
using A5Soft.DAL.Core.MicroOrm.Core;

namespace A5Soft.DAL.Core.MicroOrm
{
    public sealed class IdentityMapQueryResult<T> : OrmIdentityMapBase<T> where T : class
    {
        public override bool PrimaryKeyUpdatable => false;


        public IdentityMapQueryResult(Func<T> factoryMethod, string fetchQueryToken = null)
            : base(string.Empty, null, string.Empty, 
                string.Empty, false, fetchQueryToken, 
                null, null, null, null, 
                false, factoryMethod) { }



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
            throw new NotSupportedException();
        }

        internal override void SetPrimaryKeyAutoIncrementValue(T instance, long nid)
        {
            throw new NotSupportedException();
        }

        internal override void LoadPrimaryKeyValue(T instance, LightDataRow row)
        {
            throw new NotSupportedException();
        }

        internal override void LoadPrimaryKeyValue(T instance, ILightDataReader reader)
        {
            throw new NotSupportedException();
        }

        internal override void UpdatePrimaryKey(T instance)
        {
            throw new NotSupportedException();
        }

        internal override void DeletePrimaryKey(T instance)
        {
            throw new NotSupportedException();
        }

        internal override object GetPrimaryKey(T instance)
        {
            throw new NotSupportedException();
        }
    }
}
