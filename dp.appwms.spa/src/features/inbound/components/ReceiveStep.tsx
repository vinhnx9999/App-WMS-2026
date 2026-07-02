import React from "react";
import { useTranslation } from "react-i18next";
import { Layers, ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { InboundOrderDto } from "../models/inbound.model";

interface ReceiveStepProps {
  onNext: () => void;
  selectedOrder: InboundOrderDto | null;
}

export const ReceiveStep: React.FC<ReceiveStepProps> = ({ onNext, selectedOrder }) => {
  const { t } = useTranslation();

  return (
    <div className="h-full w-full flex items-center justify-center bg-card border rounded-xl p-8">
      <div className="text-center max-w-md space-y-4">
        <Layers className="size-12 mx-auto text-primary/70 animate-pulse" />
        <h3 className="text-sm font-bold uppercase tracking-wider text-foreground">
          {t("inbound.tabs.receive")}
        </h3>
        {selectedOrder ? (
          <div className="p-4 bg-secondary/30 border border-border rounded-lg text-left text-xs space-y-2 text-foreground max-w-sm w-full mx-auto">
            <p className="font-bold text-sm text-foreground mb-1 border-b pb-1">
              Đơn hàng nhận: {selectedOrder.orderNumber}
            </p>
            <div className="grid grid-cols-2 gap-1 text-muted-foreground">
              <span>Nhà cung cấp:</span>
              <span className="text-foreground font-medium text-right">{selectedOrder.supplierName}</span>
              <span>Tổng giá trị:</span>
              <span className="text-foreground font-medium text-right">{selectedOrder.totalValue.toLocaleString()} VND</span>
              <span>Mặt hàng:</span>
              <span className="text-foreground font-medium text-right">{selectedOrder.itemsCount} dòng</span>
            </div>
          </div>
        ) : (
          <p className="text-xs text-muted-foreground leading-relaxed">
            {t("inbound.receive.description")}
          </p>
        )}
        <Button size="sm" onClick={onNext} className="font-bold text-xs gap-1 cursor-pointer">
          {t("inbound.receive.nextAction")} <ArrowRight className="size-3.5" />
        </Button>
      </div>
    </div>
  );
};

