using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using A5Soft.DAL.Core;
using A5Soft.DAL.Core.MicroOrm;
using A5Soft.DAL.Core.MicroOrm.Core;

namespace A5Soft.DAL.MySql
{
    /// <summary>
    /// MySql implementation of micro ORM.
    /// </summary>
    public class MySqlOrmService : OrmServiceBase
    {

        public override string SqlImplementationId => Extensions.MySqlImplementationId;


        /// <summary>
        /// Creates a new micro ORM for MySql implementation.
        /// </summary>
        /// <param name="agent">a MySql agent to use for ORM service</param>
        /// <param name="customPocoMaps">custom (type) maps for POCO business classes
        /// that are defined in a different class</param>
        public MySqlOrmService(MySqlAgent agent, Dictionary<Type, Type> customPocoMaps) 
            : base(agent, customPocoMaps) { }


        protected override string GetSelectByParentIdQuery<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForSelect().Select(f => 
                $"{f.DbFieldName.ToConventional(Agent)} AS {f.PropName.Trim()}"));

            var table = map.TableName.ToConventional(Agent);
            var parentIdFieldName = map.ParentIdFieldName.ToConventional(Agent);
            var parentIdParamName = Extensions.ParamPrefix + map.ParentIdFieldName;

            return $"SELECT {fields} FROM {table} WHERE {parentIdFieldName}={parentIdParamName};";
        }

        protected override string GetSelectByNullParentIdQuery<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForSelect().Select(
                f => $"{f.DbFieldName.ToConventional(Agent)} AS {f.PropName.Trim()}"));
            var table = map.TableName.ToConventional(Agent);
            var parentIdFieldName = map.ParentIdFieldName.ToConventional(Agent);

            return $"SELECT {fields} FROM {table} WHERE {parentIdFieldName} IS NULL;";
        }

        protected override string GetSelectQuery<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForSelect().Select(
                f => $"{f.DbFieldName.ToConventional(Agent)} AS {f.PropName.Trim()}"));

            var table = map.TableName.ToConventional(Agent);
            var primaryKeyFieldName = map.PrimaryKeyFieldName.ToConventional(Agent);
            var primaryKeyParamName = Extensions.ParamPrefix + map.PrimaryKeyFieldName;

            return $"SELECT {fields} FROM {table} WHERE {primaryKeyFieldName}={primaryKeyParamName};";
        }

        protected override string GetSelectAllQuery<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForSelect().Select(
                f => $"{f.DbFieldName.ToConventional(Agent)} AS {f.PropName.Trim()}"));

            return $"SELECT {fields} FROM {map.TableName.ToConventional(Agent)};";
        }

        protected override string GetInsertStatement<T>(OrmEntityMap<T> map, SqlParam[] extraParams = null)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var propList = new List<string>(map.GetFieldsForInsert());
            if (extraParams != null) propList.AddRange(extraParams.Select(p => p.Name.Trim()));

            var fields = string.Join(", ", propList.Select(
                p => p.ToConventional(Agent)));
            var parameters = string.Join(", ", propList.Select(
                p => Extensions.ParamPrefix + p));

            return $"INSERT INTO {map.TableName.ToConventional(Agent)}({fields}) VALUES({parameters});";
        }

        protected override string GetUpdateStatement<T>(OrmEntityMap<T> map, int? scope = null)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForUpdate(scope).Select(
                f => $"{f.ToConventional(Agent)}={Extensions.ParamPrefix + f}"));

            var table = map.TableName.ToConventional(Agent);
            var primaryKeyFieldName = map.PrimaryKeyFieldName.ToConventional(Agent);
            var primaryKeyParamName = Extensions.ParamPrefix + map.PrimaryKeyUpdateWhereParamName;

            return $"UPDATE {table} SET {fields} WHERE {primaryKeyFieldName}={primaryKeyParamName};";
        }

        protected override string GetDeleteStatement<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var table = map.TableName.ToConventional(Agent);
            var primaryKeyFieldName = map.PrimaryKeyFieldName.ToConventional(Agent);
            var primaryKeyParamName = Extensions.ParamPrefix + map.PrimaryKeyFieldName;

            return $"DELETE FROM {table} WHERE {table}.{primaryKeyFieldName}={primaryKeyParamName};";
        }

    }
}
