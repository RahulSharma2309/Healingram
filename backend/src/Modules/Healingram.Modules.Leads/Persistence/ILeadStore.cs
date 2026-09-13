using Healingram.Modules.Leads.Application;

namespace Healingram.Modules.Leads.Persistence;

internal interface ILeadStore
{
    Task InsertAsync(LeadEntity entity, CancellationToken cancellationToken);
    Task<LeadEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
}
