import { useState, useEffect, useRef, useMemo } from "react";
import { useTranslation } from "react-i18next";
import * as XLSX from "xlsx";
import { toast } from "sonner";
import {
  Upload,
  Download,
  FileText
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { skuService } from "../services/sku.service";
import { SkuImportDetail } from "./SkuImportDetail";
import { SkuImportUpload } from "./SkuImportUpload";

import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";
import { DEFAULT_PAGE_SIZE } from "@/constants";

const getStatusColor = (status: string) => {
  switch (status) {
    case "CONFIRMED":
      return "bg-emerald-500/15 text-emerald-700 border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-400";
    case "VALIDATED":
      return "bg-amber-500/15 text-amber-700 border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-400";
    case "CANCELLED":
      return "bg-slate-500/15 text-slate-700 border-slate-500/30 dark:bg-slate-500/10 dark:text-slate-400";
    case "FAILED":
      return "bg-rose-500/15 text-rose-700 border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-400";
    default:
      return "bg-slate-500/15 text-slate-700 border-slate-500/30";
  }
};

const formatDateTime = (dateStr: string) => {
  if (!dateStr) return "-";
  const date = new Date(dateStr);
  return date.toLocaleString();
};

interface SkuImportTabProps {
  onImportSuccess?: () => void;
}

export function SkuImportTab({ onImportSuccess }: SkuImportTabProps) {
  const { t } = useTranslation();
  const [view, setView] = useState<"list" | "detail" | "upload">("list");
  const [selectedSession, setSelectedSession] = useState<any>(null);

  // Pagination & Grid State
  const [totalCount, setTotalCount] = useState<number | null>(null);
  const gridRef = useRef<AgGridReact>(null);
  const gridTheme = useAgGridTheme();

  // Actions states
  const [uploading, setUploading] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [cancelling, setCancelling] = useState(false);

  const datasource = useMemo<IDatasource>(() => {
    return {
      getRows: async (params: IGetRowsParams) => {
        try {
          const pageNum = Math.floor(params.startRow / DEFAULT_PAGE_SIZE) + 1;
          const res = await skuService.searchImportSessions({
            page: pageNum,
            limit: DEFAULT_PAGE_SIZE
          });
          if (res.success && res.data) {
            setTotalCount(res.data.totalCount);
            params.successCallback(res.data.items, res.data.totalCount);
          } else {
            params.failCallback();
          }
        } catch (error) {
          console.error("Failed to load import sessions in datasource:", error);
          params.failCallback();
        }
      }
    };
  }, []);

  const columnDefs = useMemo<ColDef[]>(() => [
    {
      field: "sourceFileName",
      headerName: t("translation:skus.import.fileName"),
      flex: 2,
      minWidth: 200,
      cellRenderer: (params: any) => (
        <span className="font-semibold text-foreground">
          {params.value || ""}
        </span>
      )
    },
    {
      field: "status",
      headerName: t("translation:skus.import.status"),
      width: 140,
      cellRenderer: (params: any) => {
        if (!params.value) return null;
        return (
          <div className="flex items-center h-full">
            <span
              className={`px-2.5 py-0.5 rounded-full text-xs font-semibold border ${getStatusColor(
                params.value
              )}`}
            >
              {t(`translation:skus.import.statuses.${params.value}`)}
            </span>
          </div>
        );
      }
    },
    {
      field: "totalRows",
      headerName: t("translation:skus.import.totalRows"),
      width: 110,
      type: "numericColumn",
      cellClass: "tabular-nums"
    },
    {
      field: "validRows",
      headerName: t("translation:skus.import.validRows"),
      width: 110,
      type: "numericColumn",
      cellClass: "tabular-nums text-emerald-600 dark:text-emerald-400 font-medium"
    },
    {
      field: "invalidRows",
      headerName: t("translation:skus.import.invalidRows"),
      width: 110,
      type: "numericColumn",
      cellClass: "tabular-nums text-rose-600 dark:text-rose-400 font-medium"
    },
    {
      field: "createdAt",
      headerName: t("translation:skus.import.uploadTime"),
      width: 180,
      cellRenderer: (params: any) => {
        if (!params.value) return "";
        return (
          <span className="text-xs text-muted-foreground">
            {formatDateTime(params.value)}
          </span>
        );
      }
    },
    {
      headerName: t("translation:skus.import.actions"),
      pinned: "right",
      width: 130,
      sortable: false,
      filter: false,
      resizable: false,
      cellRenderer: (params: any) => {
        if (!params.data) return null;
        const displayLabel = params.data.status === "VALIDATED"
          ? t("translation:skus.import.confirmImport")
          : (t("translation:skus.import.actions") === "Actions" ? "View" : "Xem chi tiết");
        return (
          <div className="flex items-center justify-end h-full w-full pr-2">
            <Button
              variant="ghost"
              size="xs"
              onClick={() => viewDetail(params.data.id)}
              className="text-xs text-primary hover:text-primary-foreground hover:bg-primary cursor-pointer font-medium"
            >
              {displayLabel}
            </Button>
          </div>
        );
      }
    }
  ], [t]);

  useEffect(() => {
    if (view === "list") {
      setTotalCount(null);
      gridRef.current?.api?.refreshInfiniteCache();
    }
  }, [view]);

  // View details of a session
  const viewDetail = async (sessionId: string) => {
    try {
      const res = await skuService.getImportSession(sessionId);
      if (res.success && res.data) {
        setSelectedSession(res.data);
        setView("detail");
      } else {
        toast.error(t("translation:skus.errors.generic"));
      }
    } catch (error) {
      console.error("Failed to load session details:", error);
      toast.error(t("translation:skus.errors.generic"));
    }
  };

  // Download template
  const handleDownloadTemplate = () => {
    const headers = [
      ["Mã sản phẩm", "Mã SKU", "Tên SKU", "Tính chất hàng hóa", "Mô tả", "Giá tham chiếu"],
      ["PROD-001", "SKU-001", "Tên SKU mẫu 1", "Hàng thường", "Mô tả mẫu 1", 150000],
      ["PROD-002", "", "Tên SKU mẫu 2", "Hàng dễ vỡ", "Mô tả mẫu 2", 250000]
    ];
    const ws = XLSX.utils.aoa_to_sheet(headers);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, "Template SKU Import");
    XLSX.writeFile(wb, "sku_import_template.xlsx");
  };

  // Header mapper for spreadsheet rows
  const mapExcelHeaders = (row: any) => {
    const newRow: any = {};
    for (const key of Object.keys(row)) {
      const normalizedKey = key.trim().toLowerCase();
      const value = row[key];

      if (
        normalizedKey === "mã sản phẩm" ||
        normalizedKey === "product code" ||
        normalizedKey === "productcode" ||
        normalizedKey === "mã sp" ||
        normalizedKey === "product_code"
      ) {
        newRow.productCode = value ? String(value).trim() : null;
      } else if (
        normalizedKey === "mã sku" ||
        normalizedKey === "sku code" ||
        normalizedKey === "skucode" ||
        normalizedKey === "sku_code"
      ) {
        newRow.skuCode = value ? String(value).trim() : null;
      } else if (
        normalizedKey === "tên sku" ||
        normalizedKey === "sku name" ||
        normalizedKey === "name" ||
        normalizedKey === "tên" ||
        normalizedKey === "sku_name"
      ) {
        newRow.name = value ? String(value).trim() : null;
      } else if (
        normalizedKey === "tính chất" ||
        normalizedKey === "goods nature" ||
        normalizedKey === "goodsnature" ||
        normalizedKey === "tính chất hàng hóa" ||
        normalizedKey === "goods_nature"
      ) {
        newRow.goodsNature = value ? String(value).trim() : null;
      } else if (
        normalizedKey === "mô tả" ||
        normalizedKey === "description"
      ) {
        newRow.description = value ? String(value).trim() : null;
      } else if (
        normalizedKey === "giá tham chiếu" ||
        normalizedKey === "reference price" ||
        normalizedKey === "referenceprice" ||
        normalizedKey === "giá" ||
        normalizedKey === "price" ||
        normalizedKey === "reference_price"
      ) {
        newRow.referencePrice =
          value !== undefined && value !== null && !isNaN(Number(value))
            ? Number(value)
            : null;
      }
    }
    return newRow;
  };



  const processExcelFile = (file: File) => {
    const ext = file.name.split(".").pop()?.toLowerCase();
    if (ext !== "xlsx" && ext !== "xls") {
      toast.error(t("translation:skus.import.errors.EMPTY_IMPORT"));
      return;
    }

    setUploading(true);
    const reader = new FileReader();
    reader.onload = async (e) => {
      try {
        const data = e.target?.result;
        if (!data) return;

        const workbook = XLSX.read(data, { type: "array" });
        const sheetName = workbook.SheetNames[0];
        const worksheet = workbook.Sheets[sheetName];

        // Convert sheet to JSON array
        const jsonRows = XLSX.utils.sheet_to_json<any>(worksheet);

        if (jsonRows.length === 0) {
          toast.error(t("translation:skus.import.emptyRows"));
          setUploading(false);
          return;
        }

        const mappedRows = jsonRows.map((row, index) => {
          const mapped = mapExcelHeaders(row);
          return {
            rowNumber: index + 2, // Excel row numbers starts at 1, row 1 is usually header
            productCode: mapped.productCode || null,
            skuCode: mapped.skuCode || null,
            name: mapped.name || null,
            goodsNature: mapped.goodsNature || null,
            description: mapped.description || null,
            referencePrice: mapped.referencePrice
          };
        });

        // Call backend API to create import session
        const res = await skuService.createImportSession(file.name, mappedRows);
        if (res.success && res.data) {
          toast.success(t("translation:skus.import.importSuccess"));
          // Immediately pull full session details
          await viewDetail(res.data.importSessionId);
        } else {
          toast.error(res.message || t("translation:skus.errors.generic"));
        }
      } catch (err: any) {
        console.error("Error parsing Excel:", err);
        toast.error(t("translation:skus.errors.generic"));
      } finally {
        setUploading(false);
      }
    };
    reader.readAsArrayBuffer(file);
  };

  // Confirm import session
  const handleConfirmSession = async () => {
    if (!selectedSession) return;
    try {
      setConfirming(true);
      const res = await skuService.confirmImportSession(selectedSession.importSessionId);
      if (res.success) {
        toast.success(t("translation:skus.import.importSuccess"));
        if (onImportSuccess) {
          onImportSuccess();
        }
        // Refresh details
        await viewDetail(selectedSession.importSessionId);
      } else {
        toast.error(res.message || t("translation:skus.errors.generic"));
      }
    } catch (error: any) {
      console.error("Confirm session failed:", error);
      toast.error(error.response?.data?.message || t("translation:skus.errors.generic"));
    } finally {
      setConfirming(false);
    }
  };

  // Cancel import session
  const handleCancelSession = async () => {
    if (!selectedSession) return;
    try {
      setCancelling(true);
      const res = await skuService.cancelImportSession(selectedSession.importSessionId);
      if (res.success) {
        toast.success(t("translation:skus.import.importCancelled"));
        // Refresh details
        await viewDetail(selectedSession.importSessionId);
      } else {
        toast.error(res.message || t("translation:skus.errors.generic"));
      }
    } catch (error) {
      console.error("Cancel session failed:", error);
      toast.error(t("translation:skus.errors.generic"));
    } finally {
      setCancelling(false);
    }
  };


  return (
    <div className="w-full h-full flex flex-col overflow-y-auto">
      {/* VIEW: LIST OF PAST SESSIONS */}
      {view === "list" && (
        <Card className="flex-1 w-full bg-card border border-border flex flex-col justify-between">
          <CardHeader className="flex flex-row items-center justify-between pb-4 shrink-0 border-b border-border">
            <div>
              <CardTitle className="text-xl font-bold tracking-tight">
                {t("translation:skus.import.listTitle")}
              </CardTitle>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={handleDownloadTemplate}
                className="flex items-center gap-1.5 cursor-pointer text-xs"
              >
                <Download className="size-3.5" />
                {t("translation:skus.import.downloadTemplate")}
              </Button>
              <Button
                size="sm"
                onClick={() => setView("upload")}
                className="flex items-center gap-1.5 cursor-pointer text-xs"
              >
                <Upload className="size-3.5" />
                {t("translation:skus.import.newImport")}
              </Button>
            </div>
          </CardHeader>
          <CardContent className="flex-1 w-full p-0 overflow-hidden min-h-[300px] flex flex-col">
            {totalCount === 0 ? (
              <div className="w-full h-full flex flex-col items-center justify-center py-16 text-muted-foreground gap-4 text-center">
                <div className="p-4 bg-muted rounded-full">
                  <FileText className="size-10 text-muted-foreground" />
                </div>
                <div>
                  <h3 className="text-base font-semibold">{t("translation:common.noData")}</h3>
                  <p className="text-sm text-muted-foreground mt-1 max-w-sm">
                    {t("translation:skus.import.noData")}
                  </p>
                </div>
                <Button onClick={() => setView("upload")} className="cursor-pointer">
                  {t("translation:skus.import.newImport")}
                </Button>
              </div>
            ) : (
              <div className="flex-1 w-full overflow-hidden relative">
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
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* VIEW: UPLOAD Excel FILE */}
      {view === "upload" && (
        <SkuImportUpload
          uploading={uploading}
          onUpload={processExcelFile}
          onDownloadTemplate={handleDownloadTemplate}
        />
      )}

      {/* VIEW: SESSION DETAIL PREVIEW & CONFIRM */}
      {view === "detail" && selectedSession && (
        <SkuImportDetail
          selectedSession={selectedSession}
          confirming={confirming}
          cancelling={cancelling}
          handleConfirmSession={handleConfirmSession}
          handleCancelSession={handleCancelSession}
          onBack={() => setView("list")}
          onSessionUpdated={setSelectedSession}
        />
      )}
    </div>
  );
}
