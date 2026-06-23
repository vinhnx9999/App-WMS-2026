import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, PagedResult } from "@/models/response";
import type { SupplierDto } from "../models/supplier.model";
import type { CreateSupplierRequest } from "../models/create-supplier-request";
import type { UpdateSupplierRequest } from "../models/update-supplier-request";
import type { SearchSuppliersParams } from "../models/search-suppliers-params";

export const supplierService = {
    searchSuppliers: async (params: SearchSuppliersParams): Promise<ApiResponse<PagedResult<SupplierDto>>> => {
        try {
            const response = await apiClient.get<ApiResponse<PagedResult<SupplierDto>>>(ENDPOINTS.SUPPLIER.SEARCH, {
                params
            });
            return response.data;
        } catch (error) {
            console.error("Error searching suppliers:", error);
            throw error;
        }
    },

    createSupplier: async (data: CreateSupplierRequest): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.post<ApiResponse<any>>(ENDPOINTS.SUPPLIER.SEARCH, data);
            return response.data;
        } catch (error) {
            console.error("Error creating supplier:", error);
            throw error;
        }
    },

    updateSupplier: async (id: string, data: UpdateSupplierRequest): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.put<ApiResponse<any>>(`${ENDPOINTS.SUPPLIER.SEARCH}/${id}`, data);
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
            console.error("Error updating supplier:", error);
            throw error;
        }
    },

    deleteSupplier: async (id: string): Promise<ApiResponse<any>> => {
        try {
            const response = await apiClient.delete<ApiResponse<any>>(`${ENDPOINTS.SUPPLIER.SEARCH}/${id}`);
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
            console.error("Error deleting supplier:", error);
            throw error;
        }
    }
};
