using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Tenantry.Pro.EfCore;

namespace Tenantry.Pro.Samples.AuditLogging;

/// <summary>
/// A custom <see cref="IAuditStore"/> that keeps audit entries in memory so the sample can show them
/// over an HTTP endpoint. A real implementation would persist entries to a database, an event stream,
/// or an external audit service. Register it as a singleton AFTER pro.AddAuditLogging() to replace the
/// default logging store.
/// </summary>
public sealed class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();

    public IReadOnlyCollection<AuditEntry> Entries => _entries;

    public Task SaveAsync(DbContext context, IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
            _entries.Enqueue(entry);

        return Task.CompletedTask;
    }
}
