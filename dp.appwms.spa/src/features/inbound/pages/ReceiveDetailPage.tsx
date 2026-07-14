import { useEffect, useMemo, useRef, forwardRef, useImperativeHandle, useState  } from "react";
import { useParams, useNavigate, useBlocker } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, ICellEditorParams, CellValueChangedEvent } from "ag-grid-community";
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
import { useReceiveWork } from "../hooks/useReceiveWork";
import { ReceiptStatus, type ReceiveItemRow } from "../models/inbound.model";



// Custom HTML5 Date Cell Editor for Expiry Dates
const DateCellEditor = forwardRef((props: ICellEditorParams, ref) => {
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setTimeout(() => {
      inputRef.current?.focus();
    }, 50);
  }, []);

  useImperativeHandle(ref, () => ({
    getValue() {
      return inputRef.current?.value || null;
    },
    isPopup() {
      return false;
    }
  }));

  let initialValue = "";
  if (props.value) {
    try {
      const d = new Date(props.value);
      if (!isNaN(d.getTime())) {
        initialValue = d.toISOString().split("T")[0];
      }
    } catch {
      // Ignore parsing errors
    }
  }

  return (
    <input
      ref={inputRef}
      type="date"
      defaultValue={initialValue}
      className="w-full h-full px-2 border-0 bg-transparent focus:ring-0 focus:outline-none text-xs text-foreground"
    />
  );
});
DateCellEditor.displayName = "DateCellEditor";

export default function ReceiveDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const gridTheme = useAgGridTheme();
  const { selectedWarehouse } = useWarehouseStore();

  const [isDirty, setIsDirty] = useState(false);

  const {
    order,
    receipt,
    rows,
    loading,
    saving,
    error,
    initFromDraftReceipt,
    updateRow,
    saveDraft,
    completeReceipt,
  } = useReceiveWork();

  useEffect(() => {
    if (id) {
      initFromDraftReceipt(id);
    }
  }, [id, initFromDraftReceipt]);

  const isReadOnly = useMemo(() => {
    return receipt?.status === ReceiptStatus.Completed;
  }, [receipt]);

  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      isDirty && !isReadOnly && currentLocation.pathname !== nextLocation.pathname
  );

  const onCellValueChanged = (event: CellValueChangedEvent<ReceiveItemRow>) => {
    const { data, column, newValue, oldValue } = event;
    const field = column.getColId();
    if (data && field && newValue !== oldValue) {
      setIsDirty(true);
      updateRow(data.skuId, { [field]: newValue });
    }
  };

  const handleSaveDraft = async () => {
    if (!selectedWarehouse?.id) {
      toast.error(t("inbound.receive.detail.toasts.saveFailed", "Failed to save draft"));
      return;
    }
    const resultId = await saveDraft(selectedWarehouse.id);
    if (resultId) {
      setIsDirty(false);
      toast.success(t("inbound.receive.detail.toasts.saveSuccess", "Draft saved successfully"));
    } else {
      toast.error(t("inbound.receive.detail.toasts.saveFailed", "Failed to save draft"));
    }
  };

  const handleComplete = async () => {
    if (!selectedWarehouse?.id) {
      toast.error(t("inbound.receive.detail.toasts.completeFailed", "Failed to complete receipt"));
      return;
    }
    const success = await completeReceipt(selectedWarehouse.id);
    if (success) {
      setIsDirty(false);
      toast.success(t("inbound.receive.detail.toasts.completeSuccess", "Receipt completed successfully"));
      navigate("/inbound/receive");
    } else {
      toast.error(t("inbound.receive.detail.toasts.completeFailed", "Failed to complete receipt"));
    }
  };

  const handleBack = () => {
    navigate("/inbound/receive");
  };

  const columnDefs = useMemo<ColDef<ReceiveItemRow>[]>(
    () => [
      {
        field: "skuCode",
        headerName: t("inbound.receive.detail.columns.skuCode"),
        pinned: "left",
        width: 140,
      },
      {
        field: "skuName",
        headerName: t("inbound.receive.detail.columns.skuName"),
        width: 200,
        tooltipField: "skuName",
      },
      {
        field: "expectedQuantity",
        headerName: t("inbound.receive.detail.columns.expectedQty"),
        width: 120,
        type: "numericColumn",
        valueFormatter: (params) => params.value?.toLocaleString() || "0",
      },
      {
        field: "receivedQuantity",
        headerName: t("inbound.receive.detail.columns.receivedQty"),
        width: 130,
        editable: !isReadOnly,
        cellEditor: "agNumberCellEditor",
        cellEditorParams: {
          min: 0,
        },
        type: "numericColumn",
        cellClass: !isReadOnly ? "bg-primary/5 font-semibold text-primary" : "font-semibold",
        valueFormatter: (params) => params.value?.toLocaleString() || "0",
      },
      {
        field: "expiryDate",
        headerName: t("inbound.receive.detail.columns.expiryDate"),
        width: 150,
        editable: !isReadOnly,
        cellEditor: DateCellEditor,
        valueFormatter: (params) => {
          if (!params.value) return "-";
          try {
            return new Date(params.value).toLocaleDateString("vi-VN");
          } catch {
            return params.value;
          }
        },
        cellClass: !isReadOnly ? "bg-primary/5" : "",
      },
      {
        field: "lotNumber",
        headerName: t("inbound.receive.detail.columns.lotNumber"),
        width: 140,
        editable: !isReadOnly,
        cellClass: !isReadOnly ? "bg-primary/5" : "",
      },
      {
        field: "serialNumber",
        headerName: t("inbound.receive.detail.columns.serialNumber"),
        width: 140,
        editable: !isReadOnly,
        cellClass: !isReadOnly ? "bg-primary/5" : "",
      },
      {
        field: "supplierName",
        headerName: t("inbound.receive.detail.columns.supplier"),
        width: 160,
        valueFormatter: (params) => params.value || "-",
      },
      {
        field: "notes",
        headerName: t("inbound.receive.detail.columns.notes"),
        width: 180,
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

  const orderNumber = order?.orderNumber || receipt?.inboundOrderNumber || "-";
  const receiptNumber = receipt?.receiptNumber || t("inbound.receive.detail.title", "Receive Detail");

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
              title={t("inbound.receive.detail.actions.back")}
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
