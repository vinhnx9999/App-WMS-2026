import type { SearchParams } from "@/models/search-params";

export interface SearchSuppliersParams extends SearchParams {
    search?: string;
}
