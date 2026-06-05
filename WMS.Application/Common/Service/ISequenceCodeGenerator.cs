namespace WMS.Application.Common.Service
{
    public interface ISequenceCodeGenerator
    {
        Task<string> NextAsync(
           Guid tenantId,
           string codeType,
           CancellationToken ct = default);
    }
}
