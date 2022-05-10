using System;
using System.Collections.Generic;
using System.Linq;
using A5Soft.DAL.Core;
using A5Soft.DAL.Core.MicroOrm;
using A5Soft.DAL.Core.MicroOrm.Core;

namespace A5Soft.DAL.SQLite
{
    /// <summary>
    /// An SQLite implementation of micro ORM.
    /// </summary>
    public class SqliteOrmService : OrmServiceBase
    {
        public override string SqlImplementationId => Extensions.SqliteImplementationId;


        /// <summary>
        /// Creates a new instance of SQLite micro ORM service.
        /// </summary>
        /// <param name="agent">an SQLite agent to use for ORM services</param>
        public SqliteOrmService(SqliteAgent agent, Dictionary<Type, Type> customPocoMaps)
            : base(agent, customPocoMaps) { }


        protected override string GetSelectByParentIdQuery<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForSelect()
                .Select(f =>
                $"{f.DbFieldName.ToConventional(Agent)} AS {f.PropName}"));

            var tableName = map.TableName.ToConventional(Agent);
            var parentIdFieldName = map.ParentIdFieldName.ToConventional(Agent);
            var parentIdParamName = Extensions.ParamPrefix + map.ParentIdFieldName;

            return $"SELECT {fields} FROM {tableName} WHERE {parentIdFieldName}={parentIdParamName};";
        }

        protected override string GetSelectByNullParentIdQuery<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForSelect()
                .Select(f =>
                $"{f.DbFieldName.ToConventional(Agent)} AS {f.PropName}"));

            var tableName = map.TableName.ToConventional(Agent);
            var parentIdFieldName = map.ParentIdFieldName.ToConventional(Agent);

            return $"SELECT {fields} FROM {tableName} WHERE {parentIdFieldName} IS NULL;";
        }

        protected override string GetSelectQuery<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForSelect()
                .Select(f =>
                    $"{f.DbFieldName.ToConventional(Agent)} AS {f.PropName}"));

            var tableName = map.TableName.ToConventional(Agent);
            var primaryKeyFieldName = map.PrimaryKeyFieldName.ToConventional(Agent);
            var primaryKeyParamName = Extensions.ParamPrefix + map.PrimaryKeyFieldName;

            return $"SELECT {fields} FROM {tableName} WHERE {primaryKeyFieldName}={primaryKeyParamName};";
        }

        protected override string GetSelectAllQuery<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForSelect()
                .Select(f =>
                    $"{f.DbFieldName.ToConventional(Agent)} AS {f.PropName}"));

            return $"SELECT {fields} FROM {map.TableName.ToConventional(Agent)};";
        }

        protected override string GetInsertStatement<T>(OrmEntityMap<T> map, SqlParam[] extraParams)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var propList = new List<string>(map.GetFieldsForInsert());
            if (extraParams != null) propList.AddRange(extraParams.Select(p => p.Name.Trim()));

            var fields = string.Join(", ", propList
                .Select(p => p.ToConventional(Agent)));
            var parameters = string.Join(", ", propList
                .Select(p => Extensions.ParamPrefix + p));

            return $"INSERT INTO {map.TableName.ToConventional(Agent)}({fields}) VALUES({parameters});";
        }

        protected override string GetUpdateStatement<T>(OrmEntityMap<T> map, int? scope)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var fields = string.Join(", ", map.GetFieldsForUpdate(scope)
                .Select(f => $"{f.ToConventional(Agent)}={Extensions.ParamPrefix + f}"));

            var tableName = map.TableName.ToConventional(Agent);
            var primaryKeyFieldName = map.PrimaryKeyFieldName.ToConventional(Agent);
            var primaryKeyParamName = Extensions.ParamPrefix + map.PrimaryKeyUpdateWhereParamName;

            return $"UPDATE {tableName} SET {fields} WHERE {primaryKeyFieldName}={primaryKeyParamName};";
        }

        protected override string GetDeleteStatement<T>(OrmEntityMap<T> map)
        {
            if (map.IsNull()) throw new ArgumentNullException(nameof(map));

            var tableName = map.TableName.ToConventional(Agent);
            var primaryKeyFieldName = map.PrimaryKeyFieldName.ToConventional(Agent);
            var primaryKeyParamName = Extensions.ParamPrefix + map.PrimaryKeyFieldName;

            return $"DELETE FROM {tableName} WHERE {tableName}.{primaryKeyFieldName}={primaryKeyParamName};";
        }
    }
}
