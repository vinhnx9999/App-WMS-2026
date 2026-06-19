import { useState, useMemo, useEffect, useRef, forwardRef, useImperativeHandle } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Search, Plus, Trash } from "lucide-react";
import { debounce } from "lodash";
import { toast } from "sonner";
import type { SkuDto } from "../models/sku-dto.model";
import { skuService } from "../services/sku.service";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";

interface SkuListTabProps {
  onDeleteSku: (sku: SkuDto) => void;
  onAddSkuClick: () => void;
}

export const SkuListTab = forwardRef<AgGridReact, SkuListTabProps>(({ onDeleteSku, onAddSkuClick }, ref) => {
  const { t } = useTranslation();
  const [searchValue, setSearchValue] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const gridRef = useRef<AgGridReact>(null);
  const gridTheme = useAgGridTheme();
  const [isLoading, setIsLoading] = useState(false);

  useImperativeHandle(ref, () => gridRef.current!);

  const debouncedSetSearch = useMemo(
    () => debounce((val: string) => setDebouncedSearch(val), 500),
    []
  );

  useEffect(() => {
    return () => {
      debouncedSetSearch.cancel();
    };
  }, [debouncedSetSearch]);

  const columnDefs = useMemo<ColDef<SkuDto>[]>(() => [
    {
      field: "skuCode",
      headerName: t("translation:skus.skuCode"),
      pinned: "left",
      width: 140,
      cellRenderer: (params: any) => (
        <span className="font-bold text-primary">
          {params.value || ""}
        </span>
      )
    },
    {
      field: "name",
      headerName: t("translation:skus.name", "Tên SKU"),
      width: 200,
      editable: true,
      valueFormatter: (params) => params.value || ""
    },
    {
      field: "productName",
      headerName: t("translation:skus.linkedProduct"),
      width: 200,
      valueGetter: (params) => {
        const code = params.data?.productCode;
        const name = params.data?.productName;
        return code && name ? `${name} (${code})` : name || code || "";
      }
    },
    {
      field: "categoryName",
      headerName: t("translation:skus.categoryName"),
      width: 150,
      valueFormatter: (params) => params.value || ""
    },
    {
      field: "goodsNature",
      headerName: t("translation:skus.goodsNature"),
      width: 130,
      editable: true,
      valueFormatter: (params) => params.value || ""
    },
    {
      field: "referencePrice",
      headerName: t("translation:skus.referencePrice"),
      width: 140,
      type: "numericColumn",
      cellClass: "tabular-nums",
      editable: true,
      valueParser: (params) => {
        const parsed = parseFloat(params.newValue);
        return isNaN(parsed) ? null : parsed;
      },
      valueFormatter: (params) => {
        if (params.value == null) return "";
        return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(params.value);
      }
    },
    {
      field: "createdAt",
      headerName: t("translation:skus.createdAt"),
      width: 130,
      valueFormatter: (params) => {
        if (!params.value) return "";
        return new Date(params.value).toLocaleDateString("vi-VN");
      }
    },
    {
      field: "description",
      headerName: t("translation:skus.description"),
      flex: 1,
      minWidth: 200,
      editable: true,
      valueFormatter: (params) => params.value || ""
    },
    {
      headerName: t("translation:products.actions", "Thao tác"),
      pinned: "right",
      width: 80,
      sortable: false,
      filter: false,
      resizable: false,
      editable: false,
      cellRenderer: (params: any) => {
        if (!params.data) return null;
        return (
          <div className="flex items-center justify-center h-full">
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="destructive"
                  size="icon-xs"
                  onClick={() => onDeleteSku(params.data)}
                  aria-label={t("translation:skus.deleteTooltip", "Xóa SKU")}
                >
                  <Trash className="size-3.5" aria-hidden="true" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>{t("translation:skus.deleteTooltip", "Xóa SKU")}</p>
              </TooltipContent>
            </Tooltip>
          </div>
        );
      }
    }
  ], [t, onDeleteSku]);

  const pageSize = 10;

  const datasource = useMemo<IDatasource>(() => {
    return {
      getRows: async (params: IGetRowsParams) => {
        try {
          setIsLoading(true);
          const page = Math.floor(params.startRow / pageSize) + 1;
          const response = await skuService.searchSkus({
            search: debouncedSearch || undefined,
            page,
            limit: pageSize
          });
          setIsLoading(false);
          if (response.success && response.data) {
            const { items, totalCount } = response.data;
            params.successCallback(items, totalCount);
          } else {
            params.failCallback();
          }
        } catch (error) {
          console.error("Error loading SKU in datasource:", error);
          setIsLoading(false);
          params.failCallback();
        }
      }
    };
  }, [debouncedSearch]);

  const onCellValueChanged = async (event: any) => {
    const { data, colDef, newValue, oldValue } = event;
    if (newValue === oldValue) return;

    if (colDef.field === "referencePrice" && newValue !== null && newValue < 0) {
      toast.error(t("translation:skus.errors.invalidPrice"));
      gridRef.current?.api.refreshInfiniteCache();
      return;
    }

    const updatePayload = {
      name: data.name || null,
      goodsNature: data.goodsNature || null,
      description: data.description || null,
      price: data.referencePrice !== null ? Number(data.referencePrice) : null
    };

    try {
      const response = await skuService.updateSku(data.id, updatePayload);
      if (response.success) {
        toast.success(t("translation:skus.updateSuccess"));
      } else {
        toast.error(t("translation:skus.errors.generic"));
      }
    } catch (error: any) {
      console.error("Error updating SKU:", error);
      const status = error.response?.status;
      let errorKey = t("translation:skus.errors.updateFailed");
      if (status === 400) {
        errorKey = t("translation:skus.errors.validationFailed");
      }
      toast.error(errorKey);
    } finally {
      gridRef.current?.api.refreshInfiniteCache();
    }
  };

  return (
    <div className="flex-1 w-full flex flex-col overflow-hidden m-0 border-none outline-none">
      <div className="flex items-center justify-between gap-3 shrink-0 bg-card text-card-foreground p-3 border-b border-border">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" aria-hidden="true" />
          <input
            type="text"
            name="search"
            autoComplete="off"
            aria-label={t("skus.searchPlaceholder")}
            placeholder={t("skus.searchPlaceholder")}
            value={searchValue}
            onChange={(e) => {
              setSearchValue(e.target.value);
              debouncedSetSearch(e.target.value);
            }}
            className="w-full pl-9 pr-4 py-1.5 text-sm rounded-md border border-border bg-background text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/20 focus-visible:border-primary transition-colors"
          />
        </div>
        <div>
          <Button onClick={onAddSkuClick} className="flex items-center gap-1.5 text-sm cursor-pointer">
            <Plus className="size-4" aria-hidden="true" />
            {t("translation:skus.addSku")}
          </Button>
        </div>
      </div>

      <div className="flex-1 w-full overflow-hidden relative">
        <div className="w-full h-full">
          <AgGridReact
            ref={gridRef}
            columnDefs={columnDefs}
            datasource={datasource}
            rowModelType="infinite"
            cacheBlockSize={pageSize}
            maxConcurrentDatasourceRequests={1}
            infiniteInitialRowCount={pageSize}
            pagination={true}
            paginationPageSize={pageSize}
            paginationPageSizeSelector={false}
            theme={gridTheme}
            loading={isLoading}
            overlayLoadingTemplate={`<span class="ag-overlay-loading-center">${t("translation:common.loading")}</span>`}
            overlayNoRowsTemplate={`<span class="ag-overlay-no-rows-center">${t("translation:common.noData")}</span>`}
            stopEditingWhenCellsLoseFocus={true}
            singleClickEdit={true}
            onCellValueChanged={onCellValueChanged}
            defaultColDef={{
              resizable: true,
              sortable: false,
              filter: false,
              minWidth: 120
            }}
          />
        </div>
      </div>
    </div>
  );
});

SkuListTab.displayName = "SkuListTab";
