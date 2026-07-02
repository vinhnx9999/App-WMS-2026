import React from "react";
import { useTranslation } from "react-i18next";
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, SheetFooter } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Building2, Layers, ArrowRight } from "lucide-react";
import { InboundStatus, type InboundOrderDto } from "../models/inbound.model";

interface PurchaseOrderDetailSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  order: InboundOrderDto | null;
  onReceive: () => void;
}

export const PurchaseOrderDetailSheet: React.FC<PurchaseOrderDetailSheetProps> = ({
  open,
  onOpenChange,
  order,
  onReceive,
}) => {
  const { t } = useTranslation();

  if (!order) return null;

  const isReceivable =
    order.status === InboundStatus.Pending ||
    order.status === InboundStatus.Approved ||
    order.status === InboundStatus.Receiving;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-md w-full flex flex-col h-full p-6" side="right">
        <SheetHeader className="mb-4">
          <SheetTitle className="flex items-center gap-2 text-lg font-bold">
            <span>Đơn PO: {order.orderNumber}</span>
          </SheetTitle>
          <SheetDescription>
            Chi tiết mặt hàng và số lượng cần nhập kho.
          </SheetDescription>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto space-y-5 text-xs text-foreground">
          {/* Order Info Cards */}
          <div className="grid grid-cols-2 gap-3 p-3 bg-secondary/20 rounded-lg border border-border">
            <div className="space-y-1">
              <span className="text-muted-foreground block font-medium">Nhà cung cấp</span>
              <span className="font-semibold text-foreground flex items-center gap-1">
                <Building2 className="size-3.5 text-muted-foreground shrink-0" />
                <span className="truncate">{order.supplierName}</span>
              </span>
            </div>
            <div className="space-y-1">
              <span className="text-muted-foreground block font-medium">Ngày dự kiến</span>
              <span className="font-semibold text-foreground">
                {order.expectedDate
                  ? new Date(order.expectedDate).toLocaleDateString("vi-VN")
                  : "-"}
              </span>
            </div>
            <div className="space-y-1">
              <span className="text-muted-foreground block font-medium">Tổng giá trị</span>
              <span className="font-semibold text-foreground text-primary">
                {order.totalValue.toLocaleString("vi-VN")} VND
              </span>
            </div>
            <div className="space-y-1 flex flex-col justify-center">
              <span className="text-muted-foreground block font-medium">Trạng thái</span>
              <div className="mt-1">
                <Badge variant="outline" className={
                  order.status === InboundStatus.Pending ? "bg-amber-500/10 text-amber-500 border-amber-500/20" :
                  order.status === InboundStatus.Approved ? "bg-sky-500/10 text-sky-500 border-sky-500/20" :
                  order.status === InboundStatus.Receiving ? "bg-purple-500/10 text-purple-500 border-purple-500/20" :
                  order.status === InboundStatus.Completed ? "bg-emerald-500/10 text-emerald-500 border-emerald-500/20" :
                  "bg-rose-500/10 text-rose-500 border-rose-500/20"
                }>
                  {order.status === InboundStatus.Pending && t("inbound.po.status.pending")}
                  {order.status === InboundStatus.Approved && t("inbound.po.status.approved")}
                  {order.status === InboundStatus.Receiving && t("inbound.po.status.receiving")}
                  {order.status === InboundStatus.Completed && t("inbound.po.status.completed")}
                  {order.status === InboundStatus.Cancelled && t("inbound.po.status.cancelled")}
                </Badge>
              </div>
            </div>
          </div>

          {/* Items List */}
          <div className="space-y-2">
            <span className="text-sm font-bold text-foreground">Danh sách mặt hàng</span>
            <div className="border border-border rounded-lg overflow-hidden">
              <table className="w-full text-[11px] text-left text-foreground">
                <thead className="bg-secondary/40 text-muted-foreground font-semibold border-b">
                  <tr>
                    <th className="p-2">Mã SKU</th>
                    <th className="p-2">Tên sản phẩm</th>
                    <th className="p-2 text-right">SL Đặt</th>
                    <th className="p-2 text-right">SL Nhận</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {order.items && order.items.length > 0 ? (
                    order.items.map((item) => (
                      <tr key={item.skuCode} className="hover:bg-muted/20">
                        <td className="p-2 font-mono text-[10px]">{item.skuCode}</td>
                        <td className="p-2 font-medium">{item.skuName}</td>
                        <td className="p-2 text-right">{item.quantity}</td>
                        <td className="p-2 text-right font-medium text-primary">
                          {item.receivedQuantity}
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan={4} className="p-4 text-center text-muted-foreground">
                        Không có sản phẩm nào.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <SheetFooter className="border-t pt-4">
          <Button
            className="w-full font-bold text-xs cursor-pointer flex items-center justify-center gap-1.5 animate-shimmer"
            disabled={!isReceivable}
            onClick={onReceive}
          >
            <Layers className="size-3.5" />
            Nhận Hàng Cho Đơn Này
            <ArrowRight className="size-3.5 ml-1" />
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
};
