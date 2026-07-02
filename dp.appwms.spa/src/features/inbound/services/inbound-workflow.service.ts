import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/models/response";
import type { InboundWorkflowConfigResponse } from "../models/inbound-workflow-config.model";

export const inboundWorkflowService = {
  getWorkflowConfig: async (warehouseId: string): Promise<ApiResponse<InboundWorkflowConfigResponse>> => {
    try {
      const response = await apiClient.get<ApiResponse<InboundWorkflowConfigResponse>>(
        ENDPOINTS.WAREHOUSE.INBOUND_WORKFLOW_CONFIG,
        {
          params: { warehouseId }
        }
      );
      return response.data;
    } catch (error) {
      console.error("Error fetching inbound workflow config:", error);
      throw error;
    }
  }
};
