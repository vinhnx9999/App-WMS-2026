using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Repositories
{
    public class CodeSequenceRepository(WmsDbContext wmsDbContext) : ICodeSequenceRepository
    {
        private readonly WmsDbContext _wmsDbContext = wmsDbContext;

        public async Task AddAsync(CodeSequence codeSequence, CancellationToken ct = default)
        {
            await _wmsDbContext.CodeSequences.AddAsync(codeSequence, ct);
        }

        public Task<CodeSequence?> GetByCodeTypeAsync(Guid tenantId, string codeType, CancellationToken ct = default)
        {


            return _wmsDbContext.CodeSequences
                .SingleOrDefaultAsync(
                    x => x.TenantId == tenantId &&
                         x.CodeType == codeType,
                    ct);
        }
    }
}
