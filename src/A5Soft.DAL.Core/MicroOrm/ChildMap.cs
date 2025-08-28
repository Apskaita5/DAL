using A5Soft.DAL.Core.MicroOrm.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// represents a child class mapper, i.e. a description of how a business object's child object
    /// is persisted in a database;
    /// instances per field type should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="TParent">type of parent class</typeparam>
    /// <typeparam name="TChild">type of child class</typeparam>
    public sealed class ChildMap<TParent, TChild> : IChildMap<TParent> where TParent : class where TChild : class
    {
        /// <summary>
        /// Creates a new instance of child field map.
        /// </summary>
        /// <param name="propName">a name of the property that the child is managed by</param>
        /// <param name="allowNull">whether the child could be null</param>
        /// <param name="valueGetter">a method to get a value of the mapped field for a class instance</param>
        /// <param name="valueSetter">a method to set a value of the mapped field for a class instance</param>
        /// <param name="isNewGetter">a method to get a value of the mapped field for a class instance</param>
        /// <param name="persistenceType">a value indicating how a field value (child entity) is persisted in the database</param>
        /// <param name="updateScope">an update scope that updates the property value (child entity) in database
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.
        /// If multiple scope combinations are used, the enum should be defined as [Flags].
        /// in that case: (a) a field should only be assigned to a single scope;
        /// (b) a bitwise check is used: (fieldScope & requestedScope) != 0;
        /// (c) <see cref="OrmIdentityMapBase{T}.ScopeIsFlag"/> should be set to true.</param>
        public ChildMap(string propName, bool allowNull, Func<TParent, TChild> valueGetter,
            Action<TParent, TChild> valueSetter, Func<TChild, bool> isNewGetter,
            FieldPersistenceType persistenceType = FieldPersistenceType.Read | FieldPersistenceType.Insert
                | FieldPersistenceType.Update, int? updateScope = null)
        {
            if (propName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(propName));

            PropName = propName.Trim();
            PersistenceType = persistenceType;
            AllowNull = allowNull;
            UpdateScope = updateScope;
            ValueGetter = valueGetter ?? throw new ArgumentNullException(nameof(valueGetter));
            ValueSetter = valueSetter ?? throw new ArgumentNullException(nameof(valueSetter));
            IsNewGetter = isNewGetter ?? throw new ArgumentNullException(nameof(isNewGetter));
        }

        #region Properties

        /// <summary>
        /// a name of the property that the child value is managed by
        /// </summary>
        public string PropName { get; }

        /// <summary>
        /// a value indicating how a field value (child entity) is persisted in the database
        /// </summary>
        public FieldPersistenceType PersistenceType { get; }

        /// <summary>
        /// whether the child could be null
        /// </summary>
        public bool AllowNull { get; }

        /// <summary>
        /// an update scope that updates the property value (child entity) in database
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.
        /// If multiple scope combinations are used, the enum should be defined as [Flags].
        /// in that case: (a) a field should only be assigned to a single scope;
        /// (b) a bitwise check is used: (fieldScope & requestedScope) != 0;
        /// (c) <see cref="OrmIdentityMapBase{T}.ScopeIsFlag"/> should be set to true.
        /// </summary>
        public int? UpdateScope { get; }

        /// <summary>
        /// a method to get a value of the mapped field for a class instance.
        /// </summary>
        private Func<TParent, TChild> ValueGetter { get; }

        /// <summary>
        /// a method to set a value of the mapped field for a class instance.
        /// </summary>
        private Action<TParent, TChild> ValueSetter { get; }

        /// <summary>
        /// a method to get a value indicating whether a child instance is a new entity (i.e. insert vs. update)
        /// </summary>
        private Func<TChild, bool> IsNewGetter { get; }

        #endregion

        /// <inheritdoc cref="IChildMap{TParent}.LoadChildAsync"/>
        public async Task LoadChildAsync(TParent instance, object parentId, IOrmService service, CancellationToken ct)
        {
            if (service.IsNull()) throw new ArgumentNullException(nameof(service));
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (parentId.IsNull()) throw new ArgumentNullException(nameof(parentId));

            if (!PersistenceType.HasFlag(FieldPersistenceType.Read)) return;

            var child = await service.FetchChildEntitiesAsync<TChild>(parentId, ct);

            if (child.Count > 0)
            {
                if (child.Count > 1) throw new InvalidOperationException(
                    $"Fetched multiple ({child.Count}) children of type {typeof(TChild).FullName} for parent of type {typeof(TParent).FullName} using parent id - {parentId}.");

                ValueSetter(instance, child[0]);
            }
            else
            {
                if (!AllowNull) throw new InvalidOperationException(
                    $"Failed to fetch a child of type {typeof(TChild).FullName} for parent of type {typeof(TParent).FullName} using parent id - {parentId}.");

                ValueSetter(instance, null);
            }
        }

        /// <inheritdoc cref="IChildMap{TParent}.SaveChildAsync"/>
        public async Task SaveChildAsync(TParent instance, object parentId, string userId, int? scope,
            bool scopeIsFlag, IOrmService service)
        {
            if (service.IsNull()) throw new ArgumentNullException(nameof(service));
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (parentId.IsNull()) throw new ArgumentNullException(nameof(parentId));

            var childInstance = ValueGetter(instance);
            var isNew = IsNewGetter(childInstance);

            if (isNew && PersistenceType.HasFlag(FieldPersistenceType.Insert))
                await service.ExecuteInsertChildAsync(childInstance, parentId, userId);
            else if (!isNew && PersistenceType.HasFlag(FieldPersistenceType.Update)
                && UpdateScope.IsInUpdateScope(scope, scopeIsFlag))
                await service.ExecuteUpdateAsync(childInstance, userId, scope);
        }
    }
}
