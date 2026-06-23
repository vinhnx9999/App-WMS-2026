export interface UpdateCustomerRequest {
    name: string;
    address?: string | null;
    phone?: string | null;
    type?: string | null;
}
