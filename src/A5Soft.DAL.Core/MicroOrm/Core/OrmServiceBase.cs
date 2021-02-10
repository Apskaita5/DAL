using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.MicroOrm.Core
{
    /// <summary>
    /// A base class for a concrete SQL implementation, e.g. MySql, SQLite, etc.
    /// </summary>
    public abstract class OrmServiceBase : IOrmService
    {
        private readonly Dictionary<Type, Type> _customPocoMaps;
        private static readonly ConcurrentDictionary<Type, object> _maps =
            new ConcurrentDictionary<Type, object>(Environment.ProcessorCount * 2, 1000);


        /// <summary>
        /// Gets an id of the concrete SQL implementation, e.g. MySQL, SQLite.
        /// The id is used to make sure that the OrmServiceBase implementation match SqlAgentBase implementation.
        /// </summary>
        public abstract string SqlImplementationId { get; }

        /// <summary>
        /// Gets an instance of an Sql agent to use for queries and statements.
        /// </summary>
        public ISqlAgent Agent { get; }


        /// <summary>
        /// Creates a new Orm service.
        /// </summary>
        /// <param name="agent">an instance of an Sql agent to use for queries and statements; its implementation
        /// type should match Orm service implementation type</param>
        /// <param name="customPocoMaps">custom (type) maps for POCO business classes
        /// that are defined in a different class</param>
        protected OrmServiceBase(ISqlAgent agent, Dictionary<Type, Type> customPocoMaps)
        {
            Agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _customPocoMaps = customPocoMaps;

            if (!Agent.SqlImplementationId.EqualsByConvention(SqlImplementationId))
                throw new ArgumentException(string.Format(Properties.Resources.SqlAgentAndOrmServiceTypeMismatchException,
                    Agent.SqlImplementationId, SqlImplementationId), nameof(agent));
        }


        #region Fetch And Load Methods

        /// <inheritdoc cref="IOrmService.FetchEntityAsync{T}"/>
        public async Task<T> FetchEntityAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
        {
            if (id.IsNull()) throw new ArgumentNullException(nameof(id));
             
            var reader = await GetReaderAsync<T>(id, cancellationToken).ConfigureAwait(false);

            try
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return null;

                var map = GetOrCreateMap<T>();

                return map.LoadInstance(reader);
            }
            finally
            {
                await reader.CloseAsync();
            }
        }

        /// <inheritdoc cref="IOrmService.FetchChildEntitiesAsync{T}"/>
        public async Task<List<T>> FetchChildEntitiesAsync<T>(object parentId, 
            CancellationToken cancellationToken = default) where T : class
        {
            var reader = await GetReaderByParentAsync<T>(parentId, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var map = GetOrCreateMap<T>();

                var result = new List<T>();

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    result.Add(map.LoadInstance(reader));
                }

                return result;
            }
            finally
            {
                await reader.CloseAsync();
            }
        }

        /// <inheritdoc cref="IOrmService.QueryAsync{T}"/>
        public async Task<List<T>> QueryAsync<T>(SqlParam[] parameters,
            CancellationToken cancellationToken = default) where T : class
        {
            var reader = await GetQueryReaderAsync<T>(parameters, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var map = GetOrCreateMap<T>();

                var result = new List<T>();

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    result.Add(map.LoadInstance(reader));
                }

                return result;
            }
            finally
            {
                await reader.CloseAsync();
            }
        }

        /// <inheritdoc cref="IOrmService.FetchAllEntitiesAsync{T}"/>
        public async Task<List<T>> FetchAllEntitiesAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            var reader = await GetReaderForAllAsync<T>(cancellationToken).ConfigureAwait(false);

            try
            {
                var map = GetOrCreateMap<T>();

                var result = new List<T>();

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    result.Add(map.LoadInstance(reader));
                }

                return result;
            }
            finally
            {
                await reader.CloseAsync();
            }
        }

        /// <inheritdoc cref="IOrmService.FetchTableAsync{T}"/>
        public Task<LightDataTable> FetchTableAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
        {
            if (id.IsNull()) throw new ArgumentNullException(nameof(id));

            var map = GetOrCreateMap<T>();

            var parameters = new SqlParam[] { SqlParam.Create(map.PrimaryKeyFieldName, id) };

            if (!map.FetchQueryToken.IsNullOrWhiteSpace())
            {
                return Agent.FetchTableAsync(map.FetchQueryToken, parameters, cancellationToken);
            }

            string query = map.GetOrAddSelectQuery(GetSelectQuery);

            return Agent.FetchTableRawAsync(query, parameters, cancellationToken);
        }

        /// <inheritdoc cref="IOrmService.FetchQueryTableAsync{T}"/>
        public Task<LightDataTable> FetchQueryTableAsync<T>(SqlParam[] parameters,
            CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();

            if (map.FetchQueryToken.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                $"Fetch query token is not configured for query result of type {typeof(T).FullName}.");

            return Agent.FetchTableAsync(map.FetchQueryToken, parameters, cancellationToken);
        }

        /// <inheritdoc cref="IOrmService.GetReaderAsync{T}"/>
        public async Task<ILightDataReader> GetReaderAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
        {
            if (id.IsNull()) throw new ArgumentNullException(nameof(id));

            var map = GetOrCreateMap<T>();

            var parameters = new SqlParam[] { SqlParam.Create(map.PrimaryKeyFieldName, id) };

            if (!map.FetchQueryToken.IsNullOrWhiteSpace())
            {
                return await Agent.GetReaderAsync(map.FetchQueryToken, parameters, cancellationToken)
                    .ConfigureAwait(false);
            }

            string query = map.GetOrAddSelectQuery(GetSelectQuery);

            return await Agent.GetReaderRawAsync(query, parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="IOrmService.GetQueryReaderAsync{T}"/>
        public async Task<ILightDataReader> GetQueryReaderAsync<T>(SqlParam[] parameters, 
            CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();

            if (map.FetchQueryToken.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                $"Fetch query token is not configured for query result of type {typeof(T).FullName}.");

            return await Agent.GetReaderAsync(map.FetchQueryToken, parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="IOrmService.FetchTableByParentAsync{T}"/>
        public Task<LightDataTable> FetchTableByParentAsync<T>(object parentId, 
            CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();

            if (map.ParentIdFieldName.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                $"Entity {typeof(T).FullName} is not a child entity and cannot be fetched by a parent id.");

            SqlParam[] parameters = null;
            if (!parentId.IsNull()) parameters = new SqlParam[] { SqlParam.Create(map.ParentIdFieldName, parentId) };

            if (!map.FetchByParentIdQueryToken.IsNullOrWhiteSpace())
            {
                return Agent.FetchTableAsync(map.FetchByParentIdQueryToken, parameters, cancellationToken);
            }

            string query;
            if (parentId.IsNull())
            {
                query = map.GetOrAddSelectByNullParentIdQuery(GetSelectByNullParentIdQuery);
            }
            else
            {
                query = map.GetOrAddSelectByParentIdQuery(GetSelectByParentIdQuery);
            }

            return Agent.FetchTableRawAsync(query, parameters, cancellationToken);
        }

        /// <inheritdoc cref="IOrmService.GetReaderByParentAsync{T}"/>
        public async Task<ILightDataReader> GetReaderByParentAsync<T>(object parentId,
            CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();

            if (map.ParentIdFieldName.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                $"Entity {typeof(T).FullName} is not a child entity and cannot be fetched by a parent id.");

            SqlParam[] parameters = null;
            if (!parentId.IsNull()) parameters = new SqlParam[] { SqlParam.Create(map.ParentIdFieldName, parentId) };
              
            if (!map.FetchByParentIdQueryToken.IsNullOrWhiteSpace())
            {
                return await Agent.GetReaderAsync(map.FetchByParentIdQueryToken, parameters, 
                    cancellationToken).ConfigureAwait(false);
            }

            string query;
            if (parentId.IsNull())
            {
                query = map.GetOrAddSelectByNullParentIdQuery(GetSelectByNullParentIdQuery);
            }
            else
            {
                query = map.GetOrAddSelectByParentIdQuery(GetSelectByParentIdQuery);
            }

            return await Agent.GetReaderRawAsync(query, parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="IOrmService.FetchTableForAllAsync{T}"/>
        public Task<LightDataTable> FetchTableForAllAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();

            if (!map.FetchAllQueryToken.IsNullOrWhiteSpace())
                return Agent.FetchTableAsync(map.FetchAllQueryToken, null, cancellationToken);

            return Agent.FetchTableRawAsync(map.GetOrAddSelectAllQuery(GetSelectAllQuery), 
                null, cancellationToken);
        }

        /// <inheritdoc cref="IOrmService.GetReaderForAllAsync{T}"/>
        public async Task<ILightDataReader> GetReaderForAllAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();
            
            if (map.FetchAllQueryToken.IsNullOrWhiteSpace())
            {
                return await Agent.GetReaderRawAsync(map.GetOrAddSelectAllQuery(GetSelectAllQuery),
                    null, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await Agent.GetReaderAsync(map.FetchAllQueryToken, null, 
                    cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IOrmService.LoadObjectFieldsAsync{T}"/>
        public async Task LoadObjectFieldsAsync<T>(T instance, object id, 
            CancellationToken cancellationToken = default) where T : class
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (id.IsNull()) throw new ArgumentNullException(nameof(id));

            var reader = await GetReaderAsync<T>(id, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException(
                        $"Entity of type {typeof(T).FullName} identified by {id} does not exist in database.");

                LoadObjectFields(instance, reader);
            }
            finally
            {
                await reader.CloseAsync();
            }
        }

        /// <inheritdoc cref="o:IOrmService.LoadObjectFields{T}"/>
        public void LoadObjectFields<T>(T instance, LightDataRow databaseRow) where T : class
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (databaseRow.IsNull()) throw new ArgumentNullException(nameof(databaseRow));

            var map = GetOrCreateMap<T>();

            map.LoadValues(instance, databaseRow);
        }

        /// <inheritdoc cref="o:IOrmService.LoadObjectFields{T}"/>
        public void LoadObjectFields<T>(T instance, ILightDataReader reader) where T : class
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (reader.IsNull()) throw new ArgumentNullException(nameof(reader));

            var map = GetOrCreateMap<T>();

            map.LoadValues(instance, reader);
        }

        /// <summary>
        /// Gets a (trivial) select by parent id statement for business objects of type T using integrated micro ORM.
        /// </summary>
        /// <typeparam name="T">a type of a business objects to get a select statement for</typeparam>
        /// <param name="map">a micro ORM map for a business object type</param>
        protected abstract string GetSelectByParentIdQuery<T>(OrmEntityMap<T> map) where T : class;

        /// <summary>
        /// Gets a (trivial) select by null parent id statement for business objects of type T using integrated micro ORM.
        /// </summary>
        /// <typeparam name="T">a type of a business objects to get a select statement for</typeparam>
        /// <param name="map">a micro ORM map for a business object type</param>
        protected abstract string GetSelectByNullParentIdQuery<T>(OrmEntityMap<T> map) where T : class;

        /// <summary>
        /// Gets a (trivial) select by primary key statement for a business object of type T using integrated micro ORM.
        /// </summary>
        /// <typeparam name="T">a type of a business object to get a statement for</typeparam>
        /// <param name="map">a micro ORM map for a business object type</param>
        protected abstract string GetSelectQuery<T>(OrmEntityMap<T> map) where T : class;

        /// <summary>
        /// Gets a (trivial) select all (table) query for business objects of type T using integrated micro ORM.
        /// </summary>
        /// <typeparam name="T">a type of business objects to get a statement for</typeparam>
        /// <param name="map">a micro ORM map for a business object type</param>
        protected abstract string GetSelectAllQuery<T>(OrmEntityMap<T> map) where T : class;

        #endregion

        #region Initialization Methods

        /// <inheritdoc cref="IOrmService.InitEntityAsync{T}"/>
        public async Task<T> InitEntityAsync<T>(SqlParam[] parameters, 
            CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();

            if (map.InitQueryToken.IsNullOrWhiteSpace()) throw new NotSupportedException(
                string.Format(Properties.Resources.InitQueryTokenNullException, typeof(T).FullName));

            var reader = await Agent.GetReaderAsync(map.InitQueryToken, parameters, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    throw new EntityContextNotFoundException(typeof(T), map.InitQueryToken, parameters);

                return map.InitInstance(reader);
            }
            finally
            {
                await reader.CloseAsync();
            }
        }

        /// <inheritdoc cref="IOrmService.FetchInitTableAsync{T}"/>
        public Task<LightDataTable> FetchInitTableAsync<T>(SqlParam[] parameters, 
            CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();

            if (map.InitQueryToken.IsNullOrWhiteSpace()) throw new NotSupportedException(
                string.Format(Properties.Resources.InitQueryTokenNullException, typeof(T).FullName));

            return Agent.FetchTableAsync(map.InitQueryToken, parameters, cancellationToken);
        }

        /// <inheritdoc cref="IOrmService.GetInitReaderAsync{T}"/>
        public Task<ILightDataReader> GetInitReaderAsync<T>(SqlParam[] parameters,
            CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();

            if (map.InitQueryToken.IsNullOrWhiteSpace()) throw new NotSupportedException(
                string.Format(Properties.Resources.InitQueryTokenNullException, typeof(T).FullName));

            return Agent.GetReaderAsync(map.InitQueryToken, parameters, cancellationToken);
        }

        /// <inheritdoc cref="IOrmService.InitObjectFieldsAsync{T}"/>
        public async Task InitObjectFieldsAsync<T>(T instance, SqlParam[] parameters, 
            CancellationToken cancellationToken = default) where T : class
        {
            var map = GetOrCreateMap<T>();

            var reader = await GetInitReaderAsync<T>(parameters, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    throw new EntityContextNotFoundException(typeof(T), map.InitQueryToken, parameters);

                InitObjectFields(instance, reader);
            }
            finally
            {
                await reader.CloseAsync();
            }
        }

        /// <inheritdoc cref="o:IOrmService.InitObjectFields{T}"/>
        public void InitObjectFields<T>(T instance, LightDataRow databaseRow) where T : class
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (databaseRow.IsNull()) throw new ArgumentNullException(nameof(databaseRow));

            var map = GetOrCreateMap<T>();

            map.InitValues(instance, databaseRow);
        }

        /// <inheritdoc cref="o:IOrmService.InitObjectFields{T}"/>
        public void InitObjectFields<T>(T instance, ILightDataReader reader) where T : class
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (reader.IsNull()) throw new ArgumentNullException(nameof(reader));

            var map = GetOrCreateMap<T>();

            map.InitValues(instance, reader);
        }

        #endregion

        #region Insert Methods

        /// <inheritdoc cref="IOrmService.ExecuteInsertAsync{T}"/>
        public async Task ExecuteInsertAsync<T>(T instance, string userId = null, 
            SqlParam[] extraParameters = null) where T : class
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));

            var map = GetOrCreateMap<T>();

            map.SetAuditFieldsForInsert(instance, userId);

            if (map.PrimaryKeyAutoIncrement)
            {
                var newPrimaryKey = await Agent.ExecuteInsertRawAsync(
                    map.GetOrAddInsertStatement(GetInsertStatement, extraParameters), 
                    map.GetParamsForInsert(instance, extraParameters))
                    .ConfigureAwait(false);
                
                map.SetPrimaryKeyAutoIncrementValue(instance, newPrimaryKey);
            }
            else
            {
                await Agent.ExecuteInsertRawAsync(
                    map.GetOrAddInsertStatement(GetInsertStatement, extraParameters),
                    map.GetParamsForInsert(instance, extraParameters))
                    .ConfigureAwait(false);
                map.UpdatePrimaryKey(instance);
            }
        }

        /// <inheritdoc cref="IOrmService.ExecuteInsertChildAsync{T}"/>
        public async Task ExecuteInsertChildAsync<T>(T instance, object parentId, string userId = null) where T : class
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (parentId.IsNull()) throw new ArgumentNullException(nameof(parentId));

            var map = GetOrCreateMap<T>();

            if (map.ParentIdFieldName.IsNullOrWhiteSpace()) throw new InvalidOperationException(
                $"Parent ID field name is not set for (child) type {typeof(T).FullName}.");

            var extraParameters = new SqlParam[]{ SqlParam.Create(map.ParentIdFieldName, parentId) };

            await ExecuteInsertAsync(instance, userId, extraParameters);
        }

        /// <summary>
        /// Gets a (trivial) insert statement for a business object of type T using integrated micro ORM.
        /// </summary>
        /// <typeparam name="T">a type of a business object to get a statement for</typeparam>
        /// <param name="map">a micro ORM map for a business object type</param>
        /// <param name="extraParams">extra parameters for insert if some of the business object data
        /// fields are not reflected as properties, e.g. operation type, parent id etc.;
        /// this kind of fields shall be insert only; name of a parameter for such a field shall
        /// match database field name</param>
        protected abstract string GetInsertStatement<T>(OrmEntityMap<T> map, SqlParam[] extraParams) where T : class;

        #endregion

        #region Update Methods

        /// <inheritdoc cref="IOrmService.ExecuteUpdateAsync{T}"/>
        public async Task<int> ExecuteUpdateAsync<T>(T instance, string userId = null, int? scope = null) where T : class
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));

            var map = GetOrCreateMap<T>();

            map.SetAuditFieldsForUpdate(instance, userId);

            var result = await Agent.ExecuteCommandRawAsync(
                map.GetOrAddUpdateStatement(scope, GetUpdateStatement),
                map.GetParamsForUpdate(instance, scope))
                .ConfigureAwait(false);

            if (map.PrimaryKeyUpdatable) map.UpdatePrimaryKey(instance);

            return result;
        }

        /// <summary>
        /// Gets an update statement for a business object of type T for a particular update scope. 
        /// </summary>
        /// <typeparam name="T">a type of a business object to get an update statement for</typeparam>
        /// <param name="map">a micro ORM map for a business object type</param>
        /// <param name="scope">a scope of the update operation; a business objects can define
        /// different update scopes (different collections of properties) as an ENUM
        /// which nicely converts into int.</param>
        protected abstract string GetUpdateStatement<T>(OrmEntityMap<T> map, int? scope) where T : class;

        #endregion

        #region Delete Methods

        /// <inheritdoc cref="o:IOrmService.ExecuteDeleteAsync{T}"/>
        public async Task<int> ExecuteDeleteAsync<T>(T instance) where T : class
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));

            var map = GetOrCreateMap<T>();

            var result = await Agent.ExecuteCommandRawAsync(
                map.GetOrAddDeleteStatement(GetDeleteStatement),
                map.GetParamsForDelete(instance))
                .ConfigureAwait(false);

            map.DeletePrimaryKey(instance);

            return result;
        }

        /// <inheritdoc cref="o:IOrmService.ExecuteDeleteAsync{T}"/>
        public Task<int> ExecuteDeleteAsync<T>(object primaryKey) where T : class
        {
            if (primaryKey.IsNull()) throw new ArgumentNullException(nameof(primaryKey));

            var map = GetOrCreateMap<T>();

            return Agent.ExecuteCommandRawAsync(
                map.GetOrAddDeleteStatement(GetDeleteStatement),
                new SqlParam[] { SqlParam.Create(map.PrimaryKeyFieldName, primaryKey) });
        }

        /// <summary>
        /// Gets a delete statement for a business object of type T.
        /// </summary>
        /// <typeparam name="T">a type of a business object to get a delete statement for</typeparam>
        /// <param name="map">a micro ORM map for a business object type</param>
        protected abstract string GetDeleteStatement<T>(OrmEntityMap<T> map) where T : class;

        #endregion

        protected OrmEntityMap<T> GetOrCreateMap<T>() where T : class
        {
            return (OrmEntityMap<T>)_maps.GetOrAdd(typeof(T), 
                type => new OrmEntityMap<T>(_customPocoMaps?.GetValueOrDefault(typeof(T))));
        }

    }
}
