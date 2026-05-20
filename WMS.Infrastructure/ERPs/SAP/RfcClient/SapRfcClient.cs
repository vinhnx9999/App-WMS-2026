namespace WMS.Infrastructure.ERPs.SAP.RfcClient;

using global::SAP.Middleware.Connector;

// Requires: SAP.Connector NuGet package (sapnco)
// Or: SAP.Middleware.Connector for .NET

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Infrastructure.ERPs.SAP.DataConfig;
using WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsIssue;
using WMS.Infrastructure.ERPs.SAP.RfcClient.DTOs.GoodsReceipt;

public class SapRfcClient : ISapRfcClient
{
    private readonly SapConnectionConfig _config;
    private readonly ILogger<SapRfcClient> _log;

    public SapRfcClient(
        IOptions<SapConfig> config,
        ILogger<SapRfcClient> log)
    {
        _config = config.Value.Connection;
        _log = log;

        // Configure SAP NCo destination
        var destParams = new RfcConfigParameters
        {
            { RfcConfigParameters.AppServerHost, _config.AppServerHost },
            { RfcConfigParameters.SystemNumber, _config.SystemNumber },
            { RfcConfigParameters.SystemID, _config.SystemId },
            { RfcConfigParameters.Client, _config.Client },
            { RfcConfigParameters.User, _config.Username },
            { RfcConfigParameters.Password, _config.Password },
            { RfcConfigParameters.Language, _config.Language },
            { RfcConfigParameters.PoolSize, _config.PoolSize.ToString() },
            { RfcConfigParameters.MaxPoolSize, _config.PeakConnectionsLimit.ToString() },
            //{ RfcConfigParameters.PeakConnectionsLimit, _config.PeakConnectionsLimit.ToString() },
        };

        SapDestinationConfig destinationConfig = new(destParams);
        RfcDestinationManager.RegisterDestinationConfiguration(destinationConfig);
    }

    public async Task<SapGrResult> PostGoodsReceipt(
        SapGrRequest request, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var dest = RfcDestinationManager.GetDestination("WMS_SAP");
            var repo = dest.Repository;

            // Call BAPI_GOODSMVT_CREATE
            var func = repo.CreateFunction("BAPI_GOODSMVT_CREATE");

            // Set header
            var header = func.GetStructure("GOODSMVT_HEADER");
            header.SetValue("PSTNG_DATE", request.PostingDate.ToString("yyyyMMdd"));
            header.SetValue("DOC_DATE", request.DocumentDate.ToString("yyyyMMdd"));
            header.SetValue("REF_DOC_NO", request.ReferenceDoc);
            header.SetValue("HEADER_TXT", request.HeaderText ?? "");

            // Set items
            var items = func.GetTable("GOODSMVT_ITEM");
            foreach (var item in request.Items)
            {
                items.Append();
                items.SetValue("MATERIAL", item.Material.PadLeft(18, '0'));
                items.SetValue("PLANT", item.Plant);
                items.SetValue("STGE_LOC", item.StorageLocation);
                items.SetValue("ENTRY_QNT", item.Quantity);
                items.SetValue("ENTRY_UOM", item.UnitOfMeasure);
                items.SetValue("MOVE_TYPE", item.MoveType);
                items.SetValue("MVT_IND", item.MovementIndicator ?? "B");
                if (!string.IsNullOrEmpty(item.PurchaseOrder))
                {
                    items.SetValue("PO_NUMBER", item.PurchaseOrder);
                    items.SetValue("PO_ITEM", item.PoItem ?? "00010");
                }
            }

            // Execute
            func.Invoke(dest);

            // Read result
            var matDoc = func.GetString("MATERIALDOCUMENT");
            var matDocYear = func.GetString("MATDOCUMENTYEAR");
            var returnTable = func.GetTable("RETURN");

            var messages = new List<string>();
            var hasError = false;

            for (int i = 0; i < returnTable.RowCount; i++)
            {
                returnTable.CurrentIndex = i;
                var type = returnTable.GetString("TYPE");
                var msg = returnTable.GetString("MESSAGE");
                messages.Add($"[{type}] {msg}");
                if (type == "E" || type == "A") hasError = true;
            }

            // Commit if success
            if (!hasError)
            {
                var commit = repo.CreateFunction("BAPI_TRANSACTION_COMMIT");
                commit.SetValue("WAIT", "X");
                commit.Invoke(dest);
            }

            return new SapGrResult
            {
                Success = !hasError,
                MaterialDocument = matDoc,
                MaterialDocumentYear = matDocYear,
                Messages = messages
            };
        }, ct);
    }

    public async Task<SapGiResult> PostGoodsIssue(
        SapGiRequest request, CancellationToken ct)
    {
        // Similar to PostGoodsReceipt but with GI move types
        // Move type 201/261/601 depending on scenario
        return await Task.Run(() =>
        {
            var dest = RfcDestinationManager.GetDestination("WMS_SAP");
            var repo = dest.Repository;
            var func = repo.CreateFunction("BAPI_GOODSMVT_CREATE");

            // Header
            var header = func.GetStructure("GOODSMVT_HEADER");
            header.SetValue("PSTNG_DATE", request.PostingDate.ToString("yyyyMMdd"));
            header.SetValue("DOC_DATE", request.DocumentDate.ToString("yyyyMMdd"));
            header.SetValue("REF_DOC_NO", request.ReferenceDoc);

            // Items
            var items = func.GetTable("GOODSMVT_ITEM");
            foreach (var item in request.Items)
            {
                items.Append();
                items.SetValue("MATERIAL", item.Material.PadLeft(18, '0'));
                items.SetValue("PLANT", item.Plant);
                items.SetValue("STGE_LOC", item.StorageLocation);
                items.SetValue("ENTRY_QNT", item.Quantity);
                items.SetValue("ENTRY_UOM", item.UnitOfMeasure);
                items.SetValue("MOVE_TYPE", item.MoveType);
            }

            func.Invoke(dest);

            var matDoc = func.GetString("MATERIALDOCUMENT");
            var returnTable = func.GetTable("RETURN");
            var hasError = false;
            var messages = new List<string>();

            for (int i = 0; i < returnTable.RowCount; i++)
            {
                returnTable.CurrentIndex = i;
                messages.Add(returnTable.GetString("MESSAGE"));
                if (returnTable.GetString("TYPE") == "E") hasError = true;
            }

            if (!hasError)
            {
                var commit = repo.CreateFunction("BAPI_TRANSACTION_COMMIT");
                commit.SetValue("WAIT", "X");
                commit.Invoke(dest);
            }

            return new SapGiResult
            {
                Success = !hasError,
                MaterialDocument = matDoc,
                Messages = messages
            };
        }, ct);
    }

    public async Task<decimal> GetStockAsync(
        string material, string plant, string sloc,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var dest = RfcDestinationManager.GetDestination("WMS_SAP");
            var func = dest.Repository.CreateFunction("BAPI_MATERIAL_STOCK_GET_RL");

            func.SetValue("MATERIAL", material.PadLeft(18, '0'));
            func.SetValue("PLANT", plant);
            func.SetValue("STGE_LOC", sloc);

            func.Invoke(dest);

            var stockTable = func.GetTable("STOCK_LIST");
            decimal total = 0;

            for (int i = 0; i < stockTable.RowCount; i++)
            {
                stockTable.CurrentIndex = i;
                total += stockTable.GetDecimal("MATL_WHSE_STOCK");
            }

            return total;
        }, ct);
    }
}

public class SapDestinationConfig(RfcConfigParameters parameters) : IDestinationConfiguration
{
    private RfcConfigParameters _parameters = parameters;

    // Return parameters for the requested destination
    public RfcConfigParameters GetParameters(string destinationName)
    {
        if ("MY_SAP_SYSTEM".Equals(destinationName))
        {
            RfcConfigParameters parms = new()
            {
                { RfcConfigParameters.AppServerHost, "192.168.x.x" }, // SAP Server IP
                { RfcConfigParameters.SystemNumber, "00" },            // System Number
                { RfcConfigParameters.User, "YOUR_USERNAME" },
                { RfcConfigParameters.Password, "YOUR_PASSWORD" },
                { RfcConfigParameters.Client, "800" },                // SAP Client
                { RfcConfigParameters.Language, "EN" },
                { RfcConfigParameters.PoolSize, "5" },
                { RfcConfigParameters.MaxPoolSize, "10" }
            };

            return parms;
        }

        return null; // Return null if destination is unknown
    }

    // Indicates if the configuration supports change events
    public bool ChangeEventsSupported()
    {
        return false;
    }

    // Event required by the interface (leave empty if ChangeEventsSupported is false)
    public event RfcDestinationManager.ConfigurationChangeHandler ConfigurationChanged;
}