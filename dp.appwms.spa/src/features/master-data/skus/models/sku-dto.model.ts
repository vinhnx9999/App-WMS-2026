/**
 * Sku Data Transfer Object
 * @param id - SKU ID
 * @param tenantId - Tenant ID
 * @param productId - Product ID
 * @param productCode - Product Code
 * @param productName - Product Name
 * @param categoryId - Category ID
 * @param categoryName - Category Name
 * @param skuCode - SKU Code
 * @param name - SKU Name
 * @param goodsNature - Goods Nature
 * @param description - Description
 * @param referencePrice - Reference Price
 * @param createdAt - Created At
 * @param updatedAt - Updated At
 */

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