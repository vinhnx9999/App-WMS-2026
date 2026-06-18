import { useState, useMemo, useEffect, useRef, type SubmitEvent } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { themeQuartz, colorSchemeDark } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Search, Plus, Trash } from "lucide-react";
import { debounce } from "lodash";
import { z } from "zod";
import { toast } from "sonner";
import type { SkuDto } from "./models/sku-dto.model";

import { skuService } from "./services/sku.service";
import { productService } from "../product/services/product.service";

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
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

const createSkuSchema = z.object({
    productId: z.string().min(1, "Sản phẩm liên kết là bắt buộc"),
    skuCode: z.string().optional().nullable(),
    name: z.string().optional().nullable(),
    goodsNature: z.string().optional().nullable(),
    description: z.string().optional().nullable(),
    price: z.preprocess(
        (val) => (val === "" || val === null || val === undefined ? null : Number(val)),
        z.number().min(0, "Giá tham chiếu không thể âm").nullable().optional()
    )
});

export default function Skus() {
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
        productId: "",
        productCode: "",
        productName: "",
        skuCode: "",
        name: "",
        goodsNature: "",
        description: "",
        price: ""
    });
    const [formErrors, setFormErrors] = useState<any>({});

    // Product search state (inside create modal)
    const [productSearch, setProductSearch] = useState("");
    const [products, setProducts] = useState<any[]>([]);
    const [isProductLoading, setIsProductLoading] = useState(false);
    const [isProductListOpen, setIsProductListOpen] = useState(false);
    const productContainerRef = useRef<HTMLDivElement>(null);

    // Delete Modal State
    const [isDeleteOpen, setIsDeleteOpen] = useState(false);
    const [skuToDelete, setSkuToDelete] = useState<SkuDto | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);

    useEffect(() => {
        const observer = new MutationObserver(() => {
            setIsDark(document.documentElement.classList.contains("dark"));
        });
        observer.observe(document.documentElement, { attributes: true, attributeFilter: ["class"] });
        return () => observer.disconnect();
    }, []);

    // Click outside to close product search list
    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (productContainerRef.current && !productContainerRef.current.contains(event.target as Node)) {
                setIsProductListOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    const gridTheme = useMemo(() => {
        const baseTheme = isDark ? themeQuartz.withPart(colorSchemeDark) : themeQuartz;
        return baseTheme.withParams({
            backgroundColor: "var(--background)",
            headerBackgroundColor: "var(--muted)",
            borderColor: "var(--border)",
            rowHoverColor: "var(--accent)",
            textColor: "var(--foreground)",
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

    // Debounce search products
    const fetchProducts = useMemo(
        () => debounce(async (query: string) => {
            try {
                setIsProductLoading(true);
                const response = await productService.searchProducts({
                    search: query || undefined,
                    page: 1,
                    limit: 20
                });
                if (response.success && response.data) {
                    setProducts(response.data.items);
                }
            } catch (err) {
                console.error("Error loading products:", err);
            } finally {
                setIsProductLoading(false);
            }
        }, 300),
        []
    );

    useEffect(() => {
        return () => {
            fetchProducts.cancel();
        };
    }, [fetchProducts]);

    const handleProductFocus = () => {
        setIsProductListOpen(true);
        fetchProducts(productSearch);
    };

    const columnDefs = useMemo<ColDef<SkuDto>[]>(() => [
        {
            field: "skuCode",
            headerName: t("translation:skus.skuCode"),
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
            headerName: t("translation:skus.name", "Tên SKU"),
            width: 200,
            editable: true,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "productName",
            headerName: t("translation:skus.linkedProduct"),
            width: 200,
            valueGetter: (params) => {
                const code = params.data?.productCode;
                const name = params.data?.productName;
                return code && name ? `${name} (${code})` : name || code || "";
            }
        },
        {
            field: "categoryName",
            headerName: t("translation:skus.categoryName"),
            width: 150,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "goodsNature",
            headerName: t("translation:skus.goodsNature"),
            width: 130,
            editable: true,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "referencePrice",
            headerName: t("translation:skus.referencePrice"),
            width: 140,
            type: "numericColumn",
            editable: true,
            valueParser: (params) => {
                const parsed = parseFloat(params.newValue);
                return isNaN(parsed) ? null : parsed;
            },
            valueFormatter: (params) => {
                if (params.value == null) return "";
                return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(params.value);
            }
        },
        {
            field: "createdAt",
            headerName: t("translation:skus.createdAt"),
            width: 130,
            valueFormatter: (params) => {
                if (!params.value) return "";
                return new Date(params.value).toLocaleDateString("vi-VN");
            }
        },
        {
            field: "description",
            headerName: t("translation:skus.description"),
            flex: 1,
            minWidth: 200,
            editable: true,
            valueFormatter: (params) => params.value || ""
        },
        {
            headerName: t("translation:products.actions", "Thao tác"),
            pinned: "right",
            width: 80,
            sortable: false,
            filter: false,
            resizable: false,
            editable: false,
            cellRenderer: (params: any) => {
                if (!params.data) return null;
                return (
                    <div className="flex items-center justify-center h-full">
                        <Tooltip>
                            <TooltipTrigger asChild>
                                <Button
                                    variant="destructive"
                                    size="icon-xs"
                                    onClick={() => {
                                        setSkuToDelete(params.data);
                                        setIsDeleteOpen(true);
                                    }}
                                >
                                    <Trash className="size-3.5" />
                                </Button>
                            </TooltipTrigger>
                            <TooltipContent>
                                <p>{t("translation:skus.deleteTooltip", "Xóa SKU")}</p>
                            </TooltipContent>
                        </Tooltip>
                    </div>
                );
            }
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

    const onCellValueChanged = async (event: any) => {
        const { data, colDef, newValue, oldValue } = event;
        if (newValue === oldValue) return;

        // Validation for negative price
        if (colDef.field === "referencePrice" && newValue !== null && newValue < 0) {
            toast.error(t("translation:skus.errors.invalidPrice"));
            gridRef.current?.api.refreshInfiniteCache();
            return;
        }

        const updatePayload = {
            name: data.name || null,
            goodsNature: data.goodsNature || null,
            description: data.description || null,
            price: data.referencePrice !== null ? Number(data.referencePrice) : null
        };

        try {
            const response = await skuService.updateSku(data.id, updatePayload);
            if (response.success) {
                toast.success(t("translation:skus.updateSuccess"));
            } else {
                toast.error(t("translation:skus.errors.generic"));
            }
        } catch (error: any) {
            console.error("Error updating SKU:", error);
            const status = error.response?.status;
            let errorKey = t("translation:skus.errors.updateFailed");
            if (status === 400) {
                errorKey = t("translation:skus.errors.validationFailed");
            }
            toast.error(errorKey);
        } finally {
            gridRef.current?.api.refreshInfiniteCache();
        }
    };

    const confirmDeleteSku = async () => {
        if (!skuToDelete) return;
        try {
            setIsDeleting(true);
            const response = await skuService.deleteSku(skuToDelete.id);
            if (response.success) {
                toast.success(t("translation:skus.deleteSuccess"));
                setIsDeleteOpen(false);
                setSkuToDelete(null);
                gridRef.current?.api.refreshInfiniteCache();
            } else {
                toast.error(t("translation:skus.errors.deleteFailed"));
            }
        } catch (error: any) {
            console.error("Error deleting SKU:", error);
            toast.error(t("translation:skus.errors.deleteFailed"));
        } finally {
            setIsDeleting(false);
        }
    };

    const handleSubmit = async (e: SubmitEvent) => {
        e.preventDefault();
        setFormErrors({});

        // Validate
        const validation = createSkuSchema.safeParse({
            productId: formValues.productId || null,
            skuCode: formValues.skuCode || null,
            name: formValues.name || null,
            goodsNature: formValues.goodsNature || null,
            description: formValues.description || null,
            price: formValues.price || null
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
            const response = await skuService.createSku({
                productId: validation.data.productId,
                skuCode: validation.data.skuCode,
                name: validation.data.name,
                goodsNature: validation.data.goodsNature,
                description: validation.data.description,
                price: validation.data.price
            });

            if (response.success) {
                toast.success(t("translation:skus.createSuccess"));
                setIsCreateOpen(false);
                // Reset form
                setFormValues({
                    productId: "",
                    productCode: "",
                    productName: "",
                    skuCode: "",
                    name: "",
                    goodsNature: "",
                    description: "",
                    price: ""
                });
                setProductSearch("");
                setProducts([]);
                // Refresh grid
                gridRef.current?.api.refreshInfiniteCache();
            } else {
                toast.error(t("translation:skus.errors.generic"));
            }
        } catch (error: any) {
            console.error("Error creating SKU:", error);
            const status = error.response?.status;
            let errorKey = t("translation:skus.errors.generic");
            if (status === 409) {
                errorKey = t("translation:skus.errors.duplicateSku");
            } else if (status === 404) {
                errorKey = t("translation:skus.errors.productNotFound");
            } else if (status === 400) {
                errorKey = t("translation:skus.errors.validationFailed");
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
                        placeholder={t("skus.searchPlaceholder", "Tìm theo mã SKU, tên hàng hóa")}
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
                        {t("translation:skus.addSku", "Thêm SKU")}
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
                        stopEditingWhenCellsLoseFocus={true}
                        singleClickEdit={true}
                        onCellValueChanged={onCellValueChanged}
                        defaultColDef={{
                            resizable: true,
                            sortable: false,
                            filter: false,
                            minWidth: 120
                        }}
                    />
                </div>
            </div>

            {/* Create SKU Dialog */}
            <Dialog open={isCreateOpen} onOpenChange={(open) => {
                setIsCreateOpen(open);
                if (!open) {
                    setFormErrors({});
                    setFormValues({
                        productId: "",
                        productCode: "",
                        productName: "",
                        skuCode: "",
                        name: "",
                        goodsNature: "",
                        description: "",
                        price: ""
                    });
                    setProductSearch("");
                    setProducts([]);
                }
            }}>
                <DialogContent className="sm:max-w-[425px] bg-card border border-border text-card-foreground">
                    <DialogHeader>
                        <DialogTitle>{t("translation:skus.createTitle")}</DialogTitle>
                    </DialogHeader>

                    <form onSubmit={handleSubmit} className="flex flex-col gap-4 py-2">
                        {/* Product Selection */}
                        <div className="flex flex-col gap-1.5 relative" ref={productContainerRef}>
                            <label htmlFor="product" className="text-sm font-medium text-foreground">
                                {t("skus.productId")} <span className="text-red-500">*</span>
                            </label>
                            <div className="relative">
                                <Input
                                    id="product"
                                    value={productSearch}
                                    onChange={(e) => {
                                        setProductSearch(e.target.value);
                                        if (formValues.productId) {
                                            setFormValues(prev => ({ ...prev, productId: "", productCode: "", productName: "" }));
                                        }
                                        setIsProductListOpen(true);
                                        fetchProducts(e.target.value);
                                    }}
                                    onFocus={handleProductFocus}
                                    placeholder={t("skus.productIdPlaceholder", "Tìm kiếm sản phẩm...")}
                                    className="w-full bg-background border-border pr-8"
                                />
                                {productSearch && (
                                    <button
                                        type="button"
                                        onClick={() => {
                                            setProductSearch("");
                                            setFormValues(prev => ({ ...prev, productId: "", productCode: "", productName: "" }));
                                            setProducts([]);
                                            setIsProductListOpen(false);
                                        }}
                                        className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground text-xs font-semibold"
                                    >
                                        ✕
                                    </button>
                                )}
                            </div>
                            {formErrors.productId && (
                                <span className="text-xs text-red-500">{formErrors.productId}</span>
                            )}

                            {isProductListOpen && (
                                <div className="absolute top-full left-0 w-full z-50 mt-1 border border-border bg-popover text-popover-foreground rounded-md shadow-lg overflow-hidden">
                                    <ScrollArea className="h-40">
                                        <div className="p-1">
                                            {isProductLoading ? (
                                                <div className="p-2 text-xs text-muted-foreground text-center">{t("common.loading", "Đang tải...")}</div>
                                            ) : products.length === 0 ? (
                                                <div className="p-2 text-xs text-muted-foreground text-center">{t("common.noData", "Không có dữ liệu")}</div>
                                            ) : (
                                                products.map((prod) => (
                                                    <button
                                                        key={prod.id}
                                                        type="button"
                                                        onClick={() => {
                                                            setFormValues(prev => ({
                                                                ...prev,
                                                                productId: prod.id,
                                                                productCode: prod.productCode,
                                                                productName: prod.productName
                                                            }));
                                                            setProductSearch(prod.productName ? `${prod.productName} (${prod.productCode})` : prod.productCode);
                                                            setIsProductListOpen(false);
                                                        }}
                                                        className="w-full text-left px-3 py-1.5 text-sm rounded-sm hover:bg-accent hover:text-accent-foreground text-foreground transition-colors"
                                                    >
                                                        {prod.productName} ({prod.productCode})
                                                    </button>
                                                ))
                                            )}
                                        </div>
                                    </ScrollArea>
                                </div>
                            )}
                        </div>

                        {/* SKU Code */}
                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="skuCode" className="text-sm font-medium text-foreground">
                                {t("skus.skuCode")}
                            </label>
                            <Input
                                id="skuCode"
                                value={formValues.skuCode}
                                onChange={(e) => setFormValues(prev => ({ ...prev, skuCode: e.target.value }))}
                                placeholder={t("skus.skuCodePlaceholder")}
                                className="w-full bg-background border-border"
                            />
                            {formErrors.skuCode && (
                                <span className="text-xs text-red-500">{formErrors.skuCode}</span>
                            )}
                        </div>

                        {/* SKU Name */}
                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="name" className="text-sm font-medium text-foreground">
                                {t("skus.name")}
                            </label>
                            <Input
                                id="name"
                                value={formValues.name}
                                onChange={(e) => setFormValues(prev => ({ ...prev, name: e.target.value }))}
                                placeholder={t("skus.skuNamePlaceholder")}
                                className="w-full bg-background border-border"
                            />
                        </div>

                        {/* Goods Nature */}
                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="goodsNature" className="text-sm font-medium text-foreground">
                                {t("skus.goodsNature")}
                            </label>
                            <Input
                                id="goodsNature"
                                value={formValues.goodsNature}
                                onChange={(e) => setFormValues(prev => ({ ...prev, goodsNature: e.target.value }))}
                                placeholder={t("skus.goodsNaturePlaceholder")}
                                className="w-full bg-background border-border"
                            />
                        </div>

                        {/* Price */}
                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="price" className="text-sm font-medium text-foreground">
                                {t("skus.referencePrice")}
                            </label>
                            <Input
                                id="price"
                                type="number"
                                step="any"
                                value={formValues.price}
                                onChange={(e) => setFormValues(prev => ({ ...prev, price: e.target.value }))}
                                placeholder={t("skus.pricePlaceholder")}
                                className="w-full bg-background border-border"
                            />
                            {formErrors.price && (
                                <span className="text-xs text-red-500">{formErrors.price}</span>
                            )}
                        </div>

                        {/* Description */}
                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="description" className="text-sm font-medium text-foreground">
                                {t("skus.description")}
                            </label>
                            <Input
                                id="description"
                                value={formValues.description}
                                onChange={(e) => setFormValues(prev => ({ ...prev, description: e.target.value }))}
                                placeholder={t("skus.descriptionPlaceholder")}
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
                                {t("common.button.cancel")}
                            </Button>
                            <Button
                                type="submit"
                                disabled={isSaving}
                                className="bg-primary text-primary-foreground hover:bg-primary/95"
                            >
                                {isSaving ? t("common.loading") : t("common.button.save")}
                            </Button>
                        </DialogFooter>
                    </form>
                </DialogContent>
            </Dialog>

            {/* Delete SKU Dialog */}
            <Dialog open={isDeleteOpen} onOpenChange={setIsDeleteOpen}>
                <DialogContent className="sm:max-w-[400px] bg-card border border-border text-card-foreground">
                    <DialogHeader>
                        <DialogTitle>{t("translation:skus.deleteTitle")}</DialogTitle>
                    </DialogHeader>
                    <div className="py-4 text-sm text-muted-foreground flex flex-col gap-2">
                        <p>{t("translation:skus.confirmDelete")}</p>
                        {skuToDelete && (
                            <div className="p-2 rounded bg-muted/50 border border-border mt-1 font-semibold text-foreground text-xs">
                                {skuToDelete.name || skuToDelete.skuCode} ({skuToDelete.skuCode})
                            </div>
                        )}
                    </div>
                    <DialogFooter>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => {
                                setIsDeleteOpen(false);
                                setSkuToDelete(null);
                            }}
                            disabled={isDeleting}
                            className="border-border text-muted-foreground hover:text-foreground"
                        >
                            {t("common.button.cancel")}
                        </Button>
                        <Button
                            type="button"
                            variant="destructive"
                            onClick={confirmDeleteSku}
                            disabled={isDeleting}
                        >
                            {isDeleting ? t("common.loading") : t("common.button.delete")}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}

