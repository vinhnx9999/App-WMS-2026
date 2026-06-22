import type { ApiResponse, PagedResult } from "@/models/response";
import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { SearchCategoriesParams, CategoryDto, CreateCategoryPayload, UpdateCategoryPayload } from "../models/category.model";


export const categoryService = {
    searchCategories: async (params: SearchCategoriesParams): Promise<ApiResponse<PagedResult<CategoryDto>>> => {
        try {
            const response = await apiClient.get<ApiResponse<PagedResult<CategoryDto>>>(ENDPOINTS.CATEGORY.SEARCH, {
                params
            });
            return response.data;
        } catch (error) {
            console.error("Error searching categories:", error);
            throw error;
        }
    },

    createCategory: async (payload: CreateCategoryPayload): Promise<ApiResponse<CategoryDto>> => {
        try {
            const response = await apiClient.post<ApiResponse<CategoryDto>>(ENDPOINTS.CATEGORY.SEARCH, payload);
            return response.data;
        } catch (error) {
            console.error("Error creating category:", error);
            throw error;
        }
    },

    updateCategory: async (id: string, payload: UpdateCategoryPayload): Promise<ApiResponse<void>> => {
        try {
            const response = await apiClient.put<ApiResponse<void>>(`${ENDPOINTS.CATEGORY.SEARCH}/${id}`, payload);
            if (response.status === 204 || response.status === 200) {
                return {
                    success: true,
                    data: undefined as any,
                    message: null,
                    errors: null
                };
            }
            return response.data;
        } catch (error) {
            console.error("Error updating category:", error);
            throw error;
        }
    },

    deleteCategory: async (id: string): Promise<ApiResponse<void>> => {
        try {
            const response = await apiClient.delete<ApiResponse<void>>(`${ENDPOINTS.CATEGORY.SEARCH}/${id}`);
            if (response.status === 204 || response.status === 200) {
                return {
                    success: true,
                    data: undefined as any,
                    message: null,
                    errors: null
                };
            }
            return response.data;
        } catch (error) {
            console.error("Error deleting category:", error);
            throw error;
        }
    }
};

