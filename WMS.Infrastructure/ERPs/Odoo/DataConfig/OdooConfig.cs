using System;
using System.Collections.Generic;
using System.Text;

namespace WMS.Infrastructure.ERPs.Odoo.DataConfig;

public class OdooConfig
{
    public string BaseUrl { get; set; } = "";
    public string Database { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 2000;
    public OdooMappingConfig Mapping { get; set; } = new();
    public OdooSyncConfig Sync { get; set; } = new();
}
