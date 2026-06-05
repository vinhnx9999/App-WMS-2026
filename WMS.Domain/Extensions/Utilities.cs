namespace WMS.Domain.Extensions
{
    public static class Utilities
    {
        public static string NormalizeSkuCode(string skuCode)
        {
            return skuCode.Trim().ToUpperInvariant();
        }

        public static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

    }
}
