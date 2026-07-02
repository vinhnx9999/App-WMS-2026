import React from "react";
import { useTranslation } from "react-i18next";
import { ArrowRight, Check } from "lucide-react";
import type { WorkflowStep } from "../models/inbound.model";

interface InboundStepperProps {
  enabledSteps: WorkflowStep[];
  activeStep: WorkflowStep;
  onStepChange: (step: WorkflowStep) => void;
}

export const InboundStepper: React.FC<InboundStepperProps> = ({
  enabledSteps,
  activeStep,
  onStepChange,
}) => {
  const { t } = useTranslation();

  return (
    <div className="flex items-center justify-between bg-card rounded-xl border border-border p-3 shadow-sm shrink-0">
      <div className="flex items-center gap-1 overflow-x-auto scrollbar-none">
        {enabledSteps.map((step, idx) => {
          const isActive = activeStep === step;
          const isCompleted = enabledSteps.indexOf(activeStep) > idx;

          return (
            <div key={step} className="flex items-center">
              <button
                type="button"
                onClick={() => onStepChange(step)}
                className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-bold transition-all duration-200 cursor-pointer ${isActive
                  ? "bg-primary text-primary-foreground shadow-sm scale-[1.02]"
                  : isCompleted
                    ? "text-emerald-600 dark:text-emerald-500 hover:bg-muted/50"
                    : "text-muted-foreground hover:bg-muted/50"
                  }`}
              >
                <span
                  className={`size-5 rounded-full flex items-center justify-center border text-[10px] ${isActive
                    ? "border-primary-foreground bg-primary-foreground text-primary"
                    : isCompleted
                      ? "border-emerald-500 bg-emerald-500 text-white"
                      : "border-muted-foreground"
                    }`}
                >
                  {isCompleted ? <Check className="size-3" /> : idx + 1}
                </span>
                {t(`inbound.tabs.${step}`)}
              </button>
              {idx < enabledSteps.length - 1 && (
                <ArrowRight className="size-3.5 mx-2 text-muted-foreground/40" />
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};
export default InboundStepper;
