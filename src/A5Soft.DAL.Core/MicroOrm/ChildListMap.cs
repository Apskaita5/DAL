using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// represents a child class list mapper, i.e. a description of how a business object's child objects list
    /// is persisted in a database;
    /// instances per field type should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="T">type of parent class</typeparam>
    /// <typeparam name="C">type of child class</typeparam>
    public sealed class ChildListMap<T, C> where T : class where C : class
    {

        #region Properties

        /// <summary>
        /// a name of the the property that the child list is managed by
        /// </summary>
        public string PropName { get; }

        /// <summary>
        /// a value indicating how a child list is persisted in the database
        /// </summary>
        public FieldPersistenceType PersistenceType { get; }
         
        /// <summary>
        /// an update scope that updates the property value in database
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.
        /// If multiple scope combinations are used, the enum should be defined as [Flags].
        /// in that case: (a) a field should only be assigned to a single scope;
        /// (b) a bitwise check is used: (fieldScope & requestedScope) != 0;
        /// (c) <see cref="OrmIdentityMap{T}.ScopeIsFlag"/> should be set to true.
        /// </summary>
        public int? UpdateScope { get; }

        /// <summary>
        /// a method to get a value of the mapped field for a class instance.
        /// </summary>
        private Func<T, List<C>> ValueGetter { get; }

        /// <summary>
        /// a method to set a value of the mapped field for a class instance.
        /// </summary>
        private Action<T, List<C>> ValueSetter { get; }

        /// <summary>
        /// a method to get a value indicating whether a child instance is a new entity (i.e. insert vs. update) 
        /// </summary>
        private Func<C, bool> IsNewGetter { get; }

        #endregion

        internal async Task LoadChildrenAsync(T instance, object parentId, IOrmService service, CancellationToken ct)
        {
            if (service.IsNull()) throw new ArgumentNullException(nameof(service));
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (parentId.IsNull()) throw new ArgumentNullException(nameof(parentId));
                                     
            var child = await service.FetchChildEntitiesAsync<C>(parentId, ct);

            ValueSetter(instance, child ?? new List<C>());
        }

        internal async Task SaveChildrenAsync(T instance, object parentId, string userId, int? scope, IOrmService service)
        {
            if (service.IsNull()) throw new ArgumentNullException(nameof(service));
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (parentId.IsNull()) throw new ArgumentNullException(nameof(parentId));

            var childList = ValueGetter(instance);

            foreach (var child in childList)
            {
                if (IsNewGetter(child)) await service.ExecuteInsertChildAsync(child, parentId, userId);
                else await service.ExecuteUpdateAsync(child, userId, scope);
            }
        }

    }
}
