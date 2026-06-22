using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Common.Service
{
    public class SequenceCodeGenerator(ICodeSequenceRepository codeSequenceRepository) : ISequenceCodeGenerator
    {
        private readonly ICodeSequenceRepository _codeSequenceRepository = codeSequenceRepository;

        public async Task<string> NextAsync(Guid tenantId, string codeType, CancellationToken ct = default)
        {
            if (tenantId == Guid.Empty)
                throw new AppException(400, "In_Valid", "TenantId is required.");

            var sequence = await _codeSequenceRepository.GetByCodeTypeAsync(tenantId, codeType, ct);

            if (sequence is null)
            {
                sequence = new CodeSequence(tenantId, codeType.ToUpperInvariant(), codeType);
                await _codeSequenceRepository.AddAsync(sequence, ct);
            }

            return sequence.Next();
        }
    }
}

