export interface SupplierDto {
    id: string;
    code: string;
    name: string;
    contact?: string | null;
    phone?: string | null;
    email?: string | null;
    address?: string | null;
    isDeleted: boolean;
    createdAt: string;
    updatedAt?: string | null;
}
