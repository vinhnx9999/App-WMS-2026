export interface CreateProductRequest {
    productCode?: string | null;
    productName: string;
    description?: string | null;
    categoryId?: string | null;
}
