/**
 * CreateProductRequest model for creating a new product
 * @param productCode - The product code (optional, will be auto-generated if not provided)
 * @param productName - The product name (required)
 * @param description - The product description (optional)
 * @param categoryId - The category ID (optional)
 */
export interface CreateProductRequest {
    productCode?: string | null;
    productName: string;
    description?: string | null;
    categoryId?: string | null;
}
