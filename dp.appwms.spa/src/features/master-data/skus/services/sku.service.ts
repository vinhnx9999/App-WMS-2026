import type { SkuDto } from "../models/sku-dto.model";
import type { CreateSkuRequest } from "../models/create-sku-request";
import type { UpdateSkuRequest } from "../models/update-sku-request";

import type { ApiResponse, PagedResult } from "@/types/response";
import type { SearchSkusParams } from "../models/search-sku-param.model";
import { ENDPOINTS } from "@/api/endpoints";
import type { UpdateSkuImportRowRequest } from "../models/update-sku-import-request";
import apiClient from "@/api/api-client";



export const skuService = {

    /**
     * Search SKUs
     * @param params - Search parameters
     * @returns Promise of ApiResponse<PagedResult<SkuDto>>
     */
    searchSkus: async (params: SearchSkusParams): Promise<ApiResponse<PagedResult<SkuDto>>> => {
        try {
            debugger;
            const response = await apiClient.get<ApiResponse<PagedResult<SkuDto>>>(ENDPOINTS.SKU.SEARCH, {
                params
            });
            return response.data;
        } catch (error) {
            console.error("Error searching SKUs:", error);
            throw error;
        }
    },

    /**
     * Create SKU
     * @param data - Create SKU request
     * @returns Promise of ApiResponse<any>
     */
    createSku: async (data: CreateSkuRequest): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.post<ApiResponse<any>>(ENDPOINTS.SKU.SEARCH, data);
            return response.data;
        } catch (error) {
            console.error("Error creating SKU:", error);
            throw error;
        }
    },

    /**
     * Update SKU
     * @param id - SKU ID
     * @param data - Update SKU request (Name, GoodsNature, Description, Price)
     * @returns Promise of ApiResponse<any>
     */
    updateSku: async (id: string, data: UpdateSkuRequest): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.put<ApiResponse<any>>(`${ENDPOINTS.SKU.SEARCH}/${id}`, data);
            if (response.status === 204 || response.status === 200) {
                return {
                    success: true,
                    data: null,
                    message: null,
                    errors: null
                };
            }
            return response.data;
        } catch (error) {
            console.error("Error updating SKU:", error);
            throw error;
        }
    },

    /**
     * Delete SKU
     * @param id - SKU ID
     * @returns Promise of ApiResponse<any>
     */
    deleteSku: async (id: string): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.delete<ApiResponse<any>>(`${ENDPOINTS.SKU.SEARCH}/${id}`);
            if (response.status === 204 || response.status === 200) {
                return {
                    success: true,
                    data: null,
                    message: null,
                    errors: null
                };
            }
            return response.data;
        } catch (error) {
            console.error("Error deleting SKU:", error);
            throw error;
        }
    },

    /**
     * Create SKU Import Session
     */
    createImportSession: async (sourceFileName: string, rows: any[]): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.post<ApiResponse<any>>(`${ENDPOINTS.SKU.IMPORT_SESSION}/import/session`, {
                sourceFileName,
                rows
            });
            return response.data;
        } catch (error) {
            console.error("Error creating SKU import session:", error);
            throw error;
        }
    },

    /**
     * Confirm SKU Import Session
     */
    confirmImportSession: async (id: string): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.post<ApiResponse<any>>(`${ENDPOINTS.SKU.IMPORT_SESSION}/import/session/${id}/confirm`);
            return response.data;
        } catch (error) {
            console.error("Error confirming SKU import session:", error);
            throw error;
        }
    },

    /**
     * Cancel SKU Import Session
     */
    cancelImportSession: async (id: string): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.post<ApiResponse<any>>(`${ENDPOINTS.SKU.IMPORT_SESSION}/import/session/${id}/cancel`);
            return response.data;
        } catch (error) {
            console.error("Error cancelling SKU import session:", error);
            throw error;
        }
    },

    /**
     * Search SKU Import Sessions
     */
    searchImportSessions: async (params: { status?: string, page: number, limit: number }): Promise<ApiResponse<PagedResult<any>>> => {
        try {
            const response = await apiClient.get<ApiResponse<PagedResult<any>>>(`${ENDPOINTS.SKU.IMPORT_SESSION}/import/sessions`, {
                params
            });
            return response.data;
        } catch (error) {
            console.error("Error searching SKU import sessions:", error);
            throw error;
        }
    },

    /**
     * Get SKU Import Session details
     */
    getImportSession: async (id: string, page?: number, limit?: number): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.get<ApiResponse<any>>(`${ENDPOINTS.SKU.IMPORT_SESSION}/import/sessions/${id}`, {
                params: { page, limit }
            });
            return response.data;
        } catch (error) {
            console.error("Error getting SKU import session:", error);
            throw error;
        }
    },

    /**
     * Update SKU Import Session Row
     */
    updateImportRow: async (
        id: string,
        rowId: string,
        data: UpdateSkuImportRowRequest
    ): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.put<ApiResponse<any>>(
                `${ENDPOINTS.SKU.IMPORT_SESSION}/import/session/${id}/rows/${rowId}`,
                data
            );
            return response.data;
        } catch (error) {
            console.error("Error updating SKU import session row:", error);
            throw error;
        }
    }
};