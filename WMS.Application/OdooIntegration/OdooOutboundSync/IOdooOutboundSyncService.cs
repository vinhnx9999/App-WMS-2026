using System;
using System.Collections.Generic;
using System.Text;

namespace WMS.Application.OdooIntegration.OdooOutboundSync;

public interface IOdooOutboundSyncService
{
    Task<int> SyncOutboundDeliveriesAsync(CancellationToken ct = default);
    Task ConfirmDeliveryAsync(Guid wmsOrderId,
        CancellationToken ct = default);
}
