/**
 * UpdateProductRequest model for updating an existing product
 * @param productName - The product name (required)
 * @param description - The product description (optional)
 * @param categoryId - The category ID (optional)
 */
export interface UpdateProductRequest {
    productName: string;
    description: string | null;
    categoryId: string | null;
}``