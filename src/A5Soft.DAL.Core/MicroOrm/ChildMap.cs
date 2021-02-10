using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// represents a child class mapper, i.e. a description of how a business object's child object
    /// is persisted in a database;
    /// instances per field type should be added to a business type as a static field
    /// </summary>
    /// <typeparam name="T">type of parent class</typeparam>
    /// <typeparam name="C">type of child class</typeparam>
    public sealed class ChildMap<T, C> where T : class where C : class
    {

        #region Properties
        
        /// <summary>
        /// a name of the the property that the child value is managed by
        /// </summary>
        public string PropName { get; }
           
        /// <summary>
        /// a value indicating how a child value is persisted in the database
        /// </summary>
        public FieldPersistenceType PersistenceType { get; }

        /// <summary>
        /// whether the child could be null
        /// </summary>
        public bool AllowNull { get; }

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
        private Func<T, C> ValueGetter { get; }

        /// <summary>
        /// a method to set a value of the mapped field for a class instance.
        /// </summary>
        private Action<T, C> ValueSetter { get; }

        /// <summary>
        /// a method to get a value indicating whether a child instance is a new entity (i.e. insert vs. update) 
        /// </summary>
        private Func<C, bool> IsNewGetter { get; }

        #endregion

        internal async Task LoadChildAsync(T instance, object parentId, IOrmService service, CancellationToken ct)
        {
            if (service.IsNull()) throw new ArgumentNullException(nameof(service));
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (!AllowNull && parentId.IsNull()) throw new ArgumentNullException(nameof(parentId));
                                                                                                               
            if (parentId.IsNull())
            {
                ValueSetter(instance, null);
                return;
            }

            var child = await service.FetchChildEntitiesAsync<C>(parentId, ct);

            if (child.Count > 0)
            {
                if (child.Count > 1) throw new InvalidOperationException(
                    $"Fetched multiple ({child.Count}) children of type {typeof(C).FullName} for parent of type {typeof(T).FullName} using parent id - {parentId}.");
                
                ValueSetter(instance, child[0]);
            }
            else
            {
                if (!AllowNull) throw new InvalidOperationException(
                    $"Failed to fetch a child of type {typeof(C).FullName} for parent of type {typeof(T).FullName} using parent id - {parentId}.");

                ValueSetter(instance, null);
            }
        }

        internal async Task SaveChildAsync(T instance, object parentId, string userId, int? scope, IOrmService service)
        {
            if (service.IsNull()) throw new ArgumentNullException(nameof(service));
            if (instance.IsNull()) throw new ArgumentNullException(nameof(instance));
            if (!AllowNull && parentId.IsNull()) throw new ArgumentNullException(nameof(parentId));

            var childInstance = ValueGetter(instance);
            var isNew = IsNewGetter(childInstance);

            if (isNew) await service.ExecuteInsertChildAsync(childInstance, parentId, userId);
            else await service.ExecuteUpdateAsync(childInstance, userId, scope);
        }

    }
}
