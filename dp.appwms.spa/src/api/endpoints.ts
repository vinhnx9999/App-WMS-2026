const Base_URL = "/api/v1";

export const ENDPOINTS = {
    SKU: {
        SEARCH: Base_URL + "/skus",
        IMPORT_SESSION: Base_URL + "/skus",
        LOOKUP: Base_URL + "/skus/lookup"
    },
    PRODUCT: {
        SEARCH: Base_URL + "/products",
    },
    CATEGORY: {
        SEARCH: Base_URL + "/categories",
    },
    SUPPLIER: {
        SEARCH: Base_URL + "/suppliers",
        LOOKUP: Base_URL + "/suppliers/lookup"
    },
    CUSTOMER: {
        SEARCH: Base_URL + "/customers",
    },
    WAREHOUSE: {
        LOOKUP: Base_URL + "/warehouses/lookup",
        INBOUND_WORKFLOW_CONFIG: Base_URL + "/warehouses/inbound-workflow-config",
    },
    LOCATION: {
        OCCUPANCY: Base_URL + "/locations/occupancy",
    },
    INBOUND: {
        SEARCH: Base_URL + "/inbound/search",
        DIRECT_PUTAWAY: Base_URL + "/inbound/putaway/direct",
        GET_BY_ID: (id: string) => `${Base_URL}/inbound/${id}`,
        FORCE_COMPLETE: (id: string) => `${Base_URL}/inbound/${id}/force-complete`,
        RECEIPTS: {
            CREATE: Base_URL + "/inbound/receipts",
            SEARCH: Base_URL + "/inbound/receipts/search",
            GET_BY_ID: (id: string) => `${Base_URL}/inbound/receipts/${id}`,
            COMPLETE: (id: string) => `${Base_URL}/inbound/receipts/${id}/complete`,
        }
    }
} as const;


