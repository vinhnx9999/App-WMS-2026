import { useTranslation } from "react-i18next";
import { ArrowLeft } from "lucide-react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";

export default function PurchaseOrderCreatePage() {
  const { t } = useTranslation();

  return (
    <div className="h-full w-full flex flex-col overflow-hidden bg-card border rounded-xl shadow-sm">
      <div className="flex items-center gap-4 p-4 border-b bg-secondary/10">
        <Link to="/inbound/po">
          <Button variant="ghost" size="icon-xs" title={t("common.button.cancel")}>
            <ArrowLeft className="size-4" />
          </Button>
        </Link>
        <h2 className="text-lg font-bold">{t("inbound.po.create.title", "Create Purchase Order")}</h2>
      </div>
      <div className="flex-1 p-6 flex items-center justify-center text-muted-foreground">
        <p>{t("inbound.po.create.comingSoon", "PO Creation Form is under construction.")}</p>
      </div>
    </div>
  );
}
