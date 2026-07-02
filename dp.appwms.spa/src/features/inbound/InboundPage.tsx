import { useTransition } from "react";
import { useTranslation } from "react-i18next";
import { InboundStepper } from "./components/InboundStepper";
import { INBOUND_STEPS, type WorkflowStep } from "./models/inbound.model";
import { useInboundWorkflow } from "../../hooks/use-inbound-workflow";
import { PurchaseOrderStep } from "./components/PurchaseOrderStep";
import { ReceiveStep } from "./components/ReceiveStep";
import { QcStep } from "./components/QcStep";
import { PutawayStep } from "./components/PutawayStep/PutawayStep";
import { Spinner } from "@/components/ui/spinner";

export default function InboundPage() {
  const { t } = useTranslation();
  const [isPendingTransition, startTransition] = useTransition();
  const { enabledSteps, activeStep, setActiveStep, isLoading, isDirectMode } = useInboundWorkflow();

  // Navigate to the next enabled step
  const handleNextStep = (current: WorkflowStep) => {
    const currentIndex = enabledSteps.indexOf(current);
    if (currentIndex !== -1 && currentIndex < enabledSteps.length - 1) {
      const nextStep = enabledSteps[currentIndex + 1];
      startTransition(() => {
        setActiveStep(nextStep);
      });
    }
  };

  if (isLoading) {
    return (
      <div className="h-full w-full flex items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-2">
          <Spinner className="size-8 text-primary" />
          <span className="text-xs text-muted-foreground font-medium">{t("common.loading")}...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full flex flex-col overflow-hidden bg-background p-4 gap-4">
      {/* Visual Stepper */}
      <div className="flex items-center justify-between gap-4 shrink-0">
        <InboundStepper
          enabledSteps={enabledSteps}
          activeStep={activeStep}
          onStepChange={(step) => startTransition(() => setActiveStep(step))}
        />
      </div>

      {/* Main Workspaces based on Active Step */}
      <div className="flex-1 min-h-0 w-full">
        {isPendingTransition ? (
          <div className="h-full w-full flex items-center justify-center">
            <Spinner className="size-8 text-primary" />
          </div>
        ) : activeStep === INBOUND_STEPS.PO ? (
          <PurchaseOrderStep onNext={() => handleNextStep(INBOUND_STEPS.PO)} />
        ) : activeStep === INBOUND_STEPS.RECEIVE ? (
          <ReceiveStep onNext={() => handleNextStep(INBOUND_STEPS.RECEIVE)} />
        ) : activeStep === INBOUND_STEPS.QC ? (
          <QcStep onNext={() => handleNextStep(INBOUND_STEPS.QC)} />
        ) : (
          <PutawayStep isDirectMode={isDirectMode} />
        )}
      </div>
    </div>
  );
}
