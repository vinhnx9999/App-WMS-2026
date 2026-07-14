import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { useInboundWorkflow } from "@/hooks/use-inbound-workflow";
import { INBOUND_STEPS } from "../models/inbound.model";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export default function DashboardPage() {
  const { t } = useTranslation();
  const { enabledSteps, isLoading } = useInboundWorkflow();

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6 w-full h-full p-4">
        <Skeleton className="h-10 w-48" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6 w-full h-full p-4">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{t("inbound.dashboard.title")}</h1>
        <p className="text-muted-foreground mt-2">{t("inbound.dashboard.description")}</p>
      </div>
      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {enabledSteps.includes(INBOUND_STEPS.PO) && (
          <Link to="/inbound/po" className="transition-all hover:-translate-y-1">
            <Card className="hover:border-primary/50 cursor-pointer h-full">
              <CardHeader className="pb-2">
                <CardTitle className="text-lg">{t("inbound.dashboard.purchaseOrders")}</CardTitle>
                <CardDescription>{t("inbound.dashboard.poDescription")}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="text-3xl font-bold text-primary">--</div>
                <p className="text-xs text-muted-foreground mt-1">{t("inbound.dashboard.poPending")}</p>
              </CardContent>
            </Card>
          </Link>
        )}

        {enabledSteps.includes(INBOUND_STEPS.RECEIVE) && (
          <Link to="/inbound/receive" className="transition-all hover:-translate-y-1">
            <Card className="hover:border-primary/50 cursor-pointer h-full">
              <CardHeader className="pb-2">
                <CardTitle className="text-lg">{t("inbound.dashboard.receiving")}</CardTitle>
                <CardDescription>{t("inbound.dashboard.receivingDescription")}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="text-3xl font-bold text-primary">--</div>
                <p className="text-xs text-muted-foreground mt-1">{t("inbound.dashboard.receivingActive")}</p>
              </CardContent>
            </Card>
          </Link>
        )}

        {enabledSteps.includes(INBOUND_STEPS.QC) && (
          <Link to="/inbound/qc" className="transition-all hover:-translate-y-1">
            <Card className="hover:border-primary/50 cursor-pointer h-full">
              <CardHeader className="pb-2">
                <CardTitle className="text-lg">{t("inbound.dashboard.qualityControl")}</CardTitle>
                <CardDescription>{t("inbound.dashboard.qcDescription")}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="text-3xl font-bold text-primary">--</div>
                <p className="text-xs text-muted-foreground mt-1">{t("inbound.dashboard.qcPending")}</p>
              </CardContent>
            </Card>
          </Link>
        )}

        {enabledSteps.includes(INBOUND_STEPS.PUTAWAY) && (
          <Link to="/inbound/putaway" className="transition-all hover:-translate-y-1">
            <Card className="hover:border-primary/50 cursor-pointer h-full">
              <CardHeader className="pb-2">
                <CardTitle className="text-lg">{t("inbound.dashboard.putaway")}</CardTitle>
                <CardDescription>{t("inbound.dashboard.putawayDescription")}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="text-3xl font-bold text-primary">--</div>
                <p className="text-xs text-muted-foreground mt-1">{t("inbound.dashboard.putawayPending")}</p>
              </CardContent>
            </Card>
          </Link>
        )}
      </div>
    </div>
  );
}
