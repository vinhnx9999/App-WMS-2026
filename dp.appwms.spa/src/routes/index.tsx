import { createBrowserRouter, Navigate } from "react-router-dom";
import MainLayout from "../layouts/MainLayout";
import AuthGuard from "@/components/AuthGuard";
import DashboardPage from "../features/dashboard/DashboardPage";
import LoginPage from "@/features/authentication/LoginPage";
import ErrorPage from "@/components/ErrorPage";
import CategoryListPage from "@/features/master-data/category/CategoryListPage";
import SupplierListPage from "@/features/master-data/suppliers/SupplierListPage";
import SkusListPage from "@/features/master-data/skus/SkusListPage";
import ProductListPage from "@/features/master-data/product/ProductListPage";
import CustomerListPage from "../features/master-data/customers/CustomerListPage";

export const router = createBrowserRouter([
    {
        path: "/",
        element: (
            <AuthGuard>
                <MainLayout />
            </AuthGuard>
        ),
        errorElement: <ErrorPage />,
        children: [
            {
                index: true,
                element: <Navigate to="/dashboard" replace />,
            },
            {
                path: "dashboard",
                element: <DashboardPage />,
            },
            {
                path: "master-data",
                children: [
                    {
                        path: "products",
                        element: <ProductListPage />,
                    },
                    {
                        path: "categories",
                        element: <CategoryListPage />,
                    },
                    {
                        path: "skus",
                        element: <SkusListPage />,
                    },
                    {
                        path: "suppliers",
                        element: <SupplierListPage />,
                    },
                    {
                        path: "customers",
                        element: <CustomerListPage />,
                    },
                ],
            },
            {
                path: "rules",
                element: (
                    <div className="h-full w-full overflow-y-auto p-6 flex items-center justify-center text-slate-400">
                        <div className="text-center space-y-2">
                            <h2 className="text-xl font-bold text-slate-700 dark:text-slate-300">Strategy & Rules Configuration</h2>
                            <p className="text-sm">Trang cấu hình quy tắc và chiến lược vận hành kho hàng (đang phát triển)</p>
                        </div>
                    </div>
                ),
            },
            {
                path: "audit",
                element: (
                    <div className="h-full w-full overflow-y-auto p-6 flex items-center justify-center text-slate-400">
                        <div className="text-center space-y-2">
                            <h2 className="text-xl font-bold text-slate-700 dark:text-slate-300">System Audit Logs</h2>
                            <p className="text-sm">Trang nhật ký hệ thống và truy vết lịch sử vận hành (đang phát triển)</p>
                        </div>
                    </div>
                ),
            },
        ],
    },
    {
        path: "/auth",
        element: <LoginPage />
    }
]);
