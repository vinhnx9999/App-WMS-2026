namespace WMS.Domain.Services;

/*
public class InventoryDomainService
{
    public void ValidateLotExpiryConsistency(
        string lotNumber,
        DateTime? expiryDate,
        IEnumerable<(string LotNumber, DateTime? ExpiryDate)> existingLots)
    {
        if (string.IsNullOrEmpty(lotNumber)) return;

        foreach (var existing in existingLots)
        {
            if (existing.LotNumber == lotNumber && existing.ExpiryDate != expiryDate)
            {
                throw new DomainException($"Lot {lotNumber} must have a consistent Expiry Date: {existing.ExpiryDate:yyyy-MM-dd}.");
            }
        }
    }
}
*/
