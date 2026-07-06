import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, PagedResult } from "@/models/response";
import { type InboundOrderDto, type GetInboundByIdResponse } from "../models/inbound.model";
import { type SearchQueryParams } from "@/models/search.model";

export const inboundService = {
  searchInboundOrders: async (
    params: SearchQueryParams
  ): Promise<ApiResponse<PagedResult<InboundOrderDto>>> => {
    try {
      const response = await apiClient.post<ApiResponse<PagedResult<InboundOrderDto>>>(
        ENDPOINTS.INBOUND.SEARCH,
        params.searchObjects,
        {
          params: {
            page: params.page,
            limit: params.limit,
          },
        }
      );
      return response.data;
    } catch (error) {
      console.error("Error searching inbound orders:", error);
      throw error;
    }
  },

  getInboundOrderById: async (
    id: string
  ): Promise<ApiResponse<GetInboundByIdResponse>> => {
    try {
      const response = await apiClient.get<ApiResponse<GetInboundByIdResponse>>(
        ENDPOINTS.INBOUND.GET_BY_ID(id)
      );
      return response.data;
    } catch (error) {
      console.error(`Error fetching inbound order details for id ${id}:`, error);
      throw error;
    }
  },
};


