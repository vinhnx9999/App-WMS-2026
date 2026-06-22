/**
 * Search Product Parameters
 * @param search - Search term
 * @param categoryId - Category ID
 * @param page - Page number
 * @param limit - Limit per page
 */
export interface SearchProductsParams {
    search?: string;
    categoryId?: string;
    page: number;
    limit: number;
}
