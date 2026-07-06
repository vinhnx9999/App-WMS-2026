import React, { useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, SheetFooter } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Layers, ArrowRight, AlertTriangle } from "lucide-react";
import { InboundStatus, type InboundOrderDto, type GetInboundByIdResponse } from "../models/inbound.model";
import { inboundService } from "../services/inbound.service";

interface PurchaseOrderDetailSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  order: InboundOrderDto | null;
  onReceive: () => void;
}

const SkeletonLoader = () => (
  <div className="space-y-3">
    {[1, 2, 3].map((i) => (
      <div key={i} className="p-3.5 bg-card rounded-lg border border-border/40 animate-pulse space-y-2.5">
        <div className="h-4 bg-muted/60 rounded w-3/4"></div>
        <div className="flex gap-2">
          <div className="h-4.5 bg-muted/60 rounded w-1/4"></div>
          <div className="h-4.5 bg-muted/60 rounded w-1/3"></div>
        </div>
        <div className="h-3 bg-muted/40 rounded w-1/2"></div>
      </div>
    ))}
  </div>
);

export const PurchaseOrderDetailSheet: React.FC<PurchaseOrderDetailSheetProps> = ({
  open,
  onOpenChange,
  order,
  onReceive,
}) => {
  const { t } = useTranslation();
  const [detail, setDetail] = useState<GetInboundByIdResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchDetail = async (orderId: string) => {
    setLoading(true);
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
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (open && order?.id) {
      fetchDetail(order.id);
    } else {
      setDetail(null);
      setError(null);
    }
  }, [open, order?.id]);

  if (!order) return null;

  const isReceivable =
    order.status === InboundStatus.Approved ||
    order.status === InboundStatus.Receiving;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-md w-full flex flex-col h-full p-6" side="right">
        <SheetHeader className="mb-4">
          <SheetTitle className="flex items-center gap-2 text-lg font-bold">
            <span>{t("inbound.po.detail.poLabel", { number: order.orderNumber })}</span>
          </SheetTitle>
          <SheetDescription>
            {t("inbound.po.detail.description")}
          </SheetDescription>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto space-y-5 text-xs text-foreground pr-1">
          {/* Order Info Cards */}
          <div className="grid grid-cols-2 gap-3 p-3.5 bg-secondary/15 rounded-lg border border-border/80">
            <div className="space-y-1">
              <span className="text-muted-foreground block font-medium">{t("inbound.po.detail.totalItems")}</span>
              <span className="font-semibold text-foreground flex items-center gap-1">
                <Layers className="size-3.5 text-muted-foreground shrink-0" />
                <span>{t("inbound.po.detail.itemsCountText", { count: order.itemsCount })}</span>
              </span>
            </div>
            <div className="space-y-1">
              <span className="text-muted-foreground block font-medium">{t("inbound.po.detail.expectedDate")}</span>
              <span className="font-semibold text-foreground">
                {order.expectedDate
                  ? new Date(order.expectedDate).toLocaleDateString("vi-VN")
                  : "-"}
              </span>
            </div>
            <div className="space-y-1">
              <span className="text-muted-foreground block font-medium">{t("inbound.po.detail.totalValue")}</span>
              <span className="font-semibold text-primary">
                {order.totalValue.toLocaleString("vi-VN")}
              </span>
            </div>

            <div className="space-y-1 flex flex-col justify-center">
              <span className="text-muted-foreground block font-medium">{t("inbound.po.detail.statusLabel")}</span>
              <div className="mt-1">
                <Badge variant="outline" className={
                  order.status === InboundStatus.Approved ? "bg-primary/10 text-primary border-primary/20" :
                    order.status === InboundStatus.Receiving ? "bg-accent/15 text-foreground border-accent/25" :
                      order.status === InboundStatus.Completed ? "bg-success/10 text-success border-success/20" :
                        "bg-destructive/10 text-destructive border-destructive/20"
                }>
                  {order.status === InboundStatus.Approved && t("inbound.po.status.approved")}
                  {order.status === InboundStatus.Receiving && t("inbound.po.status.receiving")}
                  {order.status === InboundStatus.Completed && t("inbound.po.status.completed")}
                  {order.status === InboundStatus.Cancelled && t("inbound.po.status.cancelled")}
                </Badge>
              </div>
            </div>
          </div>

          {/* Details Section */}
          <div className="space-y-3 pt-2">
            <h3 className="font-bold text-foreground text-sm tracking-tight flex items-center justify-between border-b pb-1.5">
              <span>{t("inbound.po.detail.itemsTitle", { count: order.itemsCount })}</span>
            </h3>

            {loading && <SkeletonLoader />}

            {error && (
              <div className="p-4 bg-destructive/10 border border-destructive/20 rounded-lg flex flex-col items-center justify-center text-center gap-3">
                <AlertTriangle className="size-8 text-destructive shrink-0" />
                <div className="space-y-1">
                  <p className="font-semibold text-destructive">
                    {t("inbound.po.detail.loadFailed")}
                  </p>
                  <p className="text-[10px] text-muted-foreground">{error}</p>
                </div>
              </div>
            )}

            {!loading && !error && detail && (
              <div className="space-y-3">
                {detail.items.map((item) => {
                  const ratio = item.quantity > 0 ? (item.receivedQuantity / item.quantity) * 100 : 0;
                  return (
                    <div
                      key={item.skuId}
                      className="p-3.5 bg-card rounded-lg border border-border/80 shadow-sm flex flex-col gap-1.5 hover:border-border/100 hover:shadow-md transition-all"
                    >
                      <div className="flex justify-between items-start gap-2">
                        <h4 className="font-semibold text-foreground text-xs leading-snug">
                          {item.skuName || t("inbound.po.detail.unknownSku")}
                        </h4>
                      </div>

                      <div className="flex flex-wrap gap-1.5 items-center">
                        <Badge variant="secondary" className="px-1.5 py-0.5 text-[10px] font-mono tracking-tight shrink-0">
                          {item.skuCode}
                        </Badge>
                        {item.lotNumber && (
                          <Badge variant="outline" className="px-1.5 py-0.5 text-[10px] border-warning/30 bg-warning/5 text-warning font-medium shrink-0">
                            {t("inbound.po.detail.lotNo")}: {item.lotNumber}
                          </Badge>
                        )}
                        {item.expiryDate && (
                          <Badge variant="outline" className="px-1.5 py-0.5 text-[10px] border-primary/20 bg-primary/5 text-primary font-medium shrink-0">
                            {t("inbound.po.detail.expiry")}: {new Date(item.expiryDate).toLocaleDateString("vi-VN")}
                          </Badge>
                        )}
                      </div>

                      {item.supplierName && (
                        <div className="text-[10px] text-muted-foreground flex items-center gap-1 font-medium mt-0.5">
                          <span className="shrink-0">{t("inbound.po.detail.supplierLabel")}</span>
                          <span className="text-foreground truncate">{item.supplierName}</span>
                        </div>
                      )}

                      <div className="mt-1">
                        <div className="flex justify-between items-center text-[10px] text-muted-foreground mb-1">
                          <span>{t("inbound.po.detail.qtyProgress", { received: item.receivedQuantity, quantity: item.quantity })}</span>
                          <span className="font-semibold text-primary">{Math.round(ratio)}%</span>
                        </div>
                        <div className="w-full bg-secondary/80 rounded-full h-1.5">
                          <div
                            className={`h-1.5 rounded-full transition-all duration-300 ${ratio >= 100 ? "bg-success" : "bg-primary"
                              }`}
                            style={{ width: `${Math.min(ratio, 100)}%` }}
                          />
                        </div>
                      </div>

                      {item.note && (
                        <p className="text-[10px] text-muted-foreground bg-secondary/30 px-2 py-1 rounded italic mt-1 border-l-2 border-muted">
                          "{item.note}"
                        </p>
                      )}
                    </div>
                  );
                })}
                {detail.items.length === 0 && (
                  <p className="text-center text-muted-foreground py-6">{t("inbound.po.detail.noItems")}</p>
                )}
              </div>
            )}
          </div>
        </div>

        <SheetFooter className="border-t pt-4">
          <Button
            className="w-full font-bold text-xs cursor-pointer flex items-center justify-center gap-1.5"
            disabled={!isReceivable}
            onClick={onReceive}
          >
            <Layers className="size-3.5" />
            {t("inbound.po.detail.receiveButton")}
            <ArrowRight className="size-3.5 ml-1" />
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
};

