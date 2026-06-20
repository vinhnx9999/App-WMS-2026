import { createBrowserRouter, Navigate } from "react-router-dom";
import MainLayout from "../layouts/MainLayout";
import DashboardPage from "../features/dashboard/DashboardPage";
import Skus from "../features/master-data/skus/SkusListPage";
import Suppliers from "../features/master-data/suppliers/Suppliers";
import Customers from "../features/master-data/customers/Customer";
import Products from "../features/master-data/product/ProductListPage";
import LoginPage from "@/features/authentication/LoginPage";
import ErrorPage from "@/components/ErrorPage";

export const router = createBrowserRouter([
    {
        path: "/",
        element: <MainLayout />,
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
                        element: <Products />,
                    },
                    {
                        path: "skus",
                        element: <Skus />,
                    },
                    {
                        path: "suppliers",
                        element: <Suppliers />,
                    },
                    {
                        path: "customers",
                        element: <Customers />,
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
