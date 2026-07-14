import  { useState, useMemo, useRef } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams, ICellRendererParams } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Eye, ArrowRight, Plus } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";
import { DEFAULT_PAGE_SIZE } from "@/constants";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { inboundService } from "../services/inbound.service";
import { ReceiptStatus, type InboundReceiptDto, INBOUND_STEPS } from "../models/inbound.model";
import { SearchOperators, type SearchObject } from "@/models/search.model";
import { useWarehouseStore } from "@/store/warehouse-store";
import { useInboundWorkflow } from "@/hooks/use-inbound-workflow";

export default function ReceiveListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const gridTheme = useAgGridTheme();
  const gridRef = useRef<AgGridReact>(null);
  const { selectedWarehouse } = useWarehouseStore();
  const { enabledSteps } = useInboundWorkflow();

  const [isLoading, setIsLoading] = useState(false);

  // If PO step is NOT enabled, we can create blind receipts
  const canCreateReceipt = !enabledSteps.includes(INBOUND_STEPS.PO);

  const columnDefs = useMemo<ColDef<InboundReceiptDto>[]>(
    () => [
      {
        field: "receiptNumber",
        headerName: t("inbound.receive.columns.receiptCode"),
        pinned: "left",
        filter: "agTextColumnFilter",
        width: 200,
        cellRenderer: (params: ICellRendererParams<InboundReceiptDto>) => {
          if (!params.value) return null;
          return <span className="font-semibold text-primary">{params.value}</span>;
        },
      },
      {
        field: "inboundOrderNumber",
        headerName: t("inbound.receive.columns.orderNumber"),
        filter: "agTextColumnFilter",
        width: 180,
        cellRenderer: (params: ICellRendererParams<InboundReceiptDto>) => {
          if (!params.value) return <span className="text-muted-foreground">-</span>;
          return <span className="font-medium text-foreground">{params.value}</span>;
        },
      },
      {
        field: "createdAt",
        headerName: t("inbound.receive.columns.createdAt"),
        width: 180,
        valueFormatter: (params) => {
          if (!params.value) return "-";
          return new Date(params.value).toLocaleString("vi-VN");
        },
      },
      {
        field: "status",
        headerName: t("inbound.receive.columns.status"),
        width: 160,
        cellRenderer: (params: ICellRendererParams<InboundReceiptDto>) => {
          if (params.value === undefined || params.value === null) return null;
          const status = params.value as ReceiptStatus;

          let customClass : string;
          let labelKey : string;

          if (status === ReceiptStatus.Receiving) {
            customClass = "bg-purple-500/10 text-purple-500 border-purple-500/20 animate-pulse";
            labelKey = "inbound.po.status.receiving";
          } else if (status === ReceiptStatus.Completed) {
            customClass = "bg-emerald-500/10 text-emerald-500 border-emerald-500/20";
            labelKey = "inbound.po.status.completed";
          } else {
            customClass = "bg-slate-500/10 text-slate-500 border-slate-500/20";
            labelKey = "inbound.po.status.unknown";
          }

          return (
            <div className="flex items-center h-full">
              <Badge variant="outline" className={customClass}>
                {t(labelKey)}
              </Badge>
            </div>
          );
        },
      },
      {
        headerName: t("inbound.receive.columns.actions"),
        pinned: "right",
        width: 140,
        sortable: false,
        filter: false,
        resizable: false,
        cellRenderer: (params: ICellRendererParams<InboundReceiptDto>) => {
          if (!params.data) return null;
          const receipt = params.data as InboundReceiptDto;
          const isDraft = receipt.status === ReceiptStatus.Receiving;

          return (
            <div className="flex items-center gap-1.5 h-full">
              {isDraft ? (
                <Button
                  variant="default"
                  size="xs"
                  className="h-7 text-[10px] font-semibold cursor-pointer"
                  onClick={() => navigate(`/inbound/receive/${receipt.id}`)}
                >
                  <ArrowRight className="size-3 mr-1" />
                  {t("inbound.receive.actions.continue")}
                </Button>
              ) : (
                <Button
                  variant="ghost"
                  size="xs"
                  className="h-7 text-[10px] font-semibold cursor-pointer text-muted-foreground"
                  onClick={() => navigate(`/inbound/receive/${receipt.id}`)}
                >
                  <Eye className="size-3 mr-1" />
                  {t("inbound.receive.actions.view")}
                </Button>
              )}
            </div>
          );
        },
      },
    ],
    [t, navigate]
  );

  const datasource = useMemo<IDatasource>(() => {
    return {
      getRows: async (params: IGetRowsParams) => {
        try {
          if (!selectedWarehouse?.id) {
            params.successCallback([], 0);
            return;
          }
          setIsLoading(true);
          const page = Math.floor(params.startRow / DEFAULT_PAGE_SIZE) + 1;

          const gridFilters: SearchObject[] = Object.keys(params.filterModel || {}).map((key) => {
            const gridFilter = params.filterModel[key];
         
            // eslint-disable-next-line react-hooks/refs
            const column = gridRef.current?.api?.getColumn(key);
            
            const colDef = column?.getColDef();
            const label = colDef?.headerName ? String(colDef.headerName) : key;
            const type = gridFilter.filterType || "text";

            let operator: SearchOperators = SearchOperators.Equal;
            if (gridFilter.type === "contains") {
              operator = SearchOperators.Contains;
            } else if (gridFilter.type === "greaterThan") {
              operator = SearchOperators.GreaterThan;
            } else if (gridFilter.type === "greaterThanOrEqual") {
              operator = SearchOperators.GreaterThanOrEqual;
            } else if (gridFilter.type === "lessThan") {
              operator = SearchOperators.LessThan;
            } else if (gridFilter.type === "lessThanOrEqual") {
              operator = SearchOperators.LessThanOrEqual;
            }

            return {
              name: key,
              operator,
              text: gridFilter.filter?.toString() || "",
              value: gridFilter.filter,
              label,
              type,
            };
          });

          const warehouseFilter: SearchObject = {
            name: "WarehouseId",
            operator: SearchOperators.Equal,
            value: selectedWarehouse.id,
          };

          const criteria = [warehouseFilter, ...gridFilters];

          const response = await inboundService.searchInboundReceipts({
            searchObjects: criteria,
            page,
            limit: DEFAULT_PAGE_SIZE,
          });
          setIsLoading(false);

          if (response.success && response.data) {
            const { items, totalCount } = response.data;
            params.successCallback(items, totalCount);
          } else {
            params.failCallback();
          }
        } catch (error) {
          console.error("Failed to load inbound receipts:", error);
          setIsLoading(false);
          params.failCallback();
        }
      },
    };
  }, [selectedWarehouse?.id]);

  return (
    <div className="h-full w-full flex flex-col overflow-hidden bg-card border rounded-xl shadow-sm">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 border-b bg-secondary/10">
        <div>
          <h2 className="text-lg font-bold text-foreground">{t("inbound.dashboard.receiving", "Receiving")}</h2>
          <p className="text-xs text-muted-foreground mt-0.5">
            {t("inbound.dashboard.receivingDescription", "Process dock deliveries")}
          </p>
        </div>
        {canCreateReceipt && (
          <Link to="/inbound/receive/create">
            <Button variant="default" size="sm" className="h-8 text-xs font-semibold cursor-pointer">
              <Plus className="size-3.5 mr-1" />
              {t("inbound.receive.actions.create", "Create Receipt")}
            </Button>
          </Link>
        )}
      </div>

      <div className="flex-1 min-h-0 w-full overflow-hidden relative p-4">
        <div className="w-full h-full">
          <AgGridReact
            ref={gridRef}
            columnDefs={columnDefs}
            datasource={datasource}
            rowModelType="infinite"
            cacheBlockSize={DEFAULT_PAGE_SIZE}
            maxConcurrentDatasourceRequests={1}
            infiniteInitialRowCount={DEFAULT_PAGE_SIZE}
            pagination={true}
            paginationPageSize={DEFAULT_PAGE_SIZE}
            paginationPageSizeSelector={false}
            theme={gridTheme}
            loading={isLoading}
            overlayLoadingTemplate={`<span class="ag-overlay-loading-center">${t("common.loading")}</span>`}
            overlayNoRowsTemplate={`<span class="ag-overlay-no-rows-center">${t("common.noData")}</span>`}
            defaultColDef={{
              resizable: true,
              sortable: true,
              filter: true,
              minWidth: 100,
            }}
            onRowDoubleClicked={(e) => {
              if (e.data?.id) {
                navigate(`/inbound/receive/${e.data.id}`);
              }
            }}
          />
        </div>
      </div>
    </div>
  );
}
