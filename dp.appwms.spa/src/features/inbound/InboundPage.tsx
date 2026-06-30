import { useState, useMemo, useTransition } from "react";
import { RefreshCw } from "lucide-react";
import { useWarehouseStore } from "@/store/warehouse-store";
import { InboundStepper } from "./components/InboundStepper";
import type { WorkflowStep } from "./types/inbound-types";
import { PurchaseOrderStep } from "./components/PurchaseOrderStep";
import { ReceiveStep } from "./components/ReceiveStep";
import { QcStep } from "./components/QcStep";
import { PutawayStep } from "./components/PutawayStep/PutawayStep";

export default function InboundPage() {
  const { selectedWarehouse } = useWarehouseStore();
  const [isPendingTransition, startTransition] = useTransition();

  // Workflow steps configuration (stored in localStorage per warehouse)
  const [enabledSteps, setEnabledSteps] = useState<WorkflowStep[]>(() => {
    const saved = localStorage.getItem(`inbound_steps_${selectedWarehouse?.id || "default"}`);
    return saved ? JSON.parse(saved) : ["po", "receive", "qc", "putaway"];
  });

  const [activeStep, setActiveStep] = useState<WorkflowStep>("po");

  // Check if we are in "Direct Putaway" mode (no preceding active steps)
  const isDirectMode = useMemo(() => {
    return !enabledSteps.includes("po") && !enabledSteps.includes("receive") && !enabledSteps.includes("qc");
  }, [enabledSteps]);

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

  return (
    <div className="h-full w-full flex flex-col overflow-hidden bg-background p-4 gap-4">
      {/* Visual Stepper & Configuration Panel */}
      <div className="flex items-center justify-between gap-4 shrink-0">
        <InboundStepper
          enabledSteps={enabledSteps}
          activeStep={activeStep}
          onStepChange={(step) => startTransition(() => setActiveStep(step))}
        />

        {/* Simulation switch for testing workflows (Will be replaced by API hook in the future) */}
        <div className="flex items-center gap-2 bg-card px-3 py-1.5 rounded-xl border border-border text-xs shadow-sm">
          <span className="text-muted-foreground font-medium">Chế độ kiểm thử:</span>
          <button
            onClick={() => {
              const newSteps: WorkflowStep[] = isDirectMode
                ? ["po", "receive", "qc", "putaway"]
                : ["putaway"];
              setEnabledSteps(newSteps);
              setActiveStep(isDirectMode ? "po" : "putaway");
              localStorage.setItem(`inbound_steps_${selectedWarehouse?.id || "default"}`, JSON.stringify(newSteps));
            }}
            className="px-2.5 py-1 bg-primary text-primary-foreground rounded-lg hover:bg-primary/90 cursor-pointer font-bold text-[10px] uppercase tracking-wider transition-all"
          >
            {isDirectMode ? "Kích hoạt 4 bước" : "Cất trực tiếp (Direct)"}
          </button>
        </div>
      </div>

      {/* Main Workspaces based on Active Step */}
      <div className="flex-1 min-h-0 w-full">
        {isPendingTransition ? (
          <div className="h-full w-full flex items-center justify-center">
            <RefreshCw className="size-8 animate-spin text-primary" />
          </div>
        ) : activeStep === "po" ? (
          <PurchaseOrderStep onNext={() => handleNextStep("po")} />
        ) : activeStep === "receive" ? (
          <ReceiveStep onNext={() => handleNextStep("receive")} />
        ) : activeStep === "qc" ? (
          <QcStep onNext={() => handleNextStep("qc")} />
        ) : (
          <PutawayStep isDirectMode={isDirectMode} />
        )}
      </div>
    </div>
  );
}
