import { useState, useEffect, useMemo } from "react";
import { useWarehouseStore } from "@/store/warehouse-store";
import { INBOUND_STEPS, type WorkflowStep } from "../features/inbound/models/inbound.model";
import { mapApiStepToWorkflowStep } from "../features/inbound/models/inbound-workflow-config.model";
import { inboundWorkflowService } from "../features/inbound/services/inbound-workflow.service";

export const useInboundWorkflow = () => {
  const { selectedWarehouse } = useWarehouseStore();
  const [enabledSteps, setEnabledSteps] = useState<WorkflowStep[]>([]);
  const [activeStep, setActiveStep] = useState<WorkflowStep>(INBOUND_STEPS.PO);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!selectedWarehouse?.id) {
      setEnabledSteps([INBOUND_STEPS.PO, INBOUND_STEPS.RECEIVE, INBOUND_STEPS.QC, INBOUND_STEPS.PUTAWAY]);
      setActiveStep(INBOUND_STEPS.PO);
      return;
    }

    const fetchConfig = async () => {
      setIsLoading(true);
      try {
        const result = await inboundWorkflowService.getWorkflowConfig(selectedWarehouse.id);
        if (result.success && result.data && result.data.steps) {
          const mappedSteps = result.data.steps
            .map(s => mapApiStepToWorkflowStep(s.stepType))
            .filter((s): s is WorkflowStep => s !== null);

          if (mappedSteps.length > 0) {
            setEnabledSteps(mappedSteps);
            setActiveStep(mappedSteps[0]);
            return;
          }
        }
        // Fallback to default steps if config is invalid or empty
        setEnabledSteps([INBOUND_STEPS.PO, INBOUND_STEPS.RECEIVE, INBOUND_STEPS.QC, INBOUND_STEPS.PUTAWAY]);
        setActiveStep(INBOUND_STEPS.PO);
      } catch (err) {
        console.error("Failed to load inbound workflow config", err);
        // Fallback to default steps on error
        setEnabledSteps([INBOUND_STEPS.PO, INBOUND_STEPS.RECEIVE, INBOUND_STEPS.QC, INBOUND_STEPS.PUTAWAY]);
        setActiveStep(INBOUND_STEPS.PO);
      } finally {
        setIsLoading(false);
      }
    };

    fetchConfig();
  }, [selectedWarehouse?.id]);

  const isDirectMode = useMemo(() => {
    return !enabledSteps.includes(INBOUND_STEPS.PO) &&
      !enabledSteps.includes(INBOUND_STEPS.RECEIVE) &&
      !enabledSteps.includes(INBOUND_STEPS.QC);
  }, [enabledSteps]);

  return {
    enabledSteps,
    activeStep,
    setActiveStep,
    isLoading,
    isDirectMode,
  };
};
