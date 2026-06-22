/**
 * Product Data Transfer Object
 * @param id - Product ID
 * @param tenantId - Tenant ID
 * @param productCode - Product Code
 * @param productName - Product Name
 * @param description - Description
 * @param categoryId - Category ID
 * @param categoryName - Category Name
 * @param createdAt - Created At
 * @param updatedAt - Updated At
 */
export interface ProductDto {
    id: string;
    tenantId: string;
    productCode: string;
    productName: string | null;
    description: string | null;
    categoryId: string | null;
    categoryName: string | null;
    createdAt: string;
    updatedAt: string | null;
}
