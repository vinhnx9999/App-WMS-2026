using WMS.Domain.Entities;

namespace WMS.Domain.Interfaces
{
    public interface ICodeSequenceRepository
    {
        Task<CodeSequence?> GetByCodeTypeAsync(
        Guid tenantId,
        string codeType,
        CancellationToken ct = default);

        Task AddAsync(
            CodeSequence codeSequence,
            CancellationToken ct = default);
    }
}
