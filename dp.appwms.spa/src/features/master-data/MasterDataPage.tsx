import { useState } from "react";
import { Plus, Search, Filter, ArrowUpDown, Tag, Box, AlertCircle } from "lucide-react";

interface SKUItem {
  sku: string;
  name: string;
  category: string;
  zone: string;
  qty: number;
  minQty: number;
  status: "In Stock" | "Low Stock" | "Out of Stock";
}

const mockSKUs: SKUItem[] = [
  { sku: "SKU-AUTO-001", name: "Cảm biến tiệm cận quang học E3Z", category: "Automation", zone: "Zone A", qty: 450, minQty: 100, status: "In Stock" },
  { sku: "SKU-AUTO-002", name: "Bộ điều khiển PLC S7-1200 CPU 1214C", category: "Automation", zone: "Zone A", qty: 24, minQty: 10, status: "In Stock" },
  { sku: "SKU-ELEC-101", name: "Cáp điện lực hạ thế CXV 2x6", category: "Electrical", zone: "Zone B", qty: 15, minQty: 50, status: "Low Stock" },
  { sku: "SKU-ELEC-102", name: "Công tắc hành trình Omron WLCA2", category: "Electrical", zone: "Zone A", qty: 120, minQty: 30, status: "In Stock" },
  { sku: "SKU-MECH-201", name: "Vòng bi cầu SKF 6204-2Z", category: "Mechanical", zone: "Zone C", qty: 800, minQty: 200, status: "In Stock" },
  { sku: "SKU-MECH-202", name: "Khớp nối mềm cao su DN80", category: "Mechanical", zone: "Zone C", qty: 2, minQty: 5, status: "Low Stock" },
  { sku: "SKU-PACK-301", name: "Thùng carton đóng gói 40x30x30", category: "Packaging", zone: "Zone B", qty: 2500, minQty: 500, status: "In Stock" },
  { sku: "SKU-PACK-302", name: "Màng PE quấn pallet 15kg", category: "Packaging", zone: "Zone B", qty: 0, minQty: 20, status: "Out of Stock" },
  { sku: "SKU-AUTO-003", name: "Cảm biến nhiệt độ Pt100 K-type", category: "Automation", zone: "Zone A", qty: 85, minQty: 25, status: "In Stock" },
  { sku: "SKU-ELEC-103", name: "Khởi động từ Contactor MC-9b", category: "Electrical", zone: "Zone A", qty: 45, minQty: 15, status: "In Stock" },
  { sku: "SKU-MECH-203", name: "Xích tải công nghiệp 80-1R", category: "Mechanical", zone: "Zone C", qty: 8, minQty: 10, status: "Low Stock" },
  { sku: "SKU-PACK-303", name: "Băng keo trong OPP 5cm", category: "Packaging", zone: "Zone B", qty: 1200, minQty: 300, status: "In Stock" },
  { sku: "SKU-AUTO-004", name: "Bộ nguồn tổ ong 24V 10A", category: "Automation", zone: "Zone A", qty: 50, minQty: 15, status: "In Stock" },
  { sku: "SKU-ELEC-104", name: "CB tép MCB 2P 16A Schneider", category: "Electrical", zone: "Zone A", qty: 95, minQty: 30, status: "In Stock" }
];

export default function MasterDataPage() {
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("All");

  const filteredSKUs = mockSKUs.filter(item => {
    const matchesSearch = item.name.toLowerCase().includes(search.toLowerCase()) || item.sku.toLowerCase().includes(search.toLowerCase());
    const matchesCategory = categoryFilter === "All" || item.category === categoryFilter;
    return matchesSearch && matchesCategory;
  });

  return (
    <div className="h-full w-full overflow-y-auto p-6 space-y-6">

      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Master Data Management</h1>
          <p className="text-sm text-slate-500">Quản lý danh mục hàng hóa, vị trí lưu trữ và định mức tồn kho tối thiểu</p>
        </div>
        <button className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium transition-colors cursor-pointer self-start sm:self-auto shadow-sm">
          <Plus className="size-4" />
          Thêm SKU mới
        </button>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col md:flex-row gap-3 p-4 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl">
        <div className="flex-1 relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-slate-400" />
          <input
            type="text"
            placeholder="Tìm kiếm theo mã SKU hoặc tên hàng..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-4 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div className="flex gap-2">
          <div className="flex items-center gap-1.5 px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm">
            <Filter className="size-3.5 text-slate-400" />
            <select
              value={categoryFilter}
              onChange={(e) => setCategoryFilter(e.target.value)}
              className="bg-transparent focus:outline-none cursor-pointer text-sm font-medium"
            >
              <option value="All">Tất cả danh mục</option>
              <option value="Automation">Automation</option>
              <option value="Electrical">Electrical</option>
              <option value="Mechanical">Mechanical</option>
              <option value="Packaging">Packaging</option>
            </select>
          </div>
        </div>
      </div>

      {/* SKUs Table (Scroll container) */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-xs">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-850/50 text-xs font-semibold uppercase tracking-wider text-slate-500">
                <th className="p-4">SKU Code</th>
                <th className="p-4">Tên Sản Phẩm</th>
                <th className="p-4">Danh Mục</th>
                <th className="p-4">Khu Vực Lưu Trữ</th>
                <th className="p-4 text-right">Tồn Kho</th>
                <th className="p-4 text-right">Định Mức</th>
                <th className="p-4 text-center">Trạng Thái</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800 text-sm">
              {filteredSKUs.length > 0 ? (
                filteredSKUs.map((item, idx) => (
                  <tr key={idx} className="hover:bg-slate-50/55 dark:hover:bg-slate-800/40 transition-colors">
                    <td className="p-4 font-mono font-semibold text-blue-600 dark:text-blue-400">{item.sku}</td>
                    <td className="p-4 font-medium max-w-xs truncate">{item.name}</td>
                    <td className="p-4">
                      <span className="flex items-center gap-1.5 text-xs">
                        <Box className="size-3.5 text-slate-400" />
                        {item.category}
                      </span>
                    </td>
                    <td className="p-4">
                      <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                        {item.zone}
                      </span>
                    </td>
                    <td className="p-4 text-right font-semibold">{item.qty.toLocaleString()}</td>
                    <td className="p-4 text-right text-slate-400 font-medium">{item.minQty.toLocaleString()}</td>
                    <td className="p-4 text-center">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-sm text-xs font-bold ${item.status === "In Stock"
                        ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-400"
                        : item.status === "Low Stock"
                          ? "bg-amber-100 text-amber-800 dark:bg-amber-950/40 dark:text-amber-400"
                          : "bg-red-100 text-red-800 dark:bg-red-950/40 dark:text-red-400"
                        }`}>
                        {item.status}
                      </span>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={7} className="p-8 text-center text-slate-400">
                    <div className="flex flex-col items-center justify-center gap-2">
                      <AlertCircle className="size-8 text-slate-300" />
                      <span>Không tìm thấy sản phẩm nào khớp với tìm kiếm</span>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

    </div>
  );
}
