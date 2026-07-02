import { INBOUND_STEPS, type WorkflowStep } from "./inbound.model";

export interface InboundWorkflowStepResponse {
  stepType: number;
  sequence: number;
  displayName: string;
}

export interface InboundWorkflowConfigResponse {
  id: string | null;
  warehouseId: string | null;
  supplierId: string | null;
  categoryId: string | null;
  allowOverReceive: boolean;
  overReceiveTolerancePercentage: number | null;
  steps: InboundWorkflowStepResponse[];
}

export const mapApiStepToWorkflowStep = (stepType: number): WorkflowStep | null => {
  switch (stepType) {
    case 0: return INBOUND_STEPS.PO;
    case 1: return INBOUND_STEPS.RECEIVE;
    case 2: return INBOUND_STEPS.QC;
    case 3: return INBOUND_STEPS.PUTAWAY;
    default: return null;
  }
};
