import { useState, useMemo, useEffect, useRef } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Search } from "lucide-react";

import { searchSkus, type SkuDto } from "../../../api/endpoints";

import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-alpine.css";

export default function Skus() {
    const { t } = useTranslation();
    const [searchValue, setSearchValue] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    const gridRef = useRef<AgGridReact>(null);

    useEffect(() => {
        const handler = setTimeout(() => {
            setDebouncedSearch(searchValue);
        }, 500);
        return () => {
            clearTimeout(handler);
        };
    }, [searchValue]);

    const columnDefs = useMemo<ColDef<SkuDto>[]>(() => [
        {
            field: "skuCode",
            headerName: "Mã SKU",
            pinned: "left",
            width: 140,
            cellRenderer: (params: any) => {
                return `<span class="font-bold text-primary dark:text-blue-400">${params.value || ""}</span>`;
            }
        },
        {
            field: "name",
            headerName: "Tên hàng hóa (SKU)",
            width: 200,
        },
        {
            field: "productName",
            headerName: "Sản phẩm liên kết",
            width: 200,
            valueGetter: (params) => {
                const code = params.data?.productCode;
                const name = params.data?.productName;
                return code && name ? `${name} (${code})` : name || code || "N/A";
            }
        },
        {
            field: "categoryName",
            headerName: "Nhóm hàng",
            width: 150,
            valueFormatter: (params) => params.value || "N/A"
        },
        {
            field: "goodsNature",
            headerName: "Tính chất",
            width: 130,
            valueFormatter: (params) => params.value || "Thường"
        },
        {
            field: "referencePrice",
            headerName: "Giá tham chiếu",
            width: 140,
            type: "numericColumn",
            valueFormatter: (params) => {
                if (params.value == null) return "N/A";
                return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(params.value);
            }
        },
        {
            field: "createdAt",
            headerName: "Ngày tạo",
            width: 130,
            valueFormatter: (params) => {
                if (!params.value) return "";
                return new Date(params.value).toLocaleDateString("vi-VN");
            }
        },
        {
            field: "description",
            headerName: "Mô tả",
            flex: 1,
            minWidth: 200,
            valueFormatter: (params) => params.value || "-"
        }
    ], []);

    // Page size config
    const pageSize = 15;

    const datasource = useMemo<IDatasource>(() => {
        return {
            getRows: async (params: IGetRowsParams) => {
                try {
                    const page = Math.floor(params.startRow / pageSize) + 1;

                    const response = await searchSkus({
                        search: debouncedSearch || undefined,
                        page,
                        limit: pageSize
                    });

                    if (response.data.success && response.data.data) {
                        const { items, totalCount } = response.data.data;

                        params.successCallback(items, totalCount);
                    } else {
                        params.failCallback();
                    }
                } catch (error) {
                    console.error("Lỗi khi tải danh sách SKU trong Datasource:", error);
                    params.failCallback();
                }
            }
        };
    }, [debouncedSearch]);

    return (
        <div className="h-full w-full flex flex-col overflow-hidden">
            <div className="flex items-center gap-3 mb-4 shrink-0 bg-white dark:bg-slate-900 p-3 rounded-lg border border-slate-200 dark:border-slate-800 shadow-xs">
                <div className="relative flex-1 max-w-md">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-slate-400" />
                    <input
                        type="text"
                        placeholder="Tìm theo mã SKU, tên hàng hóa"
                        value={searchValue}
                        onChange={(e) => setSearchValue(e.target.value)}
                        className="w-full pl-9 pr-4 py-1.5 text-sm rounded-md border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 text-slate-800 dark:text-slate-100 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                    />
                </div>
            </div>

            <div className="flex-1 w-full bg-white dark:bg-slate-900 rounded-lg border border-slate-200 dark:border-slate-800 overflow-hidden shadow-xs relative">
                <div className="ag-theme-alpine w-full h-full dark:ag-theme-alpine-dark">
                    <AgGridReact
                        ref={gridRef}
                        columnDefs={columnDefs}
                        datasource={datasource}
                        rowModelType="infinite"
                        cacheBlockSize={pageSize}
                        maxConcurrentDatasourceRequests={1}
                        infiniteInitialRowCount={pageSize}
                        pagination={true}
                        paginationPageSize={pageSize}
                        paginationPageSizeSelector={false} // Disable to force matching cacheBlockSize
                        defaultColDef={{
                            resizable: true,
                            sortable: false,
                            filter: false,
                        }}
                    />
                </div>
            </div>
        </div>
    );
}
