import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, PagedResult } from "@/models/response";
import type { InboundOrderDto, SearchInboundOrdersParams } from "../models/inbound.model";

export const inboundService = {
  searchInboundOrders: async (
    params: SearchInboundOrdersParams
  ): Promise<ApiResponse<PagedResult<InboundOrderDto>>> => {
    try {
      const response = await apiClient.get<ApiResponse<PagedResult<InboundOrderDto>>>(
        ENDPOINTS.INBOUND.SEARCH,
        {
          params,
        }
      );
      return response.data;
    } catch (error) {
      console.error("Error searching inbound orders:", error);
      throw error;
    }
  },
};
