/**
 * UpdateSkuRequest model for updating an existing SKU
 * @param name - The SKU name (optional)
 * @param goodsNature - The goods nature (optional)
 * @param description - The SKU description (optional)
 * @param price - The reference price (optional)
 */
export interface UpdateSkuRequest {
    name?: string | null;
    goodsNature?: string | null;
    description?: string | null;
    price?: number | null;
}
