namespace WMS.Domain.Extensions
{
    public static class Utilities
    {
        public static string NormalizeCode(string code)
        {
            return code.Trim().ToUpperInvariant();
        }

        public static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

    }
}
