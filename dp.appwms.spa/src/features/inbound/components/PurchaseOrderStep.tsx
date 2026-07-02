import React, { useState, useMemo, useEffect, useRef } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Search, Eye, Layers } from "lucide-react";
import { debounce } from "lodash";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";
import { DEFAULT_PAGE_SIZE } from "@/constants";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { supplierService } from "@/features/master-data/suppliers/services/supplier.service";
import { inboundService } from "../services/inbound.service";
import { InboundStatus, type InboundOrderDto } from "../models/inbound.model";
import { PurchaseOrderDetailSheet } from "./PurchaseOrderDetailSheet";

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

  // Filter States
  const [searchValue, setSearchValue] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [suppliers, setSuppliers] = useState<{ id: string; code: string; name: string }[]>([]);
  const [selectedSupplier, setSelectedSupplier] = useState<string>("all");
  const [selectedStatus, setSelectedStatus] = useState<string>("all");

  // Loading and Selection States
  const [isLoading, setIsLoading] = useState(false);
  const [isSheetOpen, setIsSheetOpen] = useState(false);
  const [viewingOrder, setViewingOrder] = useState<InboundOrderDto | null>(null);

  // Fetch Supplier Lookup on Mount
  useEffect(() => {
    const fetchSuppliers = async () => {
      try {
        const response = await supplierService.supplierLookup();
        if (response.success && response.data) {
          setSuppliers(response.data);
        }
      } catch (err) {
        console.error("Failed to load suppliers lookup", err);
      }
    };
    fetchSuppliers();
  }, []);

  // Search Debounce
  const debouncedSetSearch = useMemo(
    () => debounce((val: string) => setDebouncedSearch(val), 500),
    []
  );

  useEffect(() => {
    return () => {
      debouncedSetSearch.cancel();
    };
  }, [debouncedSetSearch]);

  // Trigger grid cache refresh on filter changes
  useEffect(() => {
    gridRef.current?.api?.refreshInfiniteCache();
  }, [debouncedSearch, selectedSupplier, selectedStatus]);

  // Column definitions for AG Grid
  const columnDefs = useMemo<ColDef<InboundOrderDto>[]>(
    () => [
      {
        field: "orderNumber",
        headerName: t("inbound.po.columns.orderNumber"),
        pinned: "left",
        width: 180,
        cellRenderer: (params: any) => {
          if (!params.value) return null;
          return <span className="font-semibold text-primary">{params.value}</span>;
        },
      },
      {
        field: "supplierName",
        headerName: t("inbound.po.columns.supplierName"),
        width: 250,
        cellRenderer: (params: any) => params.value || <span className="text-muted-foreground">-</span>,
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
        width: 150,
        cellRenderer: (params: any) => {
          if (params.value === undefined || params.value === null) return null;
          const status = params.value as InboundStatus;

          let customClass = "";
          let labelKey = "";

          switch (status) {
            case InboundStatus.Pending:
              customClass = "bg-amber-500/10 text-amber-500 border-amber-500/20";
              labelKey = "inbound.po.status.pending";
              break;
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
            order.status === InboundStatus.Pending ||
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

          const queryParams: any = {
            search: debouncedSearch || undefined,
            supplierId: selectedSupplier !== "all" ? selectedSupplier : undefined,
            page,
            limit: DEFAULT_PAGE_SIZE,
          };

          if (selectedStatus !== "all") {
            queryParams.status = parseInt(selectedStatus, 10);
          }

          if (params.sortModel && params.sortModel.length > 0) {
            queryParams.sortBy = params.sortModel[0].colId.toLowerCase();
            queryParams.sortOrder = params.sortModel[0].sort;
          }

          const response = await inboundService.searchInboundOrders(queryParams);
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
  }, [debouncedSearch, selectedSupplier, selectedStatus]);

  return (
    <div className="h-full w-full flex flex-col overflow-hidden bg-card border rounded-xl shadow-sm">
      {/* Top Filter Bar */}
      <div className="flex flex-wrap items-center justify-between gap-3 p-3 bg-secondary/10 border-b shrink-0">
        <div className="flex flex-wrap items-center gap-2 flex-1">
          {/* Quick Search */}
          <div className="relative w-full sm:max-w-[260px]">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 size-3.5 text-muted-foreground" />
            <Input
              type="text"
              placeholder={t("inbound.po.searchPlaceholder")}
              value={searchValue}
              onChange={(e) => {
                setSearchValue(e.target.value);
                debouncedSetSearch(e.target.value);
              }}
              className="w-full pl-8 h-8 text-xs bg-background"
            />
          </div>

          {/* Supplier Select */}
          <Select value={selectedSupplier} onValueChange={setSelectedSupplier}>
            <SelectTrigger className="w-[180px] h-8 text-xs bg-background">
              <SelectValue placeholder={t("inbound.po.filterSupplier")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">{t("inbound.po.allSuppliers")}</SelectItem>
              {suppliers.map((s) => (
                <SelectItem key={s.id} value={s.id}>
                  {s.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {/* Status Select */}
          <Select value={selectedStatus} onValueChange={setSelectedStatus}>
            <SelectTrigger className="w-[150px] h-8 text-xs bg-background">
              <SelectValue placeholder={t("inbound.po.filterStatus")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">{t("inbound.po.status.all")}</SelectItem>
              <SelectItem value={InboundStatus.Pending.toString()}>
                {t("inbound.po.status.pending")}
              </SelectItem>
              <SelectItem value={InboundStatus.Approved.toString()}>
                {t("inbound.po.status.approved")}
              </SelectItem>
              <SelectItem value={InboundStatus.Receiving.toString()}>
                {t("inbound.po.status.receiving")}
              </SelectItem>
              <SelectItem value={InboundStatus.Completed.toString()}>
                {t("inbound.po.status.completed")}
              </SelectItem>
              <SelectItem value={InboundStatus.Cancelled.toString()}>
                {t("inbound.po.status.cancelled")}
              </SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Grid Container */}
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
              filter: false,
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
