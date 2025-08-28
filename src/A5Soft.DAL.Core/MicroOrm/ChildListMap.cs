using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using A5Soft.DAL.Core.MicroOrm.Core;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// represents a child class list mapper, i.e. a description of how a business object's child objects list
    /// is persisted in a database;
    /// instances per field type should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="TParent">type of parent class</typeparam>
    /// <typeparam name="TChild">type of child class</typeparam>
    public sealed class ChildListMap<TParent, TChild> : IChildMap<TParent>
        where TParent : class where TChild : class
    {
        /// <summary>
        /// Creates a new instance of child list field map.
        /// </summary>
        /// <param name="propName">a name of the the property that the child list is managed by</param>
        /// <param name="valueGetter">a method to get a value of the mapped field for a class instance</param>
        /// <param name="valueSetter">a method to set a value of the mapped field for a class instance</param>
        /// <param name="isNewGetter">a method to get a value indicating whether a child instance is a new entity (i.e. insert vs. update)</param>
        /// <param name="isDirtyGetter">a method to get a value indicating whether a child instance data is dirty (i.e. requires actual update)</param>
        /// <param name="persistenceType">a value indicating how a field value (child entity) is persisted in the database</param>
        /// <param name="updateScope">an update scope that updates the property value (child entity) in database
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.
        /// If multiple scope combinations are used, the enum should be defined as [Flags].
        /// in that case: (a) a field should only be assigned to a single scope;
        /// (b) a bitwise check is used: (fieldScope & requestedScope) != 0;
        /// (c) <see cref="OrmIdentityMapBase{T}.ScopeIsFlag"/> should be set to true.</param>
        public ChildListMap(string propName, Func<TParent, List<TChild>> valueGetter,
            Action<TParent, List<TChild>> valueSetter, Func<TChild, bool> isNewGetter,
            Func<TChild, bool> isDirtyGetter, FieldPersistenceType persistenceType = FieldPersistenceType.CRUD,
            int? updateScope = null)
        {
            if (propName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(propName));

            PropName = propName.Trim();
            PersistenceType = persistenceType;
            UpdateScope = updateScope;
            ValueGetter = valueGetter ?? throw new ArgumentNullException(nameof(valueGetter));
            ValueSetter = valueSetter ?? throw new ArgumentNullException(nameof(valueSetter));
            IsNewGetter = isNewGetter ?? throw new ArgumentNullException(nameof(isNewGetter));
            IsDirtyGetter = isDirtyGetter ?? throw new ArgumentNullException(nameof(isDirtyGetter));
        }

        #region Properties

        /// <summary>
        /// a name of the the property that the child list is managed by
        /// </summary>
        public string PropName { get; }

        /// <summary>
        /// a value indicating how a field value (child entity) is persisted in the database
        /// </summary>
        public FieldPersistenceType PersistenceType { get; }

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
        private Func<TParent, List<TChild>> ValueGetter { get; }

        /// <summary>
        /// a method to set a value of the mapped field for a class instance.
        /// </summary>
        private Action<TParent, List<TChild>> ValueSetter { get; }

        /// <summary>
        /// a method to get a value indicating whether a child instance is a new entity (i.e. insert vs. update)
        /// </summary>
        private Func<TChild, bool> IsNewGetter { get; }

        /// <summary>
        /// a method to get a value indicating whether a child instance data is dirty (i.e. requires actual update)
        /// </summary>
        private Func<TChild, bool> IsDirtyGetter { get; }

        #endregion

        /// <inheritdoc cref="IChildMap{TParent}.LoadChildAsync"/>
        public async Task LoadChildAsync(TParent instance, object parentId, IOrmService service, CancellationToken ct)
        {
            if (service.IsNull()) throw new ArgumentNullException(nameof(service));
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (parentId.IsNull()) throw new ArgumentNullException(nameof(parentId));

            if (!PersistenceType.HasFlag(FieldPersistenceType.Read)) return;

            var child = await service.FetchChildEntitiesAsync<TChild>(parentId, ct);

            ValueSetter(instance, child ?? new List<TChild>());
        }

        /// <inheritdoc cref="IChildMap{TParent}.SaveChildAsync"/>
        public async Task SaveChildAsync(TParent instance, object parentId, string userId, int? scope, 
            bool scopeIsFlag, IOrmService service)
        {
            if (service.IsNull()) throw new ArgumentNullException(nameof(service));
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (parentId.IsNull()) throw new ArgumentNullException(nameof(parentId));

            var childList = ValueGetter(instance);

            foreach (var child in childList)
            {
                var isNew = IsNewGetter(child);
                var isDirty = IsDirtyGetter(child);
                if (isNew && PersistenceType.HasFlag(FieldPersistenceType.Insert))
                    await service.ExecuteInsertChildAsync(child, parentId, userId);
                else if (!isNew && isDirty && PersistenceType.HasFlag(FieldPersistenceType.Update)
                    && UpdateScope.IsInUpdateScope(scope, scopeIsFlag))
                    await service.ExecuteUpdateAsync(child, userId, scope);
            }
        }
    }
}
