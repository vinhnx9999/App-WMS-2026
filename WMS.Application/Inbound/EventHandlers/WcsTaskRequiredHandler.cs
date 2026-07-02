using MediatR;
using WMS.Application.Common.Service;
using WMS.Domain.Entities.WcsIntegration;
using WMS.Domain.Enums;
using WMS.Domain.Events;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Handlers;

public class WcsTaskRequiredHandler(
    IUnitOfWork uow,
    ISequenceCodeGenerator sequenceCodeGenerator)
    : INotificationHandler<WcsTaskRequiredEvent>
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ISequenceCodeGenerator _seq = sequenceCodeGenerator;

    public async Task Handle(WcsTaskRequiredEvent notification, CancellationToken ct)
    {
        if (notification.Items.Count == 0) return;

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var groups = notification.Items
                .GroupBy(item => item.WcsBlockId);

            foreach (var group in groups)
            {
                var wcsBlockId = group.Key;
                var taskNumber = await _seq.NextAsync(
                    notification.TenantId,
                    CodeSequenceTypes.WcsInboundTask, ct);

                var wcsTask = new WcsTask(
                    notification.TenantId,
                    notification.WarehouseId,
                    notification.PutawayTaskId,
                    wcsBlockId,
                    taskNumber,
                    WcsTaskTypes.Inbound);

                foreach (var item in group)
                {
                    var subTask = new WcsSubTask(
                        notification.TenantId,
                        wcsTask.Id,
                        item.PalletCode,
                        item.ToLocationCode);

                    wcsTask.AddSubTask(subTask);
                }

                await _uow.Repository<WcsTask>().AddAsync(wcsTask, ct);
            }

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}
