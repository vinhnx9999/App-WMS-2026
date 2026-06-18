const Base_URL = "/api/v1";

export const ENDPOINTS = {
    SKU: {
        SEARCH: Base_URL + "/skus",
    },
    PRODUCT: {
        SEARCH: Base_URL + "/products",
    },
    CATEGORY: {
        SEARCH: Base_URL + "/categories",
    }
} as const;


