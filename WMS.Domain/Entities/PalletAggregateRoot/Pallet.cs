using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.PalletAggregateRoot;

public class Pallet : BaseEntity, IAggregateRoot
{
    private Pallet() { }

    private Pallet(Guid tenantId, string palletCode)
    {
        TenantId = tenantId;
        PalletCode = palletCode;
        Status = PalletStatus.Empty;
        IsMixSku = true;
    }

    public static Pallet Create(Guid tenantId, string palletCode)
    {
        return new Pallet(tenantId, palletCode);
    }

    /// <summary>
    /// Pallet code , unique identifier for the pallet aka PLN
    /// </summary>
    public string PalletCode { get; private set; } = default!;

    public PalletMaterial? Material { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal? Length { get; private set; }
    public decimal? Width { get; private set; }
    public decimal? Height { get; private set; }
    public decimal? MaxLoadCapacity { get; private set; }
    public PalletStatus Status { get; private set; } = PalletStatus.Empty;
    public bool IsMixSku { get; private set; } = true;

    public void UpdateProperties(
        PalletMaterial? material,
        decimal? weight,
        decimal? length,
        decimal? width,
        decimal? height,
        decimal? maxLoadCapacity)
    {
        Material = material;
        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
        MaxLoadCapacity = maxLoadCapacity;
    }

    public void UpdateStatus(PalletStatus status)
    {
        Status = status;
    }

    public void UpdateMixSkuOption(bool isMixSku)
    {
        IsMixSku = isMixSku;
    }
}
