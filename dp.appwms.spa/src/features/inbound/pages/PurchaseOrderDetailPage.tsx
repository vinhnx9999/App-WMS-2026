import { useEffect, useMemo, useRef, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AgGridReact } from "ag-grid-react";
import type { ColDef } from "ag-grid-community";
import { toast } from "sonner";
import { ArrowLeft, Layers, Loader2, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";
import { useWarehouseStore } from "@/store/warehouse-store";
import { inboundService } from "../services/inbound.service";
import { InboundStatus, type GetInboundByIdResponse } from "../models/inbound.model";
import { useReceiveWork } from "../hooks/useReceiveWork";

export default function PurchaseOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const gridTheme = useAgGridTheme();
  const gridRef = useRef<AgGridReact>(null);
  const { selectedWarehouse } = useWarehouseStore();

  const [detail, setDetail] = useState<GetInboundByIdResponse | null>(null);
  const [loading, setLoading] = useState(!!id);
  const [error, setError] = useState<string | null>(null);

  // Hook to handle receipt creation  
  const { initFromOrder, saveDraft, saving } = useReceiveWork();

 const fetchDetail = async (orderId: string) => { 
  await Promise.resolve();
  setLoading(prev => prev ? prev : true); 
  setError(null);
  try {
    const response = await inboundService.getInboundOrderById(orderId);
    if (response.success && response.data) {
      setDetail(response.data);
    } else {
      setError(response.message || "Failed to fetch details");
    }
  } catch (err) {
    setError("An unexpected error occurred while fetching details");
    console.error("Error fetching purchase order details:", err);
  } finally {
    setLoading(false); // Việc set state bất đồng bộ sau await thì hoàn toàn hợp lệ
  }
};

  useEffect(() => {
    if (id) {
      fetchDetail(id);
    }
  }, [id]);

  const handleReceive = async () => {
    if (!id || !selectedWarehouse?.id) return;
    
    // First initialize the receive work with this PO
    await initFromOrder(id);
    // Then create a draft receipt
    const draftId = await saveDraft(selectedWarehouse.id);
    if (draftId) {
      toast.success(t("inbound.receive.detail.toasts.saveSuccess", "Draft receipt created successfully"));
      // Refresh to see updated status if any
      fetchDetail(id);
    } else {
      toast.error(t("inbound.receive.detail.toasts.saveFailed", "Failed to create draft receipt"));
    }
  };

  const columnDefs = useMemo<ColDef[]>(
    () => [
      {
        field: "skuCode",
        headerName: t("inbound.receive.detail.columns.skuCode", "SKU Code"),
        pinned: "left",
        width: 140,
      },
      {
        field: "skuName",
        headerName: t("inbound.receive.detail.columns.skuName", "SKU Name"),
        width: 250,
      },
      {
        field: "quantity",
        headerName: t("inbound.receive.detail.columns.expectedQty", "Expected Qty"),
        width: 130,
        type: "numericColumn",
      },
      {
        field: "receivedQuantity",
        headerName: t("inbound.receive.detail.columns.receivedQty", "Received Qty"),
        width: 130,
        type: "numericColumn",
        cellClass: "font-semibold text-primary",
      },
      {
        headerName: t("inbound.po.columns.remainingQty", "Remaining Qty"),
        width: 130,
        type: "numericColumn",
        valueGetter: (params) => {
          if (!params.data) return 0;
          return Math.max(0, params.data.quantity - params.data.receivedQuantity);
        },
        cellClass: "font-semibold text-warning",
      },
      {
        field: "supplierName",
        headerName: t("inbound.receive.detail.columns.supplier", "Supplier"),
        width: 160,
      },
      {
        field: "note",
        headerName: t("inbound.receive.detail.columns.notes", "Notes"),
        flex: 1,
        minWidth: 150,
      },
    ],
    [t]
  );

  if (loading) {
    return (
      <div className="h-full w-full flex flex-col items-center justify-center bg-card border rounded-xl p-8 space-y-3">
        <Loader2 className="size-8 text-primary animate-spin" />
        <p className="text-xs text-muted-foreground">{t("common.loading")}</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-full w-full flex flex-col items-center justify-center bg-card border rounded-xl p-8">
        <AlertTriangle className="size-10 text-destructive mb-4" />
        <p className="text-sm font-semibold text-destructive">{error}</p>
        <Link to="/inbound/po" className="mt-4">
          <Button variant="outline">{t("common.button.cancel", "Back")}</Button>
        </Link>
      </div>
    );
  }

  if (!detail) return null;

  const isReceivable =
    detail.status === InboundStatus.Approved ||
    detail.status === InboundStatus.Receiving;

  return (
    <div className="h-full w-full flex flex-col overflow-hidden bg-card border rounded-xl shadow-sm">
      {/* Header Area */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 border-b bg-secondary/10">
        <div className="flex items-center gap-3">
          <Link to="/inbound/po">
            <Button variant="ghost" size="icon-xs" title={t("common.button.cancel")}>
              <ArrowLeft className="size-4" />
            </Button>
          </Link>
          <div>
            <h2 className="text-lg font-bold text-foreground flex items-center gap-2">
              {t("inbound.po.detail.poLabel", { number: detail.orderNumber })}
              <Badge variant="outline" className={
                detail.status === InboundStatus.Approved ? "bg-primary/10 text-primary border-primary/20" :
                  detail.status === InboundStatus.Receiving ? "bg-accent/15 text-foreground border-accent/25" :
                    detail.status === InboundStatus.Completed ? "bg-success/10 text-success border-success/20" :
                      "bg-destructive/10 text-destructive border-destructive/20"
              }>
                {detail.status === InboundStatus.Approved && t("inbound.po.status.approved")}
                {detail.status === InboundStatus.Receiving && t("inbound.po.status.receiving")}
                {detail.status === InboundStatus.Completed && t("inbound.po.status.completed")}
                {detail.status === InboundStatus.Cancelled && t("inbound.po.status.cancelled")}
              </Badge>
            </h2>
            <p className="text-xs text-muted-foreground mt-0.5 flex gap-4">
              <span>{t("inbound.po.detail.expectedDate")}: <span className="font-semibold text-foreground">{detail.expectedDate ? new Date(detail.expectedDate).toLocaleDateString("vi-VN") : "-"}</span></span>
              <span>{t("inbound.po.detail.totalItems")}: <span className="font-semibold text-foreground">{detail.items?.length || 0}</span></span>
            </p>
          </div>
        </div>
        <Button
          className="h-8 text-xs font-semibold cursor-pointer"
          disabled={!isReceivable || saving}
          onClick={handleReceive}
        >
          {saving ? <Loader2 className="size-3.5 mr-1 animate-spin" /> : <Layers className="size-3.5 mr-1" />}
          {t("inbound.po.detail.receiveButton", "Receive")}
        </Button>
      </div>

      {/* Grid Area */}
      <div className="flex-1 min-h-0 w-full overflow-hidden relative p-4">
        <div className="w-full h-full">
          <AgGridReact
            ref={gridRef}
            rowData={detail.items}
            columnDefs={columnDefs}
            theme={gridTheme}
            getRowId={(params) => params.data.skuId}
            defaultColDef={{
              resizable: true,
              sortable: true,
              filter: true,
              minWidth: 100,
            }}
          />
        </div>
      </div>
    </div>
  );
}
