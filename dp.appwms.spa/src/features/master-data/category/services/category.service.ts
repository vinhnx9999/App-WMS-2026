import type { ApiResponse, PagedResult } from "@/types/response";
import apiClient from "@/api/apiClient";
import { ENDPOINTS } from "@/api/endpoints";

export interface CategoryDto {
    id: string;
    name: string;
    description: string | null;
}

export interface SearchCategoriesParams {
    search?: string;
    page: number;
    limit: number;
}

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
    }
};
