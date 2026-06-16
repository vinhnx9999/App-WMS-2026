import { useState } from "react";
import MainLayout from "./layouts/MainLayout";
import DashboardPage from "./features/dashboard/DashboardPage";
import MasterDataPage from "./features/master-data/MasterDataPage";

function App() {
  const [activeTab, setActiveTab] = useState<string>("dashboard");

  const renderContent = () => {
    switch (activeTab) {
      case "dashboard":
        return <DashboardPage />;
      case "master-data":
        return <MasterDataPage />;
      case "rules":
        return (
          <div className="h-full w-full overflow-y-auto p-6 flex items-center justify-center text-slate-400">
            <div className="text-center space-y-2">
              <h2 className="text-xl font-bold text-slate-700 dark:text-slate-300">Strategy & Rules Configuration</h2>
              <p className="text-sm">Trang cấu hình quy tắc và chiến lược vận hành kho hàng (đang phát triển)</p>
            </div>
          </div>
        );
      case "audit":
        return (
          <div className="h-full w-full overflow-y-auto p-6 flex items-center justify-center text-slate-400">
            <div className="text-center space-y-2">
              <h2 className="text-xl font-bold text-slate-700 dark:text-slate-300">System Audit Logs</h2>
              <p className="text-sm">Trang nhật ký hệ thống và truy vết lịch sử vận hành (đang phát triển)</p>
            </div>
          </div>
        );
      default:
        return <DashboardPage />;
    }
  };

  return (
    <MainLayout activeTab={activeTab} setActiveTab={setActiveTab}>
      {renderContent()}
    </MainLayout>
  );
}

export default App;

