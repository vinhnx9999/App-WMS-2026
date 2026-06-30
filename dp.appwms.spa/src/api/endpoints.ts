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
    },
    LOCATION: {
        OCCUPANCY: Base_URL + "/locations/occupancy",
    },
    INBOUND: {
        DIRECT_PUTAWAY: Base_URL + "/inbound/putaway/direct",
    }
} as const;


