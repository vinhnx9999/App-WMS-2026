import type { SkuDto } from "../models/sku-dto.model";

import type { ApiResponse, PagedResult } from "@/types/response";
import type { SearchSkusParams } from "../models/search-sku-param.model";
import apiClient from "@/api/apiClient";
import { ENDPOINTS } from "@/api/endpoints";

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
    }
};