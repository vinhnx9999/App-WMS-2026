const Base_URL = "/api/v1";

export const ENDPOINTS = {
    SKU: {
        SEARCH: Base_URL + "/skus",
        IMPORT_SESSION: Base_URL + "/skus"
    },
    PRODUCT: {
        SEARCH: Base_URL + "/products",
    },
    CATEGORY: {
        SEARCH: Base_URL + "/categories",
    },
    SUPPLIER: {
        SEARCH: Base_URL + "/suppliers",
    },
    CUSTOMER: {
        SEARCH: Base_URL + "/customers",
    },
    WAREHOUSE: {
        LOOKUP: Base_URL + "/warehouses/lookup",
    }
} as const;


