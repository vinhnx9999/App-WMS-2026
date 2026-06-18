import { useState, useMemo, useEffect, useRef } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { themeQuartz, colorSchemeDark } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Search } from "lucide-react";
import { debounce } from "lodash";
import type { SkuDto } from "./models/sku-dto.model";

import { skuService } from "./services/sku.service";


export default function Skus() {
    const { t } = useTranslation();
    const [searchValue, setSearchValue] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    const gridRef = useRef<AgGridReact>(null);
    const [isDark, setIsDark] = useState(document.documentElement.classList.contains("dark"));
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        const observer = new MutationObserver(() => {
            setIsDark(document.documentElement.classList.contains("dark"));
        });
        observer.observe(document.documentElement, { attributes: true, attributeFilter: ["class"] });
        return () => observer.disconnect();
    }, []);

    const gridTheme = useMemo(() => {
        const baseTheme = isDark ? themeQuartz.withPart(colorSchemeDark) : themeQuartz;
        return baseTheme.withParams({
            backgroundColor: "var(--wms-grid-bg)",
            headerBackgroundColor: "var(--wms-grid-header-bg)",
            borderColor: "var(--wms-grid-border)",
            rowHoverColor: "var(--wms-grid-hover-bg)",
            textColor: "var(--wms-grid-text)",
        });
    }, [isDark]);

    // Lodash debounce search
    const debouncedSetSearch = useMemo(
        () => debounce((val: string) => setDebouncedSearch(val), 500),
        []
    );

    useEffect(() => {
        return () => {
            debouncedSetSearch.cancel();
        };
    }, [debouncedSetSearch]);

    const columnDefs = useMemo<ColDef<SkuDto>[]>(() => [
        {
            field: "skuCode",
            headerName: t("skus.skuCode", "Mã SKU"),
            pinned: "left",
            width: 140,
            cellRenderer: (params: any) => (
                <span className="font-bold text-primary">
                    {params.value || ""}
                </span>
            )
        },
        {
            field: "name",
            headerName: t("translation:common.noData"),
            width: 200,
        },
        {
            field: "productName",
            headerName: t("skus.linkedProduct", "Sản phẩm liên kết"),
            width: 200,
            valueGetter: (params) => {
                const code = params.data?.productCode;
                const name = params.data?.productName;
                return code && name ? `${name} (${code})` : name || code || "";
            }
        },
        {
            field: "categoryName",
            headerName: t("skus.categoryName", "Nhóm hàng"),
            width: 150,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "goodsNature",
            headerName: t("skus.goodsNature", "Tính chất"),
            width: 130,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "referencePrice",
            headerName: t("skus.referencePrice", "Giá tham chiếu"),
            width: 140,
            type: "numericColumn",
            valueFormatter: (params) => {
                if (params.value == null) return "";
                return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(params.value);
            }
        },
        {
            field: "createdAt",
            headerName: t("skus.createdAt", "Ngày tạo"),
            width: 130,
            valueFormatter: (params) => {
                if (!params.value) return "";
                return new Date(params.value).toLocaleDateString("vi-VN");
            }
        },
        {
            field: "description",
            headerName: t("skus.description", "Mô tả"),
            flex: 1,
            minWidth: 200,
            valueFormatter: (params) => params.value || ""
        }
    ], [t]);

    // Page size config
    const pageSize = 10;

    const datasource = useMemo<IDatasource>(() => {
        return {
            getRows: async (params: IGetRowsParams) => {
                try {
                    setIsLoading(true);

                    const page = Math.floor(params.startRow / pageSize) + 1;

                    const response = await skuService.searchSkus({
                        search: debouncedSearch || undefined,
                        page,
                        limit: pageSize
                    });

                    setIsLoading(false);

                    if (response.success && response.data) {
                        const { items, totalCount } = response.data;
                        params.successCallback(items, totalCount);
                    } else {
                        params.failCallback();
                    }
                } catch (error) {
                    console.error("Lỗi khi tải danh sách SKU trong Datasource:", error);
                    setIsLoading(false);
                    params.failCallback();
                }
            }
        };
    }, [debouncedSearch]);


    return (
        <div className="h-full w-full flex flex-col overflow-hidden">
            <div className="flex items-center gap-3 shrink-0 bg-card text-card-foreground p-3 ">
                <div className="relative flex-1 max-w-md">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                    <input
                        type="text"
                        placeholder={t("skus.searchPlaceholder", "Tìm theo mã SKU, tên hàng hóa")}
                        value={searchValue}
                        onChange={(e) => {
                            setSearchValue(e.target.value);
                            debouncedSetSearch(e.target.value);
                        }}
                        className="w-full pl-9 pr-4 py-1.5 text-sm rounded-md border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                    />
                </div>
            </div>

            <div className="flex-1 w-full overflow-hidden relative">
                <div className="w-full h-full">
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
                        paginationPageSizeSelector={false}
                        theme={gridTheme}
                        loading={isLoading}
                        overlayLoadingTemplate={`<span class="ag-overlay-loading-center">${t("translation:common.loading")}</span>`}
                        overlayNoRowsTemplate={`<span class="ag-overlay-no-rows-center">${t("translation:common.noData")}</span>`}
                        defaultColDef={{
                            resizable: true,
                            sortable: false,
                            filter: false,
                            minWidth: 120
                        }}
                    />
                </div>
            </div>
        </div>
    );
}

