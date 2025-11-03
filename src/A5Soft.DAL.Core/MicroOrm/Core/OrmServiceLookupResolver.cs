using System;
using System.Threading;
using System.Threading.Tasks;

namespace A5Soft.DAL.Core.MicroOrm.Core
{
    /// <summary>
    /// Wrapper for <see cref="ILookupResolver"/> and <see cref="IOrmService"/> to simplify calls.
    /// </summary>
    public class OrmServiceLookupResolver
    {
        private readonly IOrmService _service;
        private readonly ILookupResolver _resolver;

        public OrmServiceLookupResolver(IOrmService service, ILookupResolver resolver)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _resolver = resolver;
        }


        public async Task<TLookup> FetchLookupAsync<TLookup>(uint? id, CancellationToken ct = default)
            where TLookup : class
        {
            if (null == _resolver) throw new InvalidOperationException("Lookup resolver is not injected.");

            if (!id.HasValue) return null;

            return await _resolver.FetchLookupAsync<TLookup>(_service, id.Value, ct);
        }

        public async Task<TLookup> FetchLookupAsync<TLookup>(ulong? id, CancellationToken ct = default)
            where TLookup : class
        {
            if (null == _resolver) throw new InvalidOperationException("Lookup resolver is not injected.");

            if (!id.HasValue) return null;

            return await _resolver.FetchLookupAsync<TLookup>(_service, id.Value, ct);
        }

        public async Task<TLookup> FetchLookupAsync<TLookup>(Guid? id, CancellationToken ct = default)
            where TLookup : class
        {
            if (null == _resolver) throw new InvalidOperationException("Lookup resolver is not injected.");

            if (!id.HasValue) return null;

            return await _resolver.FetchLookupAsync<TLookup>(_service, id.Value, ct);
        }

        public async Task<TLookup> FetchLookupAsync<TLookup>(string id, CancellationToken ct = default)
            where TLookup : class
        {
            if (null == _resolver) throw new InvalidOperationException("Lookup resolver is not injected.");

            if (string.IsNullOrWhiteSpace(id)) return null;

            return await _resolver.FetchLookupAsync<TLookup>(_service, id, ct);
        }
    }
}
