using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// A lookup service for ORM sevice injection.
    /// </summary>
    public interface ILookupResolver
    {
        /// <summary>
        /// Fetches a lookups of type <typeparamref name="TLookup"/>.
        /// </summary>
        /// <param name="service">ORM service to use for fetching lookup list</param>
        /// <param name="ct">cancellation token (if any)</param>
        /// <returns>lookups of type <typeparamref name="TLookup"/></returns>
        Task<IEnumerable<TLookup>> FetchLookupsAsync<TLookup>(IOrmService service, CancellationToken ct = default)
            where TLookup : class;

        /// <summary>
        /// Fetches a lookup instance (of type TLookup), which is identified by the id from the database.
        /// </summary>
        /// <param name="service">ORM service to use for fetching lookup list</param>
        /// <param name="id">identifier of the lookup to fetch</param>
        /// <param name="ct">An optional CancellationToken</param>
        /// <typeparam name="TLookup">The type of lookup to fetch, which must be a class</typeparam>
        /// <returns>a lookup identified by <paramref name="id"/> or null if no such lookup</returns>
        Task<TLookup> FetchLookupAsync<TLookup>(IOrmService service, Guid id, CancellationToken ct = default)
                    where TLookup : class;

        /// <summary>
        /// Fetches a lookup instance (of type TLookup), which is identified by the id from the database.
        /// </summary>
        /// <param name="service">ORM service to use for fetching lookup list</param>
        /// <param name="id">identifier of the lookup to fetch</param>
        /// <param name="ct">An optional CancellationToken</param>
        /// <typeparam name="TLookup">The type of lookup to fetch, which must be a class</typeparam>
        /// <returns>a lookup identified by <paramref name="id"/> or null if no such lookup</returns>
        Task<TLookup> FetchLookupAsync<TLookup>(IOrmService service, uint id, CancellationToken ct = default)
            where TLookup : class;

        /// <summary>
        /// Fetches a lookup instance (of type TLookup), which is identified by the id from the database.
        /// </summary>
        /// <param name="service">ORM service to use for fetching lookup list</param>
        /// <param name="id">identifier of the lookup to fetch</param>
        /// <param name="ct">An optional CancellationToken</param>
        /// <typeparam name="TLookup">The type of lookup to fetch, which must be a class</typeparam>
        /// <returns>a lookup identified by <paramref name="id"/> or null if no such lookup</returns>
        Task<TLookup> FetchLookupAsync<TLookup>(IOrmService service, ulong id, CancellationToken ct = default)
            where TLookup : class;

        /// <summary>
        /// Fetches a lookup instance (of type TLookup), which is identified by the id from the database.
        /// </summary>
        /// <param name="service">ORM service to use for fetching lookup list</param>
        /// <param name="id">identifier of the lookup to fetch</param>
        /// <param name="ct">An optional CancellationToken</param>
        /// <typeparam name="TLookup">The type of lookup to fetch, which must be a class</typeparam>
        /// <returns>a lookup identified by <paramref name="id"/> or null if no such lookup</returns>
        Task<TLookup> FetchLookupAsync<TLookup>(IOrmService service, string id, CancellationToken ct = default)
            where TLookup : class;
    }
}
