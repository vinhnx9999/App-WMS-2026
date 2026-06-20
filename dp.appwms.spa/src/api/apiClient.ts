import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/authStore';

const API_URL = import.meta.env.VITE_API_URL || "https://localhost:7366";

// Primary axios instance
const apiClient = axios.create({
    baseURL: API_URL,
    headers: {
        "Content-Type": "application/json",
    },
});

// Independent axios instance for refresh token requests to avoid interceptor loops
const authApiClient = axios.create({
    baseURL: API_URL,
    headers: {
        "Content-Type": "application/json",
    },
});

let isRefreshing = false;
let failedQueue: Array<{
    resolve: (token: string) => void;
    reject: (error: any) => void;
}> = [];

const processQueue = (error: any, token: string | null = null) => {
    failedQueue.forEach((prom) => {
        if (token) {
            prom.resolve(token);
        } else {
            prom.reject(error);
        }
    });
    failedQueue = [];
};

// Request Interceptor: Attach token dynamically
apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const token = localStorage.getItem("accessToken");
        if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);

// Response Interceptor: Catch 401 and refresh token silently
apiClient.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        debugger;
        const originalRequest = error.config;

        if (!originalRequest || error.response?.status !== 401) {
            return Promise.reject(error);
        }

        // Avoid infinite loop if retried request still returns 401
        // @ts-ignore
        if (originalRequest._retry) {
            return Promise.reject(error);
        }

        if (isRefreshing) {
            return new Promise((resolve, reject) => {
                failedQueue.push({
                    resolve: (token: string) => {
                        if (originalRequest.headers) {
                            originalRequest.headers.Authorization = `Bearer ${token}`;
                        }
                        resolve(apiClient(originalRequest));
                    },
                    reject: (err: any) => reject(err),
                });
            });
        }

        // @ts-ignore
        originalRequest._retry = true;
        isRefreshing = true;

        const refreshToken = localStorage.getItem("refreshToken");
        const logout = useAuthStore.getState().logout;
        const setAccessToken = useAuthStore.getState().setAccessToken;

        if (!refreshToken) {
            logout();
            return Promise.reject(error);
        }

        try {
            const response = await authApiClient.post("/v1/auth/refresh", {
                refreshToken: refreshToken
            });

            const { accessToken, refreshToken: newRefreshToken } = response.data.data;

            setAccessToken(accessToken);
            localStorage.setItem("refreshToken", newRefreshToken);

            if (originalRequest.headers) {
                originalRequest.headers.Authorization = `Bearer ${accessToken}`;
            }

            processQueue(null, accessToken);
            return apiClient(originalRequest);
        } catch (refreshError) {
            processQueue(refreshError, null);
            logout();
            return Promise.reject(refreshError);
        } finally {
            isRefreshing = false;
        }
    }
);

export default apiClient;
