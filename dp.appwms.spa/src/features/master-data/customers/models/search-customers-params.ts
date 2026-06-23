import type { SearchParams } from "@/models/search-params";

export interface SearchCustomersParams extends SearchParams {
    search?: string;
}
