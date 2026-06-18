import { useState, useMemo, useEffect, useRef, type SubmitEvent } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { themeQuartz, colorSchemeDark } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Search, Plus } from "lucide-react";
import { debounce } from "lodash";
import { z } from "zod";
import { toast } from "sonner";
import type { ProductDto } from "./models/product-dto.model";
import { productService } from "./services/product.service";
import { categoryService } from "../category/services/category.service";

import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogFooter
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";

const createProductSchema = z.object({
    productCode: z.string().optional().nullable(),
    productName: z.string().min(1, "Tên sản phẩm là bắt buộc"),
    description: z.string().optional().nullable(),
    categoryId: z.string().optional().nullable()
});

export default function ProductListPage() {
    const { t } = useTranslation();
    const [searchValue, setSearchValue] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    const gridRef = useRef<AgGridReact>(null);
    const [isDark, setIsDark] = useState(document.documentElement.classList.contains("dark"));
    const [isLoading, setIsLoading] = useState(false);

    // Create Modal State
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [formValues, setFormValues] = useState({
        productCode: "",
        productName: "",
        description: "",
        categoryId: "",
        categoryName: ""
    });
    const [formErrors, setFormErrors] = useState<any>({});

    // Category search state
    const [categorySearch, setCategorySearch] = useState("");
    const [categories, setCategories] = useState<any[]>([]);
    const [isCategoryLoading, setIsCategoryLoading] = useState(false);
    const [isCategoryListOpen, setIsCategoryListOpen] = useState(false);
    const categoryContainerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const observer = new MutationObserver(() => {
            setIsDark(document.documentElement.classList.contains("dark"));
        });
        observer.observe(document.documentElement, { attributes: true, attributeFilter: ["class"] });
        return () => observer.disconnect();
    }, []);

    // Click outside to close category search list
    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (categoryContainerRef.current && !categoryContainerRef.current.contains(event.target as Node)) {
                setIsCategoryListOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
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

    // Debounce search categories
    const fetchCategories = useMemo(
        () => debounce(async (query: string) => {
            try {
                setIsCategoryLoading(true);
                const response = await categoryService.searchCategories({
                    search: query || undefined,
                    page: 1,
                    limit: 20
                });
                if (response.success && response.data) {
                    setCategories(response.data.items);
                }
            } catch (err) {
                console.error("Error loading categories:", err);
            } finally {
                setIsCategoryLoading(false);
            }
        }, 300),
        []
    );

    useEffect(() => {
        return () => {
            fetchCategories.cancel();
        };
    }, [fetchCategories]);

    const handleCategoryFocus = () => {
        setIsCategoryListOpen(true);
        fetchCategories(categorySearch);
    };

    const columnDefs = useMemo<ColDef<ProductDto>[]>(() => [
        {
            field: "productCode",
            headerName: t("products.productCode", "Mã sản phẩm"),
            pinned: "left",
            width: 150,
            cellRenderer: (params: any) => (
                <span className="font-bold text-primary">
                    {params.value || ""}
                </span>
            )
        },
        {
            field: "productName",
            headerName: t("products.productName", "Tên sản phẩm"),
            width: 250,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "categoryName",
            headerName: t("products.categoryName", "Nhóm hàng"),
            width: 180,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "createdAt",
            headerName: t("products.createdAt", "Ngày tạo"),
            width: 140,
            valueFormatter: (params) => {
                if (!params.value) return "";
                return new Date(params.value).toLocaleDateString("vi-VN");
            }
        },
        {
            field: "description",
            headerName: t("products.description", "Mô tả"),
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

                    const response = await productService.searchProducts({
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
                    console.error("Lỗi khi tải danh sách sản phẩm trong Datasource:", error);
                    setIsLoading(false);
                    params.failCallback();
                }
            }
        };
    }, [debouncedSearch]);

    const handleSubmit = async (e: SubmitEvent) => {
        e.preventDefault();
        setFormErrors({});

        // Validate
        const validation = createProductSchema.safeParse({
            productCode: formValues.productCode || null,
            productName: formValues.productName,
            description: formValues.description || null,
            categoryId: formValues.categoryId || null
        });

        if (!validation.success) {
            const errors: any = {};
            validation.error.issues.forEach(err => {
                const fieldName = err.path[0];
                if (fieldName !== undefined && fieldName !== null) {
                    errors[fieldName] = err.message;
                }
            });
            setFormErrors(errors);
            return;
        }

        try {
            setIsSaving(true);
            const response = await productService.createProduct(validation.data);

            if (response.success) {
                toast.success(t("translation:products.createSuccess"));
                setIsCreateOpen(false);
                // Reset form
                setFormValues({
                    productCode: "",
                    productName: "",
                    description: "",
                    categoryId: "",
                    categoryName: ""
                });
                setCategorySearch("");
                // Refresh grid
                gridRef.current?.api.refreshInfiniteCache();
            } else {
                toast.error(t("translation:products.errors.generic"));
            }
        } catch (error: any) {
            console.error("Lỗi khi tạo sản phẩm:", error);
            // Handle error codes translated
            const status = error.response?.status;
            let errorKey = t("translation:products.errors.generic");
            if (status === 409) {
                errorKey = t("translation:products.errors.duplicateProduct");
            } else if (status === 404) {
                errorKey = t("translation:products.errors.categoryNotFound");
            } else if (status === 400) {
                errorKey = t("translation:products.errors.validationFailed");
            }
            toast.error(errorKey);
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="h-full w-full flex flex-col overflow-hidden">
            <div className="flex items-center justify-between gap-3 shrink-0 bg-card text-card-foreground p-3 ">
                <div className="relative flex-1 max-w-md">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                    <input
                        type="text"
                        placeholder={t("products.searchPlaceholder", "Tìm theo mã sản phẩm, tên sản phẩm")}
                        value={searchValue}
                        onChange={(e) => {
                            setSearchValue(e.target.value);
                            debouncedSetSearch(e.target.value);
                        }}
                        className="w-full pl-9 pr-4 py-1.5 text-sm rounded-md border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                    />
                </div>
                <div>
                    <Button onClick={() => setIsCreateOpen(true)} className="flex items-center gap-1.5 text-sm">
                        <Plus className="size-4" />
                        {t("translation:products.addProduct")}
                    </Button>
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

            <Dialog open={isCreateOpen} onOpenChange={(open) => {
                setIsCreateOpen(open);
                if (!open) {
                    // Reset errors and values on close
                    setFormErrors({});
                    setFormValues({
                        productCode: "",
                        productName: "",
                        description: "",
                        categoryId: "",
                        categoryName: ""
                    });
                    setCategorySearch("");
                }
            }}>
                <DialogContent className="sm:max-w-[425px] bg-card border border-border text-card-foreground">
                    <DialogHeader>
                        <DialogTitle>{t("translation:products.createTitle")}</DialogTitle>
                    </DialogHeader>

                    <form onSubmit={handleSubmit} className="flex flex-col gap-4 py-2">
                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="productCode" className="text-sm font-medium text-foreground">
                                {t("products.productCode", "Mã sản phẩm")}
                            </label>
                            <Input
                                id="productCode"
                                value={formValues.productCode}
                                onChange={(e) => setFormValues(prev => ({ ...prev, productCode: e.target.value }))}
                                placeholder={t("products.productCodePlaceholder", "Để trống để tự động sinh mã")}
                                className="w-full bg-background border-border"
                            />
                            {formErrors.productCode && (
                                <span className="text-xs text-red-500">{formErrors.productCode}</span>
                            )}
                        </div>

                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="productName" className="text-sm font-medium text-foreground">
                                {t("products.productName", "Tên sản phẩm")} <span className="text-red-500">*</span>
                            </label>
                            <Input
                                id="productName"
                                value={formValues.productName}
                                onChange={(e) => setFormValues(prev => ({ ...prev, productName: e.target.value }))}
                                placeholder={t("products.productNamePlaceholder", "Nhập tên sản phẩm")}
                                className="w-full bg-background border-border"
                            />
                            {formErrors.productName && (
                                <span className="text-xs text-red-500">{formErrors.productName}</span>
                            )}
                        </div>

                        <div className="flex flex-col gap-1.5 relative" ref={categoryContainerRef}>
                            <label htmlFor="category" className="text-sm font-medium text-foreground">
                                {t("products.categoryName", "Nhóm hàng")}
                            </label>
                            <div className="relative">
                                <Input
                                    id="category"
                                    value={categorySearch}
                                    onChange={(e) => {
                                        setCategorySearch(e.target.value);
                                        // clear ID if user typed something different
                                        if (formValues.categoryId) {
                                            setFormValues(prev => ({ ...prev, categoryId: "", categoryName: "" }));
                                        }
                                        setIsCategoryListOpen(true);
                                        fetchCategories(e.target.value);
                                    }}
                                    onFocus={handleCategoryFocus}
                                    placeholder={t("products.categoryPlaceholder", "Tìm kiếm nhóm hàng...")}
                                    className="w-full bg-background border-border pr-8"
                                />
                                {categorySearch && (
                                    <button
                                        type="button"
                                        onClick={() => {
                                            setCategorySearch("");
                                            setFormValues(prev => ({ ...prev, categoryId: "", categoryName: "" }));
                                            setCategories([]);
                                            setIsCategoryListOpen(false);
                                        }}
                                        className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground text-xs font-semibold"
                                    >
                                        ✕
                                    </button>
                                )}
                            </div>
                            {isCategoryListOpen && (
                                <div className="absolute top-full left-0 w-full z-50 mt-1 border border-border bg-popover text-popover-foreground rounded-md shadow-lg overflow-hidden">
                                    <ScrollArea className="h-40">
                                        <div className="p-1">
                                            {isCategoryLoading ? (
                                                <div className="p-2 text-xs text-muted-foreground text-center">{t("common.loading", "Đang tải...")}</div>
                                            ) : categories.length === 0 ? (
                                                <div className="p-2 text-xs text-muted-foreground text-center">{t("common.noData", "Không có dữ liệu")}</div>
                                            ) : (
                                                categories.map((cat) => (
                                                    <button
                                                        key={cat.id}
                                                        type="button"
                                                        onClick={() => {
                                                            setFormValues(prev => ({ ...prev, categoryId: cat.id, categoryName: cat.name }));
                                                            setCategorySearch(cat.name);
                                                            setIsCategoryListOpen(false);
                                                        }}
                                                        className="w-full text-left px-3 py-1.5 text-sm rounded-sm hover:bg-accent hover:text-accent-foreground text-foreground transition-colors"
                                                    >
                                                        {cat.name}
                                                    </button>
                                                ))
                                            )}
                                        </div>
                                    </ScrollArea>
                                </div>
                            )}
                        </div>

                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="description" className="text-sm font-medium text-foreground">
                                {t("products.description", "Mô tả")}
                            </label>
                            <Input
                                id="description"
                                value={formValues.description}
                                onChange={(e) => setFormValues(prev => ({ ...prev, description: e.target.value }))}
                                placeholder={t("products.descriptionPlaceholder", "Nhập mô tả sản phẩm")}
                                className="w-full bg-background border-border"
                            />
                        </div>

                        <DialogFooter className="mt-4">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setIsCreateOpen(false)}
                                disabled={isSaving}
                                className="border-border text-muted-foreground hover:text-foreground"
                            >
                                {t("common.button.cancel", "Hủy")}
                            </Button>
                            <Button
                                type="submit"
                                disabled={isSaving}
                                className="bg-primary text-primary-foreground hover:bg-primary/95"
                            >
                                {isSaving ? t("common.loading", "Đang tải...") : t("common.button.save", "Lưu")}
                            </Button>
                        </DialogFooter>
                    </form>
                </DialogContent>
            </Dialog>
        </div>
    );
}