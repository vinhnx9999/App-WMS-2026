import apiClient from "./apiClient";

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}

export interface ApiResponse<T> {
    success: boolean;
    data: T | null;
    message: string | null;
    errors: string[] | null;
}

export interface SkuDto {
    id: string;
    tenantId: string;
    productId: string | null;
    productCode: string | null;
    productName: string | null;
    categoryId: string | null;
    categoryName: string | null;
    skuCode: string;
    name: string | null;
    goodsNature: string | null;
    description: string | null;
    referencePrice: number | null;
    createdAt: string;
    updatedAt: string | null;
}

export interface SearchSkusParams {
    search?: string;
    categoryId?: string;
    productId?: string;
    page: number;
    limit: number;
}

export const searchSkus = (params: SearchSkusParams) => {
    return apiClient.get<ApiResponse<PagedResult<SkuDto>>>("/api/v1/skus", {
        params,
    });
};
