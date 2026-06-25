import { create } from "zustand";
import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/models/response";

export interface Warehouse {
  id: string;
  code: string;
  name: string;
}

interface WarehouseState {
  warehouses: Warehouse[];
  selectedWarehouse: Warehouse | null;
  isLoading: boolean;
  error: string | null;

  fetchWarehouses: () => Promise<void>;
  setSelectedWarehouse: (warehouse: Warehouse) => void;
  clearSelection: () => void;
}

export const useWarehouseStore = create<WarehouseState>((set) => ({
  warehouses: [],
  selectedWarehouse: null,
  isLoading: false,
  error: null,

  fetchWarehouses: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.get<ApiResponse<Warehouse[]>>(ENDPOINTS.WAREHOUSE.LOOKUP);
      const list = response.data.data || [];

      let selected = null;
      if (list.length > 0) {
        const storedId = localStorage.getItem("selectedWarehouseId");
        const found = list.find((w) => w.id === storedId);

        if (found) {
          selected = found;
        } else {
          selected = list[0];
          localStorage.setItem("selectedWarehouseId", selected.id);
        }
      } else {
        localStorage.removeItem("selectedWarehouseId");
      }

      set({
        warehouses: list,
        selectedWarehouse: selected,
        isLoading: false,
      });
    } catch (err: any) {
      console.error("Failed to fetch warehouses:", err);
      set({
        isLoading: false,
        error: err.message || "Failed to fetch warehouses",
      });
    }
  },

  setSelectedWarehouse: (warehouse: Warehouse) => {
    localStorage.setItem("selectedWarehouseId", warehouse.id);
    set({ selectedWarehouse: warehouse });
  },

  clearSelection: () => {
    localStorage.removeItem("selectedWarehouseId");
    set({ selectedWarehouse: null });
  },
}));
