import type { ProductDto } from "../models/product-dto.model";
import type { ApiResponse, PagedResult } from "@/types/response";
import type { SearchProductsParams } from "../models/search-product-param.model";
import type { CreateProductRequest } from "../models/create-product-request";
import apiClient from "@/api/apiClient";
import { ENDPOINTS } from "@/api/endpoints";

export const productService = {
    /**
     * Search Products
     * @param params - Search parameters
     * @returns Promise of ApiResponse<PagedResult<ProductDto>>
     */
    searchProducts: async (params: SearchProductsParams): Promise<ApiResponse<PagedResult<ProductDto>>> => {
        try {
            const response = await apiClient.get<ApiResponse<PagedResult<ProductDto>>>(ENDPOINTS.PRODUCT.SEARCH, {
                params
            });
            return response.data;
        } catch (error) {
            console.error("Error searching products:", error);
            throw error;
        }
    },

    /**
     * Create Product
     * @param data - Create product request
     * @returns Promise of ApiResponse<any>
     */
    createProduct: async (data: CreateProductRequest): Promise<ApiResponse<any>> => {
        try {
            debugger;
            const response = await apiClient.post<ApiResponse<any>>(ENDPOINTS.PRODUCT.SEARCH, data);
            return response.data;
        } catch (error) {
            console.error("Error creating product:", error);
            throw error;
        }
    }
};
