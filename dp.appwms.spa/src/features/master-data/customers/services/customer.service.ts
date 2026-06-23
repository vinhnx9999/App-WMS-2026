import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, PagedResult } from "@/models/response";
import type { CustomerDto } from "../models/customer.model";
import type { CreateCustomerRequest } from "../models/create-customer-request";
import type { UpdateCustomerRequest } from "../models/update-customer-request";
import type { SearchCustomersParams } from "../models/search-customers-params";

export const customerService = {
    searchCustomers: async (params: SearchCustomersParams): Promise<ApiResponse<PagedResult<CustomerDto>>> => {
        try {
            const response = await apiClient.get<ApiResponse<PagedResult<CustomerDto>>>(ENDPOINTS.CUSTOMER.SEARCH, {
                params
            });
            return response.data;
        } catch (error) {
            console.error("Error searching customers:", error);
            throw error;
        }
    },

    createCustomer: async (data: CreateCustomerRequest): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.post<ApiResponse<any>>(ENDPOINTS.CUSTOMER.SEARCH, data);
            return response.data;
        } catch (error) {
            console.error("Error creating customer:", error);
            throw error;
        }
    },

    updateCustomer: async (id: string, data: UpdateCustomerRequest): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.put<ApiResponse<any>>(`${ENDPOINTS.CUSTOMER.SEARCH}/${id}`, data);
            if (response.status === 204 || response.status === 200) {
                return {
                    success: true,
                    data: null,
                    message: null,
                    errors: null
                };
            }
            return response.data;
        } catch (error) {
            console.error("Error updating customer:", error);
            throw error;
        }
    },

    deleteCustomer: async (id: string): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.delete<ApiResponse<any>>(`${ENDPOINTS.CUSTOMER.SEARCH}/${id}`);
            if (response.status === 204 || response.status === 200) {
                return {
                    success: true,
                    data: null,
                    message: null,
                    errors: null
                };
            }
            return response.data;
        } catch (error) {
            console.error("Error deleting customer:", error);
            throw error;
        }
    }
};
