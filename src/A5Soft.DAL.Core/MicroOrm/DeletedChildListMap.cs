using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using A5Soft.DAL.Core.MicroOrm.Core;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// represents a deleted child list mapper, i.e. a description of how a business object's deleted child objects
    /// are persisted in a database;
    /// instances per field type should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="TParent">type of parent class</typeparam>
    /// <typeparam name="TChild">type of child class</typeparam>
    public sealed class DeletedChildListMap<TParent, TChild> : IChildMap<TParent>
        where TParent : class where TChild : class
    {

        /// <summary>
        /// Creates a new instance of deleted child list field map.
        /// </summary>
        /// <param name="propName">a name of the the property that the child list is managed by</param>
        /// <param name="valueGetter">a method to get a value of the mapped field for a class instance</param>
        /// <param name="updateScope">an update scope that updates the property value (child entity) in database
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.
        /// If multiple scope combinations are used, the enum should be defined as [Flags].
        /// in that case: (a) a field should only be assigned to a single scope;
        /// (b) a bitwise check is used: (fieldScope & requestedScope) != 0;
        /// (c) <see cref="OrmIdentityMapBase{T}.ScopeIsFlag"/> should be set to true.</param>
        public DeletedChildListMap(string propName, Func<TParent, List<TChild>> valueGetter,
            int? updateScope = null)
        {
            if (propName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(propName));

            PropName = propName.Trim();
            UpdateScope = updateScope;
            ValueGetter = valueGetter ?? throw new ArgumentNullException(nameof(valueGetter));
        }

        #region Properties

        /// <summary>
        /// a name of the the property that the deleted child list is managed by
        /// </summary>
        public string PropName { get; }
        
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
        
        #endregion

        /// <inheritdoc cref="IChildMap{TParent}.LoadChildAsync"/>
        public Task LoadChildAsync(TParent instance, object parentId, IOrmService service, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IChildMap{TParent}.SaveChildAsync"/>
        public async Task SaveChildAsync(TParent instance, object parentId, string userId, int? scope,
            bool scopeIsFlag, IOrmService service)
        {
            if (service.IsNull()) throw new ArgumentNullException(nameof(service));
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));

            if (!UpdateScope.IsInUpdateScope(scope, scopeIsFlag)) return;

            var childList = ValueGetter(instance) ?? new List<TChild>();

            foreach (var child in childList)
            {
                _ = await service.ExecuteDeleteAsync(child);
            }

            childList.Clear();
        }

    }
}
