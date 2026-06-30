import React from "react";
import { useTranslation } from "react-i18next";
import { CheckCircle2, ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";

interface QcStepProps {
  onNext: () => void;
}

export const QcStep: React.FC<QcStepProps> = ({ onNext }) => {
  const { t } = useTranslation();

  return (
    <div className="h-full w-full flex items-center justify-center bg-card border rounded-xl p-8">
      <div className="text-center max-w-md space-y-3">
        <CheckCircle2 className="size-12 mx-auto text-primary/70" />
        <h3 className="text-sm font-bold uppercase tracking-wider text-foreground">
          {t("inbound.tabs.qc")}
        </h3>
        <p className="text-xs text-muted-foreground leading-relaxed">
          {t("inbound.qc.description")}
        </p>
        <Button size="sm" onClick={onNext} className="font-bold text-xs gap-1 cursor-pointer">
          {t("inbound.qc.nextAction")} <ArrowRight className="size-3.5" />
        </Button>
      </div>
    </div>
  );
};
