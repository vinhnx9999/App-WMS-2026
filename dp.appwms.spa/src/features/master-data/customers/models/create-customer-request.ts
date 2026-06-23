export interface CreateCustomerRequest {
    code?: string | null;
    name: string;
    address?: string | null;
    phone?: string | null;
    type?: string | null;
}
