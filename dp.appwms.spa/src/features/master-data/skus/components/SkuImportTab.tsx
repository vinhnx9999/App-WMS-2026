import { useState, useEffect, useRef, useMemo } from "react";
import { useTranslation } from "react-i18next";
import * as XLSX from "xlsx";
import { toast } from "sonner";
import {
  ArrowLeft,
  Upload,
  Download,
  CheckCircle,
  XCircle,
  AlertCircle,
  Loader2,
  Calendar,
  FileText,
  Check,
  X
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { skuService } from "../services/sku.service";

import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";

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
  const limit = 10;
  const gridRef = useRef<AgGridReact>(null);
  const gridTheme = useAgGridTheme();

  // Actions states
  const [uploading, setUploading] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [cancelling, setCancelling] = useState(false);

  // Drag and drop ref
  const fileInputRef = useRef<HTMLInputElement>(null);

  const datasource = useMemo<IDatasource>(() => {
    return {
      getRows: async (params: IGetRowsParams) => {
        try {
          const pageNum = Math.floor(params.startRow / limit) + 1;
          const res = await skuService.searchImportSessions({
            page: pageNum,
            limit
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
          {params.value || "N/A"}
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

  // Handle file import
  const handleFileDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    const files = e.dataTransfer.files;
    if (files && files.length > 0) {
      processExcelFile(files[0]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files && files.length > 0) {
      processExcelFile(files[0]);
    }
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
                    cacheBlockSize={limit}
                    maxConcurrentDatasourceRequests={1}
                    infiniteInitialRowCount={limit}
                    pagination={true}
                    paginationPageSize={limit}
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
        <Card className="flex-1 w-full bg-card border border-border flex flex-col">
          <CardHeader className="flex flex-row items-center gap-4 pb-4 border-b border-border shrink-0">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setView("list")}
              className="rounded-full cursor-pointer hover:bg-accent"
            >
              <ArrowLeft className="size-4" />
            </Button>
            <div>
              <CardTitle className="text-xl font-bold tracking-tight">
                {t("translation:skus.import.newImport")}
              </CardTitle>
              <CardDescription className="text-sm text-muted-foreground">
                Tải lên file Excel để kiểm tra và import SKU vào kho hàng.
              </CardDescription>
            </div>
          </CardHeader>
          <CardContent className="flex-1 flex flex-col items-center justify-center p-8">
            <div
              onDragOver={(e) => e.preventDefault()}
              onDrop={handleFileDrop}
              onClick={() => fileInputRef.current?.click()}
              className="w-full max-w-xl border-2 border-dashed border-muted hover:border-primary/50 transition-all rounded-xl p-12 flex flex-col items-center justify-center gap-4 bg-muted/20 hover:bg-muted/40 cursor-pointer text-center group"
            >
              <input
                type="file"
                ref={fileInputRef}
                onChange={handleFileChange}
                accept=".xlsx, .xls"
                className="hidden"
              />
              {uploading ? (
                <div className="flex flex-col items-center gap-3">
                  <Loader2 className="size-10 animate-spin text-primary" />
                  <span className="text-sm font-semibold">{t("translation:skus.import.uploading")}</span>
                </div>
              ) : (
                <>
                  <div className="p-4 bg-card rounded-full border border-border shadow-sm group-hover:scale-110 transition-transform duration-200">
                    <Upload className="size-8 text-primary" />
                  </div>
                  <div>
                    <p className="text-base font-semibold text-foreground group-hover:text-primary transition-colors">
                      {t("translation:skus.import.dragDropArea")}
                    </p>
                    <p className="text-xs text-muted-foreground mt-1.5">
                      {t("translation:skus.import.dragDropSub")}
                    </p>
                  </div>
                </>
              )}
            </div>
            <div className="mt-8 flex gap-4">
              <Button variant="outline" onClick={handleDownloadTemplate} className="cursor-pointer text-sm gap-1.5">
                <Download className="size-4" />
                {t("translation:skus.import.downloadTemplate")}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* VIEW: SESSION DETAIL PREVIEW & CONFIRM */}
      {view === "detail" && selectedSession && (
        <Card className="flex-1 w-full bg-card border border-border flex flex-col justify-between">
          <CardHeader className="flex flex-col md:flex-row md:items-center justify-between pb-4 border-b border-border shrink-0 gap-4">
            <div className="flex items-center gap-3">
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setView("list")}
                className="rounded-full cursor-pointer hover:bg-accent"
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
                  <span>Tổng số dòng: {selectedSession.totalRows}</span>
                  <span className="text-emerald-600 dark:text-emerald-400 font-semibold">
                    Hợp lệ: {selectedSession.validRows}
                  </span>
                  <span className="text-rose-600 dark:text-rose-400 font-semibold">
                    Lỗi: {selectedSession.invalidRows}
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
                  className="border-rose-500/30 text-rose-600 hover:bg-rose-500/10 cursor-pointer text-xs"
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
                  className="bg-emerald-600 hover:bg-emerald-700 text-white cursor-pointer text-xs"
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
          <CardContent className="flex-1 w-full p-0 overflow-y-auto min-h-[300px]">
            {/* Status alerts */}
            {selectedSession.status === "CONFIRMED" && (
              <div className="p-4 bg-emerald-500/10 border-b border-emerald-500/20 text-emerald-700 dark:text-emerald-400 flex items-center gap-2.5 text-sm font-medium">
                <CheckCircle className="size-5 shrink-0" />
                <span>
                  Phiên nhập Excel này đã được hoàn tất và xác nhận thành công vào hệ thống.
                </span>
              </div>
            )}
            {selectedSession.status === "CANCELLED" && (
              <div className="p-4 bg-slate-500/10 border-b border-slate-500/20 text-slate-700 dark:text-slate-400 flex items-center gap-2.5 text-sm font-medium">
                <XCircle className="size-5 shrink-0" />
                <span>Phiên nhập Excel này đã bị huỷ bỏ bởi người dùng.</span>
              </div>
            )}
            {selectedSession.status === "FAILED" && (
              <div className="p-4 bg-rose-500/10 border-b border-rose-500/20 text-rose-700 dark:text-rose-400 flex items-center gap-2.5 text-sm font-medium">
                <AlertCircle className="size-5 shrink-0" />
                <div>
                  <span className="font-semibold">Phiên nhập Excel thất bại. </span>
                  {selectedSession.failureReason && (
                    <p className="text-xs mt-1 bg-rose-950/20 p-2 rounded border border-rose-500/20">
                      Lý do: {selectedSession.failureReason}
                    </p>
                  )}
                </div>
              </div>
            )}

            {/* Rows Table */}
            <div className="w-full overflow-x-auto">
              <table className="w-full text-xs text-left">
                <thead className="bg-muted text-muted-foreground uppercase border-b border-border font-semibold">
                  <tr>
                    <th className="px-6 py-3 text-center w-[80px]">
                      {t("translation:skus.import.rowNumber")}
                    </th>
                    <th className="px-6 py-3 w-[120px]">
                      {t("translation:skus.import.validationStatus")}
                    </th>
                    <th className="px-6 py-3">{t("translation:skus.import.productCode")}</th>
                    <th className="px-6 py-3">{t("translation:skus.import.skuCode")}</th>
                    <th className="px-6 py-3">{t("translation:skus.import.skuName")}</th>
                    <th className="px-6 py-3">{t("translation:skus.import.goodsNature")}</th>
                    <th className="px-6 py-3 text-right">{t("translation:skus.import.referencePrice")}</th>
                    <th className="px-6 py-3 w-[250px]">{t("translation:skus.import.errorMessage")}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {selectedSession.rows?.map((row: any) => (
                    <tr
                      key={row.importRowId}
                      className={`hover:bg-accent/30 transition-colors ${!row.isValid
                        ? "bg-rose-500/5 hover:bg-rose-500/10 border-l-2 border-l-rose-500"
                        : "border-l-2 border-l-transparent"
                        }`}
                    >
                      <td className="px-6 py-3 text-center font-medium text-muted-foreground">
                        {row.rowNumber}
                      </td>
                      <td className="px-6 py-3 font-semibold">
                        {row.isValid ? (
                          <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-1">
                            <CheckCircle className="size-3" /> Hợp lệ
                          </span>
                        ) : (
                          <span className="text-rose-600 dark:text-rose-400 flex items-center gap-1">
                            <AlertCircle className="size-3" /> Bị lỗi
                          </span>
                        )}
                      </td>
                      <td className="px-6 py-3 font-medium">{row.productCode || "-"}</td>
                      <td className="px-6 py-3">
                        {row.skuCode ? (
                          <span className="font-mono">{row.skuCode}</span>
                        ) : (
                          <span className="text-muted-foreground italic text-[10px]">Tự động sinh</span>
                        )}
                      </td>
                      <td className="px-6 py-3 font-medium">{row.name || "-"}</td>
                      <td className="px-6 py-3">{row.goodsNature || "-"}</td>
                      <td className="px-6 py-3 text-right font-medium">
                        {row.referencePrice !== null && row.referencePrice !== undefined
                          ? Number(row.referencePrice).toLocaleString()
                          : "-"}
                      </td>
                      <td className="px-6 py-3 text-rose-600 dark:text-rose-400 font-medium whitespace-normal break-words max-w-[250px]">
                        {row.errorCode
                          ? t(`translation:skus.import.errors.${row.errorCode}`, {
                            defaultValue: row.errorMessage || row.errorCode
                          })
                          : "-"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
          <div className="flex items-center justify-end px-6 py-4 border-t border-border shrink-0 bg-muted/40">
            <Button variant="outline" size="sm" onClick={() => setView("list")} className="cursor-pointer text-xs">
              {t("translation:skus.import.backToList")}
            </Button>
          </div>
        </Card>
      )}
    </div>
  );
}
