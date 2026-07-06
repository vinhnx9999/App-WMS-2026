import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, PagedResult } from "@/models/response";
import {
  type InboundOrderDto,
  type GetInboundByIdResponse,
  type CreateReceiptRequest,
  type InboundReceiptDto,
  type GetInboundReceiptByIdResponse,
} from "../models/inbound.model";
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

  searchInboundReceipts: async (
    params: SearchQueryParams
  ): Promise<ApiResponse<PagedResult<InboundReceiptDto>>> => {
    try {
      const response = await apiClient.post<ApiResponse<PagedResult<InboundReceiptDto>>>(
        ENDPOINTS.INBOUND.RECEIPTS.SEARCH,
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
      console.error("Error searching inbound receipts:", error);
      throw error;
    }
  },

  getInboundReceiptById: async (
    id: string
  ): Promise<ApiResponse<GetInboundReceiptByIdResponse>> => {
    try {
      const response = await apiClient.get<ApiResponse<GetInboundReceiptByIdResponse>>(
        ENDPOINTS.INBOUND.RECEIPTS.GET_BY_ID(id)
      );
      return response.data;
    } catch (error) {
      console.error(`Error fetching inbound receipt details for id ${id}:`, error);
      throw error;
    }
  },

  createInboundReceipt: async (
    request: CreateReceiptRequest
  ): Promise<ApiResponse<string>> => {
    try {
      const response = await apiClient.post<ApiResponse<string>>(
        ENDPOINTS.INBOUND.RECEIPTS.CREATE,
        request
      );
      return response.data;
    } catch (error) {
      console.error("Error creating inbound receipt:", error);
      throw error;
    }
  },

  completeInboundReceipt: async (
    id: string
  ): Promise<ApiResponse<void>> => {
    try {
      const response = await apiClient.put<ApiResponse<void>>(
        ENDPOINTS.INBOUND.RECEIPTS.COMPLETE(id)
      );
      return response.data;
    } catch (error) {
      console.error(`Error completing inbound receipt for id ${id}:`, error);
      throw error;
    }
  },

  forceCompleteInboundOrder: async (
    id: string
  ): Promise<ApiResponse<void>> => {
    try {
      const response = await apiClient.put<ApiResponse<void>>(
        ENDPOINTS.INBOUND.FORCE_COMPLETE(id)
      );
      return response.data;
    } catch (error) {
      console.error(`Error force completing inbound order for id ${id}:`, error);
      throw error;
    }
  },
};



