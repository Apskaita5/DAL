using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.MicroOrm.Core
{
    /// <summary>
    /// base interface for child field mappers
    /// </summary>
    /// <typeparam name="TParent">a type of parent entity</typeparam>
    public interface IChildMap<in TParent> where TParent : class
    {

        /// <summary>
        /// Loads child field data from a database.
        /// </summary>
        /// <param name="instance">an instance of parent entity</param>
        /// <param name="parentId">a primary key (value) of the parent entity</param>
        /// <param name="service">a MicroOrm service instance to use for loading child data</param>
        /// <param name="ct">a cancellation token</param>
        Task LoadChildAsync(TParent instance, object parentId, IOrmService service, CancellationToken ct = default);

        /// <summary>
        /// Saves (inserts, updates or deletes) child field (child entity) data to a database.
        /// </summary>
        /// <param name="instance">an instance of parent entity</param>
        /// <param name="parentId">a primary key (value) of the parent entity</param>
        /// <param name="userId">a user identifier (e.g. email) for audit field UpdatedBy 
        /// (only applicable if the child entity implements standard audit fields)</param>
        /// <param name="scope">a scope of the update operation; a business objects can define
        /// different update scopes (different collections of properties) as an ENUM
        /// which nicely converts into int.</param>
        /// <param name="scopeIsFlag">whether the scope is a (enum) bit flag</param>
        /// <param name="service">a MicroOrm service instance to use for saving child data</param>
        Task SaveChildAsync(TParent instance, object parentId, string userId, int? scope, bool scopeIsFlag, IOrmService service);

    }
}
