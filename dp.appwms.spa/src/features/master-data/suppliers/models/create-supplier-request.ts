export interface CreateSupplierRequest {
    code?: string;
    name: string;
    contact?: string | null;
    phone?: string | null;
    email?: string | null;
    address?: string | null;
}
