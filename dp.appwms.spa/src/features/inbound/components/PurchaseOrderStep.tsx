import React, { useState, useMemo, useRef } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Eye, Layers } from "lucide-react";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";
import { DEFAULT_PAGE_SIZE } from "@/constants";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { inboundService } from "../services/inbound.service";
import { InboundStatus, type InboundOrderDto } from "../models/inbound.model";
import { PurchaseOrderDetailSheet } from "./PurchaseOrderDetailSheet";
import { SearchOperators, type SearchObject } from "@/models/search.model";

interface PurchaseOrderStepProps {
  onNext: () => void;
  onSelectOrder: (order: InboundOrderDto) => void;
}

export const PurchaseOrderStep: React.FC<PurchaseOrderStepProps> = ({
  onSelectOrder,
}) => {
  const { t } = useTranslation();
  const gridTheme = useAgGridTheme();
  const gridRef = useRef<AgGridReact>(null);

  // Loading and Selection States
  const [isLoading, setIsLoading] = useState(false);
  const [isSheetOpen, setIsSheetOpen] = useState(false);
  const [viewingOrder, setViewingOrder] = useState<InboundOrderDto | null>(null);

  // Column definitions for AG Grid
  const columnDefs = useMemo<ColDef<InboundOrderDto>[]>(
    () => [
      {
        field: "orderNumber",
        headerName: t("inbound.po.columns.orderNumber"),
        pinned: "left",
        filter: "agTextColumnFilter",
        width: 180,
        cellRenderer: (params: any) => {
          if (!params.value) return null;
          return <span className="font-semibold text-primary">{params.value}</span>;
        },
      },
      {
        field: "expectedDate",
        headerName: t("inbound.po.columns.expectedDate"),
        width: 150,
        valueFormatter: (params) => {
          if (!params.value) return "-";
          return new Date(params.value).toLocaleDateString("vi-VN");
        },
      },
      {
        field: "totalValue",
        headerName: t("inbound.po.columns.totalValue"),
        width: 160,
        valueFormatter: (params) => {
          if (params.value === undefined || params.value === null) return "0";
          return params.value.toLocaleString("vi-VN") + " VND";
        },

      },
      {
        field: "itemsCount",
        headerName: t("inbound.po.columns.itemsCount"),
        width: 120,
        type: "numericColumn",
      },
      {
        field: "status",
        headerName: t("inbound.po.columns.status"),
        flex: 1,
        minWidth: 150,
        cellRenderer: (params: any) => {
          if (params.value === undefined || params.value === null) return null;
          const status = params.value as InboundStatus;

          let customClass = "";
          let labelKey = "";

          switch (status) {
            case InboundStatus.Approved:
              customClass = "bg-sky-500/10 text-sky-500 border-sky-500/20";
              labelKey = "inbound.po.status.approved";
              break;
            case InboundStatus.Receiving:
              customClass = "bg-purple-500/10 text-purple-500 border-purple-500/20";
              labelKey = "inbound.po.status.receiving";
              break;
            case InboundStatus.Completed:
              customClass = "bg-emerald-500/10 text-emerald-500 border-emerald-500/20";
              labelKey = "inbound.po.status.completed";
              break;
            case InboundStatus.Cancelled:
              customClass = "bg-rose-500/10 text-rose-500 border-rose-500/20";
              labelKey = "inbound.po.status.cancelled";
              break;
            default:
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
        headerName: t("inbound.po.columns.actions"),
        pinned: "right",
        width: 180,
        sortable: false,
        filter: false,
        resizable: false,
        cellRenderer: (params: any) => {
          if (!params.data) return null;
          const order = params.data as InboundOrderDto;
          const isReceivable =
            order.status === InboundStatus.Approved ||
            order.status === InboundStatus.Receiving;

          return (
            <div className="flex items-center gap-1.5 h-full">
              <Button
                variant="ghost"
                size="icon-xs"
                title={t("inbound.po.actions.viewDetails")}
                onClick={() => {
                  setViewingOrder(order);
                  setIsSheetOpen(true);
                }}
              >
                <Eye className="size-3.5" />
              </Button>
              <Button
                variant="default"
                size="xs"
                className="h-7 text-[10px] font-semibold cursor-pointer"
                disabled={!isReceivable}
                onClick={() => onSelectOrder(order)}
              >
                <Layers className="size-3 mr-1" />
                {t("inbound.po.actions.receive")}
              </Button>
            </div>
          );
        },
      },
    ],
    [t, onSelectOrder]
  );

  // AG Grid Datasource Configuration
  const datasource = useMemo<IDatasource>(() => {
    return {
      getRows: async (params: IGetRowsParams) => {
        try {
          setIsLoading(true);
          const page = Math.floor(params.startRow / DEFAULT_PAGE_SIZE) + 1;

          const searchObjects: SearchObject[] = Object.keys(params.filterModel || {}).map((key) => {
            const gridFilter = params.filterModel[key];
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

          const response = await inboundService.searchInboundOrders({
            searchObjects,
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
          console.error("Failed to load inbound orders:", error);
          setIsLoading(false);
          params.failCallback();
        }
      },
    };
  }, []);

  return (
    <div className="h-full w-full flex flex-col overflow-hidden bg-card border rounded-xl shadow-sm">
      <div className="flex-1 min-h-0 w-full overflow-hidden relative">
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
          />
        </div>
      </div>

      {/* Detail Slide panel (Sheet) */}
      <PurchaseOrderDetailSheet
        open={isSheetOpen}
        onOpenChange={setIsSheetOpen}
        order={viewingOrder}
        onReceive={() => {
          if (viewingOrder) {
            setIsSheetOpen(false);
            onSelectOrder(viewingOrder);
          }
        }}
      />
    </div>
  );
};
