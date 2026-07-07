import { useEffect, useMemo, useState } from "react";
import { useParams, useNavigate, useBlocker } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AgGridReact } from "ag-grid-react";
import type { ColDef } from "ag-grid-community";
import { ArrowLeft, Save, Check, Loader2, AlertTriangle } from "lucide-react";
import { toast } from "sonner";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useWarehouseStore } from "@/store/warehouse-store";
import { inboundService } from "../services/inbound.service";
import { ReceiptStatus, type GetInboundReceiptByIdResponse } from "../models/inbound.model";

interface QcItemRow {
  skuId: string;
  skuCode: string;
  skuName: string;
  totalQty: number;
  passQty: number;
  failQty: number;
  notes: string | null;
}

export default function QcDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const gridTheme = useAgGridTheme();
  const { selectedWarehouse } = useWarehouseStore();

  const [isDirty, setIsDirty] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [qcTask, setQcTask] = useState<GetInboundReceiptByIdResponse | null>(null);
  const [rows, setRows] = useState<QcItemRow[]>([]);

  useEffect(() => {
    const fetchQcTask = async (taskId: string) => {
      setLoading(true);
      setError(null);
      try {
        // Using receipt API as a placeholder for QC API
        const response = await inboundService.getInboundReceiptById(taskId);
        if (response.success && response.data) {
          setQcTask(response.data);
          const initialRows: QcItemRow[] = response.data.items.map((item: any) => ({
            skuId: item.skuId,
            skuCode: item.skuCode || "",
            skuName: item.skuName || "",
            totalQty: item.receivedQuantity,
            passQty: item.receivedQuantity, // Default all to pass
            failQty: 0,
            notes: item.notes,
          }));
          setRows(initialRows);
          setIsDirty(false);
        } else {
          setError(response.message || "Failed to load QC task details");
        }
      } catch (err) {
        setError("An error occurred while loading QC task details");
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      fetchQcTask(id);
    }
  }, [id]);

  const isReadOnly = useMemo(() => {
    return qcTask?.status === ReceiptStatus.Completed;
  }, [qcTask]);

  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      isDirty && !isReadOnly && currentLocation.pathname !== nextLocation.pathname
  );

  const onCellValueChanged = (event: any) => {
    const { data, column, newValue, oldValue } = event;
    const field = column.getColId();
    if (data && field && newValue !== oldValue) {
      setIsDirty(true);
      setRows(prevRows =>
        prevRows.map(row => (row.skuId === data.skuId ? { ...row, [field]: newValue } : row))
      );
    }
  };

  const handleSaveDraft = async () => {
    setSaving(true);
    // Simulate save draft
    setTimeout(() => {
      setIsDirty(false);
      setSaving(false);
      toast.success(t("inbound.qc.detail.toasts.saveSuccess", "Draft saved successfully"));
    }, 500);
  };

  const handleComplete = async () => {
    setSaving(true);
    // Simulate complete
    setTimeout(() => {
      setIsDirty(false);
      setSaving(false);
      toast.success(t("inbound.qc.detail.toasts.completeSuccess", "QC completed successfully"));
      navigate("/inbound/qc");
    }, 500);
  };

  const handleBack = () => {
    navigate("/inbound/qc");
  };

  const columnDefs = useMemo<ColDef<QcItemRow>[]>(
    () => [
      {
        field: "skuCode",
        headerName: t("inbound.qc.detail.columns.skuCode", "SKU Code"),
        pinned: "left",
        width: 140,
      },
      {
        field: "skuName",
        headerName: t("inbound.qc.detail.columns.skuName", "SKU Name"),
        width: 200,
        tooltipField: "skuName",
      },
      {
        field: "totalQty",
        headerName: t("inbound.qc.detail.columns.totalQty", "Total Qty"),
        width: 120,
        type: "numericColumn",
        valueFormatter: (params) => params.value?.toLocaleString() || "0",
      },
      {
        field: "passQty",
        headerName: t("inbound.qc.detail.columns.passQty", "Pass Qty"),
        width: 130,
        editable: !isReadOnly,
        cellEditor: "agNumberCellEditor",
        cellEditorParams: {
          min: 0,
        },
        type: "numericColumn",
        cellClass: !isReadOnly ? "bg-emerald-500/10 font-semibold text-emerald-600" : "font-semibold text-emerald-600",
        valueFormatter: (params) => params.value?.toLocaleString() || "0",
      },
      {
        field: "failQty",
        headerName: t("inbound.qc.detail.columns.failQty", "Fail Qty"),
        width: 130,
        editable: !isReadOnly,
        cellEditor: "agNumberCellEditor",
        cellEditorParams: {
          min: 0,
        },
        type: "numericColumn",
        cellClass: !isReadOnly ? "bg-rose-500/10 font-semibold text-rose-600" : "font-semibold text-rose-600",
        valueFormatter: (params) => params.value?.toLocaleString() || "0",
      },
      {
        field: "notes",
        headerName: t("inbound.qc.detail.columns.notes", "Notes"),
        flex: 1,
        minWidth: 200,
        editable: !isReadOnly,
        cellClass: !isReadOnly ? "bg-primary/5" : "",
      },
    ],
    [t, isReadOnly]
  );

  const statusBadge = useMemo(() => {
    if (isReadOnly) {
      return (
        <Badge variant="outline" className="bg-emerald-500/10 text-emerald-500 border-emerald-500/20">
          {t("inbound.po.status.completed")}
        </Badge>
      );
    }
    return (
      <Badge variant="outline" className="bg-purple-500/10 text-purple-500 border-purple-500/20 animate-pulse">
        {t("inbound.po.status.receiving")}
      </Badge>
    );
  }, [isReadOnly, t]);

  if (loading) {
    return (
      <div className="h-full w-full flex flex-col items-center justify-center bg-card border rounded-xl p-8 space-y-3">
        <Loader2 className="size-8 text-primary animate-spin" />
        <p className="text-xs text-muted-foreground">{t("common.loading")}</p>
      </div>
    );
  }

  const orderNumber = qcTask?.inboundOrderNumber || "-";
  const receiptNumber = qcTask?.receiptNumber || t("inbound.qc.detail.title", "QC Detail");

  return (
    <>
      <div className="h-full w-full flex flex-col overflow-hidden bg-card border rounded-xl shadow-sm">
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 border-b bg-secondary/10">
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="icon-xs"
              onClick={handleBack}
              className="cursor-pointer"
              title={t("inbound.qc.detail.actions.back", "Back")}
            >
              <ArrowLeft className="size-4" />
            </Button>
            <div>
              <h2 className="text-sm font-bold text-foreground flex items-center gap-2">
                {receiptNumber}
                {statusBadge}
                {isDirty && <Badge variant="secondary" className="ml-2 bg-warning/20 text-warning border-warning/30">Unsaved</Badge>}
              </h2>
              <p className="text-[10px] text-muted-foreground mt-0.5">
                {t("inbound.receive.detail.poCode")}: <span className="font-semibold text-foreground">{orderNumber}</span>
                {" | "}
                {t("inbound.receive.detail.warehouse")}: <span className="font-semibold text-foreground">{selectedWarehouse?.name || "-"}</span>
              </p>
            </div>
          </div>
        </div>

        {error && (
          <div className="mx-4 mt-4 p-3 bg-destructive/10 border border-destructive/20 text-destructive text-xs rounded-lg">
            {error}
          </div>
        )}

        <div className="flex-1 min-h-0 w-full overflow-hidden relative p-4">
          <div className="w-full h-full">
            <AgGridReact
              rowData={rows}
              columnDefs={columnDefs}
              theme={gridTheme}
              getRowId={(params) => params.data.skuId}
              onCellValueChanged={onCellValueChanged}
              defaultColDef={{
                resizable: true,
                sortable: true,
                filter: false,
                minWidth: 100,
              }}
            />
          </div>
        </div>

        {!isReadOnly && (
          <div className="border-t p-4 flex justify-end gap-2 bg-secondary/10">
            <Button
              variant="outline"
              onClick={handleSaveDraft}
              disabled={saving || !isDirty}
              className="font-bold text-xs cursor-pointer flex items-center gap-1.5"
            >
              {saving ? <Loader2 className="size-3.5 animate-spin" /> : <Save className="size-3.5" />}
              {t("inbound.receive.detail.actions.saveDraft")}
            </Button>
            <Button
              variant="default"
              onClick={handleComplete}
              disabled={saving}
              className="font-bold text-xs cursor-pointer flex items-center gap-1.5"
            >
              {saving ? <Loader2 className="size-3.5 animate-spin" /> : <Check className="size-3.5" />}
              {t("inbound.receive.detail.actions.complete")}
            </Button>
          </div>
        )}
      </div>

      <Dialog open={blocker.state === "blocked"} onOpenChange={(open) => {
        if (!open && blocker.state === "blocked") {
          blocker.reset();
        }
      }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="size-5 text-warning" />
              {t("inbound.receive.detail.unsaved.title", "Unsaved Changes")}
            </DialogTitle>
            <DialogDescription>
              {t("inbound.receive.detail.unsaved.message", "You have unsaved changes. Are you sure you want to leave? Your changes will be lost.")}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => blocker.state === "blocked" && blocker.reset()}>
              {t("common.button.cancel", "Cancel")}
            </Button>
            <Button variant="destructive" onClick={() => blocker.state === "blocked" && blocker.proceed()}>
              {t("inbound.receive.detail.unsaved.confirm", "Leave without saving")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
