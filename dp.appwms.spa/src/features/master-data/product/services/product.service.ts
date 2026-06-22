import type { ProductDto } from "../models/product-dto.model";
import type { ApiResponse, PagedResult } from "@/models/response";
import type { SearchProductsParams } from "../models/search-product-param.model";
import type { CreateProductRequest } from "../models/create-product-request";
import { ENDPOINTS } from "@/api/endpoints";
import type { UpdateProductRequest } from "../models/update-product-request";
import apiClient from "@/api/api-client";

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
            const response = await apiClient.post<ApiResponse<any>>(ENDPOINTS.PRODUCT.SEARCH, data);
            return response.data;
        } catch (error) {
            console.error("Error creating product:", error);
            throw error;
        }
    },

    /**
     * Update Product
     * @param id - Product ID
     * @param data - Update product request (ProductName, Description, CategoryId)
     * @returns Promise of ApiResponse<any>
     */
    updateProduct: async (id: string, data: UpdateProductRequest): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.put<ApiResponse<any>>(`${ENDPOINTS.PRODUCT.SEARCH}/${id}`, data);
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
            console.error("Error updating product:", error);
            throw error;
        }
    },

    /**
     * Delete Product
     * @param id - Product ID
     * @returns Promise of ApiResponse<any>
     */
    deleteProduct: async (id: string): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.delete<ApiResponse<any>>(`${ENDPOINTS.PRODUCT.SEARCH}/${id}`);
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
            console.error("Error deleting product:", error);
            throw error;
        }
    }
};
