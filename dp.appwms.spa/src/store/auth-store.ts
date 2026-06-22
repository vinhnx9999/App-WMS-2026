import { create } from "zustand";
import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:7366";

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;

  initializeAuth: () => void;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  setAccessToken: (token: string) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isLoading: true,

  initializeAuth: () => {
    try {
      const savedUser = localStorage.getItem("user");
      const token = localStorage.getItem("accessToken");
      if (savedUser && token) {
        set({
          user: JSON.parse(savedUser),
          isAuthenticated: true,
          isLoading: false
        });
      } else {
        set({ isLoading: false });
      }
    } catch (error) {
      console.error("Failed to initialize auth state:", error);
      set({ isLoading: false });
    }
  },

  login: async (email, password) => {
    set({ isLoading: true });
    try {
      const response = await axios.post(`${API_URL}/v1/auth/login`, { email, password });

      const { accessToken, refreshToken, user: userData } = response.data.data;

      localStorage.setItem("accessToken", accessToken);
      localStorage.setItem("refreshToken", refreshToken);
      localStorage.setItem("user", JSON.stringify(userData));

      set({
        user: userData,
        isAuthenticated: true,
        isLoading: false
      });
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  logout: () => {
    const refreshToken = localStorage.getItem("refreshToken");
    const accessToken = localStorage.getItem("accessToken");

    if (accessToken) {
      axios.post(`${API_URL}/v1/auth/logout`,
        { refreshToken, logoutAllDevices: false },
        { headers: { Authorization: `Bearer ${accessToken}` } }
      ).catch(err => {
        console.error("Backend logout API failed:", err);
      });
    }

    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("user");

    set({
      user: null,
      isAuthenticated: false
    });

    // Redirect to login page
    window.location.href = "/auth";
  },

  setAccessToken: (token: string) => {
    localStorage.setItem("accessToken", token);
  }
}));
