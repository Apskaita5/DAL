using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.MicroOrm.Core
{
    /// <summary>
    /// Describes how a business entity (identity plus fields) is persisted in a database table.
    /// </summary>
    /// <typeparam name="T">a type of the business object that is described</typeparam>
    /// <remarks>Only meant for use by <see cref="SqlAgentBase"/> implementations.</remarks>
    public sealed class OrmEntityMap<T> where T : class
    {

        private readonly string[] _paramNameCandidates = new string[] 
            { "AA", "AB", "AC", "AD", "CD", "currentKey", "currentId" };

        private readonly OrmIdentityMapBase<T> _identity;
        private readonly List<OrmFieldMapBase<T>> _fields;
        private readonly List<IChildMap<T>> _children;
        private readonly FieldMapInsertedAt<T> _insertedAt;
        private readonly FieldMapInsertedBy<T> _insertedBy;
        private readonly FieldMapUpdatedAt<T> _updatedAt;
        private readonly FieldMapUpdatedBy<T> _updatedBy;


        /// <summary>
        /// Creates a new mapping description for a type of business object.
        /// </summary>
        /// <param name="mapType">a type of the class that defines a database map (if not the same as T)</param>
        public OrmEntityMap(Type mapType)
        {
            var staticFieldsInfo = GetStaticFields(mapType);

            var identityFieldInfo = staticFieldsInfo.FirstOrDefault(f => 
                typeof(OrmIdentityMapBase<T>).IsAssignableFrom(f.FieldType));
            if (identityFieldInfo.IsNull()) throw new NotSupportedException(string.Format(
                Properties.Resources.MicroOrmIsNotSupportedByTypeException, typeof(T).FullName));

            _identity = (OrmIdentityMapBase<T>)identityFieldInfo.GetValue(null);

            _fields = staticFieldsInfo.Where(f => typeof(OrmFieldMapBase<T>).IsAssignableFrom(f.FieldType))
                .Select(f => (OrmFieldMapBase<T>)f.GetValue(null)).ToList();
            _children = staticFieldsInfo.Where(f => typeof(IChildMap<T>).IsAssignableFrom(f.FieldType))
                .Select(f => (IChildMap<T>)f.GetValue(null)).ToList();

            if (null == _fields || _fields.Count < 1) throw new NotSupportedException(
                string.Format(Properties.Resources.NoFieldMapsForMicroOrmException, typeof(T).FullName));

            _insertedAt = _fields.FirstOrDefault(f => f.GetType() == typeof(FieldMapInsertedAt<T>)) as FieldMapInsertedAt<T>;
            _insertedBy = _fields.FirstOrDefault(f => f.GetType() == typeof(FieldMapInsertedBy<T>)) as FieldMapInsertedBy<T>;
            _updatedAt = _fields.FirstOrDefault(f => f.GetType() == typeof(FieldMapUpdatedAt<T>)) as FieldMapUpdatedAt<T>;
            _updatedBy = _fields.FirstOrDefault(f => f.GetType() == typeof(FieldMapUpdatedBy<T>)) as FieldMapUpdatedBy<T>;

            _updateStatements = new ConcurrentDictionary<int, string>(Environment.ProcessorCount * 2, 10);

            if (_identity.PrimaryKeyAutoIncrement || !_identity.PrimaryKeyUpdatable)
                PrimaryKeyUpdateWhereParamName = _identity.PrimaryKeyFieldName;
            else
            {
                PrimaryKeyUpdateWhereParamName = null;
                for (int i = 0; i < _paramNameCandidates.Length; i++)
                {
                    if (!_fields.Any(f => f.DbFieldName.EqualsByConvention(_paramNameCandidates[i])))
                    {
                        PrimaryKeyUpdateWhereParamName = _paramNameCandidates[i];
                        break;
                    }
                }
                if (PrimaryKeyUpdateWhereParamName.IsNullOrWhiteSpace()) throw new NotSupportedException(
                    "Failed to resolve UPDATE statement WHERE clause parameter name.");
            }

        }


        #region Info Props        

        /// <summary>
        /// Gets a name of the database table that the business object is persisted in.
        /// </summary>
        public string TableName => _identity.TableName;

        /// <summary>
        /// Gets a name of the database table field that persists business object parent id value.
        /// Used only for select by parent id functionality.
        /// </summary>
        public string ParentIdFieldName => _identity.ParentIdFieldName;

        /// <summary>
        /// Gets a primary key field name.
        /// </summary>
        public string PrimaryKeyFieldName => _identity.PrimaryKeyFieldName;

        /// <summary>
        /// Gets a name of the primary key parameter for inserts and updates.
        /// </summary>
        public string PrimaryKeyParamName => _identity.PrimaryKeyPropName;

        /// <summary>
        /// Gets a name of the primary key reference parameter for update statement WHERE clause.
        /// </summary>
        public string PrimaryKeyUpdateWhereParamName { get; }

        /// <summary>
        /// Gets a value indicating whether a new primary key is autogenerated by the database.
        /// </summary>
        public bool PrimaryKeyAutoIncrement => _identity.PrimaryKeyAutoIncrement;

        /// <summary>
        /// Gets a value indicating whether a primary key value could be updated manually by user.
        /// </summary>
        public bool PrimaryKeyUpdatable => !_identity.PrimaryKeyAutoIncrement && _identity.PrimaryKeyUpdatable;

        /// <summary>
        /// Gets a fetch query token if custom query should be used.
        /// </summary>
        public string FetchQueryToken => _identity.FetchQueryToken;

        /// <summary>
        /// Gets a fetch by parent id query token if a custom query should be used.
        /// </summary>
        public string FetchByParentIdQueryToken => _identity.FetchByParentIdQueryToken;

        /// <summary>
        /// Gets a fetch all (table) query token if a custom query should be used.
        /// </summary>
        public string FetchAllQueryToken => _identity.FetchAllQueryToken;

        /// <summary>
        /// Gets an init query token to fetch initial values for a new business object (if init required).
        /// </summary>
        public string InitQueryToken => _identity.InitQueryToken;

        #endregion

        #region SqlAgent Managed Props And Methods

        private string _selectQuery;
        private string _selectByParentIdQuery;
        private string _selectByNullParentIdQuery;
        private string _selectAllQuery;
        private string _insertStatement;
        private string _deleteStatement;
        private readonly ConcurrentDictionary<int, string> _updateStatements;
        private string _defaultUpdateStatement;

        /// <summary>
        /// Gets or sets a select query used to fetch object's field values by its primary key.
        /// </summary>
        /// <param name="selectQueryFactory">a factory method to create a select by primary key query</param>
        public string GetOrAddSelectQuery(Func<OrmEntityMap<T>, string> selectQueryFactory)
        {
            if (null == selectQueryFactory) throw new ArgumentNullException(nameof(selectQueryFactory));
            if (_selectQuery.IsNullOrWhiteSpace()) _selectQuery = selectQueryFactory(this);
            return _selectQuery;
        }

        /// <summary>
        /// Gets or sets a select query used to fetch object's field values by its parent id.
        /// </summary>
        /// <param name="selectByParentIdQueryFactory">a factory method to create a select by parent id query</param>
        public string GetOrAddSelectByParentIdQuery(Func<OrmEntityMap<T>, string> selectByParentIdQueryFactory)
        {
            if (null == selectByParentIdQueryFactory) throw new ArgumentNullException(nameof(selectByParentIdQueryFactory));
            
            if (_selectByParentIdQuery.IsNullOrWhiteSpace())
                _selectByParentIdQuery = selectByParentIdQueryFactory(this);
            
            return _selectByParentIdQuery;
        }

        /// <summary>
        /// Gets or sets a select query used to fetch object's field values by its parent id when the parent id is null.
        /// </summary>
        /// <param name="selectByNullParentIdQueryFactory">a factory method to create a select by parent id query when the parent id is null</param>
        public string GetOrAddSelectByNullParentIdQuery(Func<OrmEntityMap<T>, string> selectByNullParentIdQueryFactory)
        {
            if (null == selectByNullParentIdQueryFactory) throw new ArgumentNullException(nameof(selectByNullParentIdQueryFactory));
            
            if (_selectByNullParentIdQuery.IsNullOrWhiteSpace())
                _selectByNullParentIdQuery = selectByNullParentIdQueryFactory(this);
            
            return _selectByNullParentIdQuery;
        }

        /// <summary>
        /// Gets or sets a select query used to fetch object field values for all of the objects of type T in the database.
        /// </summary>
        /// <param name="selectAllQueryFactory">a factory method to create a select all query</param>
        public string GetOrAddSelectAllQuery(Func<OrmEntityMap<T>, string> selectAllQueryFactory)
        {
            if (null == selectAllQueryFactory) throw new ArgumentNullException(nameof(selectAllQueryFactory));
            
            if (_selectAllQuery.IsNullOrWhiteSpace()) _selectAllQuery = selectAllQueryFactory(this);
            
            return _selectAllQuery;
        }

        /// <summary>
        /// Gets or sets an insert statement used to insert object's field values into the database.
        /// </summary>
        /// <param name="insertStatementFactory">a factory method to create an insert statement</param>
        public string GetOrAddInsertStatement(Func<OrmEntityMap<T>, SqlParam[], string> insertStatementFactory,
            SqlParam[] extraParameters)
        {
            if (null == insertStatementFactory) throw new ArgumentNullException(nameof(insertStatementFactory));

            if (_insertStatement.IsNullOrWhiteSpace()) _insertStatement = insertStatementFactory(this, extraParameters);
            
            return _insertStatement;
        }

        /// <summary>
        /// Gets or sets a delete statement used to delete object from the database.
        /// </summary>
        /// <param name="deleteStatementFactory">a factory method to create a delete statement</param>
        public string GetOrAddDeleteStatement(Func<OrmEntityMap<T>, string> deleteStatementFactory)
        {
            if (null == deleteStatementFactory) throw new ArgumentNullException(nameof(deleteStatementFactory));
            
            if (_deleteStatement.IsNullOrWhiteSpace()) _deleteStatement = deleteStatementFactory(this);
            
            return _deleteStatement;
        }

        /// <summary>
        /// Gets an update statement for given scopes. If there is no update statement for given scopes,
        /// adds a statement using the given update statement factory method.
        /// </summary>
        /// <param name="scope">a <see cref="OrmFieldMapBase{T}.UpdateScope">scope</see> of the update operation</param>
        /// <param name="updateStatementFactory">a method to create a new update statement</param>
        public string GetOrAddUpdateStatement(int? scope, Func<OrmEntityMap<T>, int?, string> updateStatementFactory)
        {
            if (updateStatementFactory.IsNull()) throw new ArgumentNullException(nameof(updateStatementFactory));
            if (!scope.HasValue)
            {
                if (_defaultUpdateStatement.IsNullOrWhiteSpace())
                    _defaultUpdateStatement = updateStatementFactory(this, null);
                return _defaultUpdateStatement;
            }
            return _updateStatements.GetOrAdd(scope.Value, s => updateStatementFactory(this, scope));
        }
                
        #endregion

        #region Mapping Methods

        //SELECT

        /// <summary>
        /// Gets a collection of database field name and associated property name pairs for trivial select operation.
        /// Use it to format select query in db_field_name AS PropertyName style.
        /// </summary>
        public (string DbFieldName, string PropName)[] GetFieldsForSelect()
        {
            var result = new List<(string DbFieldName, string PropName)>()
                { _identity.GetPrimaryKeySelectField() };
            result.AddRange(_fields.Where(f => f.PersistenceType.HasFlag(FieldPersistenceType.Read))
                .Select(f => f.GetSelectField()));
            return result.ToArray();
        }

        //INSERT

        /// <summary>
        /// Gets a collection of database table fields for insert operation.
        /// </summary>
        public string[] GetFieldsForInsert()
        {
            var result = _fields.Where(f => f.PersistenceType.HasFlag(FieldPersistenceType.Insert)).
                Select(f => f.DbFieldName).ToList();
            if (!_identity.PrimaryKeyAutoIncrement) result.Add(_identity.PrimaryKeyFieldName);
            return result.ToArray();
        }

        /// <summary>
        /// Gets a collection of <see cref="SqlParam">SqlParams</see> for insert statement.
        /// </summary>
        /// <param name="instance">an instance of the business object to get the param values for</param>
        public SqlParam[] GetParamsForInsert(T instance, SqlParam[] extraParameters = null)
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));

            var result = _fields.Where(f => f.PersistenceType.HasFlag(FieldPersistenceType.Insert)).
                Select(f => f.GetParam(instance)).ToList();
            if (!_identity.PrimaryKeyAutoIncrement) result.Add(_identity.GetPrimaryKeyParamForInsert(instance));
            if (null != extraParameters) result.AddRange(extraParameters);

            return result.ToArray();
        }

        /// <summary>
        /// Sets standard audit fields before inserting the entity data into the database.
        /// </summary>
        /// <param name="instance">an instance of a business object to set the audit data for</param>
        /// <param name="userId">a user description to set in the InsertedBy field</param>
        public void SetAuditFieldsForInsert(T instance, string userId)
        {
            if (!_insertedBy.IsNull() && userId.IsNullOrWhiteSpace()) 
                throw new ArgumentNullException(nameof(userId));
            
            _insertedAt?.InitValue(instance, _updatedAt);
            _insertedBy?.InitValue(instance, userId, _updatedBy);
        }

        /// <summary>
        /// Sets a new primary key that was returned by the database after insert.
        /// </summary>
        /// <param name="instance">an instance of the business object to set the primary key for</param>
        /// <param name="newPrimaryKey">new primary key value</param>
        internal void SetPrimaryKeyAutoIncrementValue(T instance, long newPrimaryKey)
        {
            if (_identity.PrimaryKeyAutoIncrement) 
                _identity.SetPrimaryKeyAutoIncrementValue(instance, newPrimaryKey);
            else 
                throw new InvalidOperationException($"Primary key for entity of type {typeof(T).FullName} is not autoincremented.");
        }

        //UPDATE       

        /// <summary>
        /// Gets a collection of database table fields for update operation of a particular scope.
        /// </summary>
        /// <param name="scope">a <see cref="OrmFieldMapBase{T}.UpdateScope">scope</see> of the update operation;
        /// use null for full update</param>
        public string[] GetFieldsForUpdate(int? scope)
        {
            var result = new List<string>();

            if (_identity.PrimaryKeyIsInUpdateScope(scope)) 
                result.Add(_identity.PrimaryKeyFieldName);

            result.AddRange(_fields.Where(f => 
                    f.PersistenceType.HasFlag(FieldPersistenceType.Update) 
                    && f.UpdateScope.IsInUpdateScope(scope, _identity.ScopeIsFlag))
                .Select(f => f.DbFieldName));
            
            return result.ToArray();
        }

        /// <summary>
        /// Gets a collection of <see cref="SqlParam">SqlParams</see> for update statement.
        /// </summary>
        /// <param name="instance">an instance of the business object to get the params for</param>
        /// <param name="scope">a <see cref="OrmFieldMapBase{T}.UpdateScope">scope</see> of the update operation;
        /// use null for full update</param>
        public SqlParam[] GetParamsForUpdate(T instance, int? scope)
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));

            var result = new List<SqlParam>();

            if (_identity.PrimaryKeyIsInUpdateScope(scope)) 
                result.Add(_identity.GetPrimaryKeyParamForUpdateSet(instance));

            result.AddRange(_fields.Where(f => f.PersistenceType.HasFlag(FieldPersistenceType.Update)
                    && f.UpdateScope.IsInUpdateScope(scope, _identity.ScopeIsFlag))
                .Select(f => f.GetParam(instance)));

            result.Add(_identity.GetPrimaryKeyParamForUpdateWhere(instance, PrimaryKeyUpdateWhereParamName));

            return result.ToArray();
        }

        /// <summary>
        /// Sets standard audit fields before updating the entity data in the database.
        /// </summary>
        /// <param name="instance">an instance of a business object to set the audit data for</param>
        /// <param name="userId">a user description to set in the UpdatedBy field</param>
        public void SetAuditFieldsForUpdate(T instance, string userId)
        {
            if (!_updatedBy.IsNull() && userId.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(userId));

            _updatedAt?.InitValue(instance);
            _updatedBy?.InitValue(instance, userId);
        }

        //DELETE

        /// <summary>
        /// Gets a collection of <see cref="SqlParam">SqlParams</see> for delete statement,
        /// i.e. single param for primary key in array.
        /// </summary>
        /// <param name="instance">an instance of the business object to get the params for</param>
        public SqlParam[] GetParamsForDelete(T instance)
        {
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            return new SqlParam[] { _identity.GetPrimaryKeyParamForUpdateWhere(instance, _identity.PrimaryKeyFieldName) };
        }

        /// <summary>
        /// Sets current (original) primary key value to null after it has been deleted from the database.
        /// </summary>
        /// <param name="instance">an instance of business object to clear the primary key value for</param>
        public void DeletePrimaryKey(T instance)
        {
            _identity.DeletePrimaryKey(instance);
        }

        //LOAD

        /// <summary>
        /// Creates a new instance of business object.
        /// </summary>
        public T CreateUninitializedInstance() => _identity.CreateInstance();

        /// <summary>
        /// Creates a new instance of business object and fills it with the data from database.
        /// </summary>
        public T LoadInstance(LightDataRow row)
        {
            var result = _identity.CreateInstance();
            LoadValues(result, row);
            return result;
        }

        /// <summary>
        /// Loads a business object field values using data fetched from the database.
        /// </summary>
        /// <param name="instance">an instance of business object to load the values for</param>
        /// <param name="row">data fetched from the database by an autogenerated fetch query or
        /// a query defined by <see cref="FetchQueryToken"/></param>
        public void LoadValues(T instance, LightDataRow row)
        {
            if (!_identity.PrimaryKeyPropName.IsNullOrWhiteSpace()) 
                _identity.LoadPrimaryKeyValue(instance, row);
            foreach (var f in _fields
                .Where(f => f.PersistenceType.HasFlag(FieldPersistenceType.Read))) 
                f.SetValue(instance, row);
        }

        /// <summary>
        /// Creates a new instance of business object and fills it with the data from database.
        /// </summary>
        public T LoadInstance(ILightDataReader reader)
        {
            var result = _identity.CreateInstance();
            LoadValues(result, reader);
            return result;
        }

        /// <summary>
        /// Loads a business object field values using data fetched from the database.
        /// </summary>
        /// <param name="instance">an instance of business object to load the values for</param>
        /// <param name="reader">reader for data fetched from the database by an autogenerated fetch query or
        /// a query defined by <see cref="FetchQueryToken"/></param>
        public void LoadValues(T instance, ILightDataReader reader)
        {
            if (!_identity.PrimaryKeyPropName.IsNullOrWhiteSpace())
                _identity.LoadPrimaryKeyValue(instance, reader);
            foreach (var f in _fields
                .Where(f => f.PersistenceType.HasFlag(FieldPersistenceType.Read)))
                f.SetValue(instance, reader);
        }

        /// <summary>
        /// Creates a new instance of business object and initializes it with the data from database.
        /// </summary>
        public T InitInstance(LightDataRow row)
        {
            var result = _identity.CreateInstance();
            InitValues(result, row);
            return result;
        }

        /// <summary>
        /// Initializes a business object field values using data fetched from the database.
        /// </summary>
        /// <param name="instance">an instance of business object to initialize the values for</param>
        /// <param name="row">data fetched from the database by a query defined by <see cref="InitQueryToken"/></param>
        public void InitValues(T instance, LightDataRow row)
        {
            foreach (var f in _fields
                .Where(f => f.PersistenceType.HasFlag(FieldPersistenceType.Init))) 
                f.SetValue(instance, row);
        }

        /// <summary>
        /// Creates a new instance of business object and initializes it with the data from database.
        /// </summary>
        public T InitInstance(ILightDataReader reader)
        {
            var result = _identity.CreateInstance();
            InitValues(result, reader);
            return result;
        }

        /// <summary>
        /// Initializes a business object field values using data fetched from the database.
        /// </summary>
        /// <param name="instance">an instance of business object to initialize the values for</param>
        /// <param name="reader">reader for the data fetched from the database
        /// by a query defined by <see cref="InitQueryToken"/></param>
        public void InitValues(T instance, ILightDataReader reader)
        {
            foreach (var f in _fields
                .Where(f => f.PersistenceType.HasFlag(FieldPersistenceType.Init)))
                f.SetValue(instance, reader);
        }

        // CHILD FIELDS

        /// <summary>
        /// Loads child fields data from a database using MicroOrm service specified.
        /// </summary>
        /// <param name="instance">a parent instance to load the child fields for</param>
        /// <param name="service">a MicroOrm service to use for loading data</param>
        /// <param name="ct">a cancellation token (if any)</param>
        public async Task LoadChildren(T instance, IOrmService service, CancellationToken ct = default)
        {
            var primaryKey = _identity.GetPrimaryKey(instance);
            foreach (var child in _children)
            {
                await child.LoadChildAsync(instance, primaryKey, service, ct);
            }
        }

        /// <summary>
        /// Saves (inserts, updates or deletes) child fields (child entities) data to a database.
        /// </summary>
        /// <param name="instance">a parent instance to save the child fields for</param>
        /// <param name="userId">a user identifier (e.g. email) for audit field UpdatedBy 
        /// (only applicable if the child entity implements standard audit fields)</param>
        /// <param name="scope">a scope of the update operation; a business objects can define
        /// different update scopes (different collections of properties) as an ENUM
        /// which nicely converts into int.</param>
        /// <param name="service">a MicroOrm service to use for saving data</param>
        public async Task SaveChildrenAsync(T instance, string userId, int? scope, IOrmService service)
        {
            var primaryKey = _identity.GetPrimaryKey(instance);
            foreach (var child in _children)
            {
                await child.SaveChildAsync(instance, primaryKey, userId, scope, _identity.ScopeIsFlag, service);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the entity has any child fields.
        /// </summary>
        public bool HasChildren => _children.Count > 0;

        #endregion

        /// <summary>
        /// Updates current (original) primary key value after it has been updated or inserted in database,
        /// i.e. primary key (updated) value -> current primary key value. 
        /// (only applicable for non autoincrement primary key)
        /// </summary>
        /// <param name="instance"></param>
        public void UpdatePrimaryKey(T instance)
        {
            if (!_identity.PrimaryKeyAutoIncrement) 
                _identity.UpdatePrimaryKey(instance);
            else 
                throw new InvalidOperationException($"Primary key for entity of type {typeof(T).FullName} is autoincremented (cannot be updated).");
        }


        private static IEnumerable<FieldInfo> GetStaticFields(Type mapType)
        {
            var fields = new List<FieldInfo>();

            var currentType = mapType ?? typeof(T);

            while (currentType != null)
            {
                fields.AddRange(currentType.GetFields(BindingFlags.NonPublic 
                    | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
                currentType = currentType.GetTypeInfo().BaseType;
            }

            return fields;
        }

    }
}
