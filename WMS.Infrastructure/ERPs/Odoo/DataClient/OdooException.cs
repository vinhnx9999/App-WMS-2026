namespace WMS.Infrastructure.ERPs.Odoo.DataClient;

// ── Exceptions ──

public class OdooException(string message) : Exception(message)
{
}

public class OdooSessionExpiredException : OdooException
{
    public OdooSessionExpiredException(): base("Odoo session expired") { }
}