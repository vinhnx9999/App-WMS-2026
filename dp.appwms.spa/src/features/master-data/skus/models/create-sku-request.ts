/**
 * CreateSkuRequest model for creating a new SKU
 * @param productId - The product ID (required)
 * @param skuCode - The SKU code (optional, will be auto-generated if not provided)
 * @param name - The SKU name (optional)
 * @param goodsNature - The goods nature (optional)
 * @param description - The SKU description (optional)
 * @param price - The reference price (optional)
 */
export interface CreateSkuRequest {
    productId: string;
    skuCode?: string | null;
    name?: string | null;
    goodsNature?: string | null;
    description?: string | null;
    price?: number | null;
}
