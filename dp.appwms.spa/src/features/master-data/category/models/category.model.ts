import type { SearchParams } from "@/models/search-params";

export interface CategoryDto {
    id: string;
    tenantId: string;
    name: string;
    slug: string | null;
    description: string | null;
    createdAt: string;
    updatedAt: string | null;
}

export interface SearchCategoriesParams extends SearchParams {
    search?: string;
}

export interface CreateCategoryPayload {
    name: string;
    description: string | null;
}

export interface UpdateCategoryPayload {
    name: string;
    description: string | null;
}
