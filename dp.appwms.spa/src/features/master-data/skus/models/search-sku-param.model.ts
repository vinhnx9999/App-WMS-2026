/**
 * Search SKU Parameters
 * @param search - Search term
 * @param categoryId - Category ID
 * @param productId - Product ID
 * @param page - Page number
 * @param limit - Limit per page
 */
export interface SearchSkusParams {
    search?: string;
    categoryId?: string;
    productId?: string;
    page: number;
    limit: number;
}