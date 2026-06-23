export interface CustomerDto {
    id: string;
    code: string;
    name: string;
    address?: string | null;
    phone?: string | null;
    type?: string | null;
    isDeleted: boolean;
    createdAt: string;
    updatedAt?: string | null;
}
