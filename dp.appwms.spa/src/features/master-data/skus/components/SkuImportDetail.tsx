import { useMemo, useRef } from "react";
import { useTranslation } from "react-i18next";
import {
  Calendar,
  CheckCircle,
  AlertCircle,
  Loader2,
  X,
  Check,
  ArrowLeft
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";
import { skuService } from "../services/sku.service";
import { toast } from "sonner";
import { DEFAULT_PAGE_SIZE } from "@/constants";

const getStatusColor = (status: string) => {
  switch (status) {
    case "CONFIRMED":
      return "bg-success/15 text-success border-success/30";
    case "VALIDATED":
      return "bg-warning/15 text-warning border-warning/30";
    case "CANCELLED":
      return "bg-muted text-muted-foreground border-border";
    case "FAILED":
      return "bg-destructive/15 text-destructive border-destructive/30";
    default:
      return "bg-muted text-muted-foreground border-border";
  }
};

const formatDateTime = (dateStr: string) => {
  if (!dateStr) return "-";
  const date = new Date(dateStr);
  return date.toLocaleString();
};

interface SkuImportDetailProps {
  selectedSession: any;
  confirming: boolean;
  cancelling: boolean;
  handleConfirmSession: () => void;
  handleCancelSession: () => void;
  onBack: () => void;
  onSessionUpdated?: (updatedSession: any) => void;
}

export function SkuImportDetail({
  selectedSession,
  confirming,
  cancelling,
  handleConfirmSession,
  handleCancelSession,
  onBack,
  onSessionUpdated
}: SkuImportDetailProps) {
  const { t } = useTranslation();
  const gridTheme = useAgGridTheme();
  const gridRef = useRef<AgGridReact>(null);

  const isProcessingRef = useRef(false);

  const isEditable = selectedSession.status === "VALIDATED";

  const datasource = useMemo<IDatasource>(() => {
    return {
      getRows: async (params: IGetRowsParams) => {
        try {
          const pageNum = Math.floor(params.startRow / DEFAULT_PAGE_SIZE) + 1;
          const res = await skuService.getImportSession(
            selectedSession.importSessionId || selectedSession.id,
            pageNum,
            DEFAULT_PAGE_SIZE
          );
          if (res.success && res.data) {
            params.successCallback(res.data.rows.items, res.data.rows.totalCount);
          } else {
            params.failCallback();
          }
        } catch (error) {
          console.error("Failed to load rows in datasource:", error);
          params.failCallback();
        }
      }
    };
  }, [selectedSession.importSessionId, selectedSession.id]);

  const onCellValueChanged = async (event: any) => {
    // Block any event triggered by our own data updates (rollback, setData, or React re-render)
    if (isProcessingRef.current) return;

    const { data, colDef, newValue, oldValue } = event;
    if (newValue === oldValue) return;

    // Client-side validation: negative price
    if (colDef.field === "referencePrice" && newValue !== null && newValue < 0) {
      toast.error(t("translation:skus.errors.invalidPrice") || "Price cannot be negative");
      isProcessingRef.current = true;
      try {
        event.node.setDataValue(colDef.field, oldValue);
      } finally {
        // Delay reset to survive any async events from AG Grid change detection
        setTimeout(() => { isProcessingRef.current = false; }, 100);
      }
      return;
    }

    isProcessingRef.current = true;

    try {
      const response = await skuService.updateImportRow(
        selectedSession.importSessionId || selectedSession.id,
        data.importRowId,
        {
          productCode: (data.productCode && data.productCode.trim()) || null,
          skuCode: (data.skuCode && data.skuCode.trim()) || null,
          name: (data.name && data.name.trim()) || null,
          goodsNature: (data.goodsNature && data.goodsNature.trim()) || null,
          description: (data.description && data.description.trim()) || null,
          referencePrice: data.referencePrice !== null && data.referencePrice !== undefined ? Number(data.referencePrice) : null
        }
      );

      if (response.success && response.data) {
        toast.success(t("translation:skus.import.rowUpdatedSuccess") || "Row updated successfully");

        // Refresh the grid rows cache to pull latest validation and data state
        gridRef.current?.api?.refreshInfiniteCache();

        if (onSessionUpdated) {
          onSessionUpdated({
            ...selectedSession,
            status: response.data.status,
            totalRows: response.data.totalRows,
            validRows: response.data.validRows,
            invalidRows: response.data.invalidRows
          });
        }
      } else {
        throw new Error(response.message || "Failed to update row");
      }
    } catch (error: any) {
      console.error("Failed to update row:", error);
      toast.error(error.message || t("translation:skus.errors.generic"));
      // Rollback the cell to old value
      event.node.setDataValue(colDef.field, oldValue);
    } finally {
      setTimeout(() => { isProcessingRef.current = false; }, 100);
    }
  };

  const columnDefs = useMemo<ColDef[]>(() => {
    return [
      {
        field: "rowNumber",
        headerName: t("translation:skus.import.rowNumber"),
        width: 80,
        cellClass: "text-center font-medium text-muted-foreground",
        pinned: "left"
      },
      {
        field: "isValid",
        headerName: t("translation:skus.import.validationStatus"),
        width: 140,
        cellRenderer: (params: any) => {
          if (params.data?.isValid) {
            return (
              <span className="text-success flex items-center gap-1 h-full font-semibold">
                <CheckCircle className="size-3" /> {t("translation:skus.import.rowValid")}
              </span>
            );
          } else {
            return (
              <span className="text-destructive flex items-center gap-1 h-full font-semibold">
                <AlertCircle className="size-3" /> {t("translation:skus.import.rowInvalid")}
              </span>
            );
          }
        }
      },
      {
        field: "productCode",
        headerName: t("translation:skus.import.productCode"),
        editable: isEditable,
        cellClass: isEditable ? "cursor-pointer hover:bg-accent/40 font-medium" : "font-medium"
      },
      {
        field: "skuCode",
        headerName: t("translation:skus.import.skuCode"),
        editable: isEditable,
        cellClass: isEditable ? "font-mono cursor-pointer hover:bg-accent/40" : "font-mono"
      },
      {
        field: "name",
        headerName: t("translation:skus.import.skuName"),
        editable: isEditable,
        cellClass: isEditable ? "font-medium cursor-pointer hover:bg-accent/40" : "font-medium"
      },
      {
        field: "goodsNature",
        headerName: t("translation:skus.import.goodsNature"),
        editable: isEditable,
        cellClass: isEditable ? "cursor-pointer hover:bg-accent/40" : ""
      },
      {
        field: "description",
        headerName: t("translation:skus.description"),
        editable: isEditable,
        cellClass: isEditable ? "cursor-pointer hover:bg-accent/40" : "",
        width: 180
      },
      {
        field: "referencePrice",
        headerName: t("translation:skus.import.referencePrice"),
        editable: isEditable,
        type: "numericColumn",
        valueFormatter: (params: any) => {
          if (params.value === null || params.value === undefined) return "-";
          return Number(params.value).toLocaleString();
        },
        valueParser: (params: any) => {
          if (params.newValue === "" || params.newValue === null || params.newValue === undefined) {
            return null;
          }
          const val = parseFloat(params.newValue);
          return isNaN(val) ? null : val;
        },
        cellClass: isEditable ? "cursor-pointer hover:bg-accent/40 font-medium text-right" : "font-medium text-right"
      },
      {
        field: "errorCode",
        headerName: t("translation:skus.import.errorMessage"),
        flex: 1,
        minWidth: 250,
        cellClass: "text-destructive font-medium whitespace-normal break-words",
        valueGetter: (params: any) => {
          if (!params.data?.errorCode) return "";
          return t(`translation:skus.import.errors.${params.data.errorCode}`, {
            defaultValue: params.data.errorMessage || params.data.errorCode
          });
        }
      }
    ];
  }, [t, isEditable]);

  const getRowClass = (params: any) => {
    if (params.data && !params.data.isValid) {
      return "bg-destructive/5 hover:bg-destructive/10 border-l-2 border-l-destructive";
    }
    return "border-l-2 border-l-transparent";
  };

  return (
    <Card className="flex-1 w-full bg-card border border-border flex flex-col justify-between overflow-hidden">
      <CardHeader className="flex flex-col md:flex-row md:items-center justify-between pb-4 border-b border-border shrink-0 gap-4">
        <div className="flex items-center gap-3">
          <Button
            variant="ghost"
            size="icon"
            onClick={onBack}
            className="size-8 cursor-pointer shrink-0"
            title={t("translation:common.back")}
          >
            <ArrowLeft className="size-4" />
          </Button>
          <div>
            <CardTitle className="text-lg font-bold flex items-center gap-2 max-w-[400px] truncate" title={selectedSession.sourceFileName}>
              {selectedSession.sourceFileName || "Import Session"}
              <span
                className={`px-2.5 py-0.5 rounded-full text-xs font-semibold border ${getStatusColor(
                  selectedSession.status
                )}`}
              >
                {t(`translation:skus.import.statuses.${selectedSession.status}`)}
              </span>
            </CardTitle>
            <CardDescription className="text-xs text-muted-foreground mt-1 flex flex-wrap gap-x-4 gap-y-1">
              <span className="flex items-center gap-1">
                <Calendar className="size-3" />
                {formatDateTime(selectedSession.createdAt)}
              </span>
              <span>{t("translation:skus.import.totalRows")}: {selectedSession.totalRows}</span>
              <span className="text-emerald-600 dark:text-emerald-400 font-semibold">
                {t("translation:skus.import.validRows")}: {selectedSession.validRows}
              </span>
              <span className="text-rose-600 dark:text-rose-400 font-semibold">
                {t("translation:skus.import.invalidRows")}: {selectedSession.invalidRows}
              </span>
            </CardDescription>
          </div>
        </div>
        {selectedSession.status === "VALIDATED" && (
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              onClick={handleCancelSession}
              disabled={cancelling || confirming}
              className="border-destructive/30 text-destructive hover:bg-destructive/10 cursor-pointer text-xs"
            >
              {cancelling ? (
                <Loader2 className="size-3 animate-spin mr-1.5" />
              ) : (
                <X className="size-3.5 mr-1.5" />
              )}
              {t("translation:skus.import.cancelImport")}
            </Button>
            <Button
              onClick={handleConfirmSession}
              disabled={selectedSession.validRows === 0 || confirming || cancelling}
              className="bg-success hover:bg-success/90 text-success-foreground cursor-pointer text-xs"
            >
              {confirming ? (
                <Loader2 className="size-3 animate-spin mr-1.5" />
              ) : (
                <Check className="size-3.5 mr-1.5" />
              )}
              {t("translation:skus.import.confirmImport")}
            </Button>
          </div>
        )}
      </CardHeader>
      <CardContent className="flex-1 w-full p-0 min-h-[400px] relative overflow-hidden">
        <div className="w-full h-full">
          <AgGridReact
            ref={gridRef}
            rowModelType="infinite"
            datasource={datasource}
            cacheBlockSize={DEFAULT_PAGE_SIZE}
            pagination={true}
            paginationPageSize={DEFAULT_PAGE_SIZE}
            paginationPageSizeSelector={false}
            getRowId={(params: any) => params.data.importRowId}
            columnDefs={columnDefs}
            theme={gridTheme}
            singleClickEdit={true}
            onCellValueChanged={onCellValueChanged}
            getRowClass={getRowClass}
            overlayLoadingTemplate={`<span class="ag-overlay-loading-center">${t("translation:common.loading")}</span>`}
            overlayNoRowsTemplate={`<span class="ag-overlay-no-rows-center">${t("translation:common.noData")}</span>`}
            defaultColDef={{
              resizable: true,
              sortable: false,
              filter: false,
              minWidth: 100
            }}
          />
        </div>
      </CardContent>
    </Card>
  );
}
