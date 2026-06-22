import { useState, useMemo, useEffect, useRef, type FormEvent } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Search, Plus, Trash, Loader2 } from "lucide-react";
import { debounce } from "lodash";
import { z } from "zod";
import { toast } from "sonner";
import { categoryService } from "./services/category.service";
import type { CategoryDto } from "./models/category.model";
import { useAgGridTheme } from "@/hooks/use-ag-grid-theme";
import { DEFAULT_PAGE_SIZE } from "@/constants";

import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogFooter
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

export default function CategoryListPage() {
    const { t } = useTranslation();

    const createCategorySchema = useMemo(() => z.object({
        name: z.string()
            .min(1, t("translation:categories.errors.nameRequired"))
            .max(200, t("translation:categories.errors.nameTooLong")),
        description: z.string()
            .max(500, t("translation:categories.errors.descriptionTooLong"))
            .nullable()
            .optional()
    }), [t]);
    const [searchValue, setSearchValue] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    const gridRef = useRef<AgGridReact>(null);
    const gridTheme = useAgGridTheme();
    const [isLoading, setIsLoading] = useState(false);
    const isProcessingRef = useRef(false);

    // Create Modal State
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [formValues, setFormValues] = useState({
        name: "",
        description: ""
    });
    const [formErrors, setFormErrors] = useState<Record<string, string>>({});

    // Delete Modal State
    const [isDeleteOpen, setIsDeleteOpen] = useState(false);
    const [categoryToDelete, setCategoryToDelete] = useState<CategoryDto | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);

    // Search debounce setup
    const debouncedSetSearch = useMemo(
        () => debounce((val: string) => setDebouncedSearch(val), 500),
        []
    );

    useEffect(() => {
        return () => {
            debouncedSetSearch.cancel();
        };
    }, [debouncedSetSearch]);

    const columnDefs = useMemo<ColDef<CategoryDto>[]>(() => [
        {
            field: "name",
            headerName: t("translation:categories.categoryName"),
            pinned: "left",
            width: 250,
            editable: true,
            cellRenderer: (params: any) => {
                if (!params.value) return null;
                return (
                    <span className="font-semibold text-primary">
                        {params.value}
                    </span>
                );
            }
        },
        {
            field: "slug",
            headerName: t("translation:categories.slug"),
            width: 200,
            editable: false,
            cellRenderer: (params: any) => {
                if (!params.value) return <span className="text-muted-foreground">-</span>;
                return (
                    <span className="font-mono text-xs text-muted-foreground">
                        {params.value}
                    </span>
                );
            }
        },
        {
            field: "description",
            headerName: t("translation:categories.description"),
            flex: 1,
            minWidth: 250,
            editable: true,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "createdAt",
            headerName: t("translation:categories.createdAt"),
            width: 150,
            editable: false,
            valueFormatter: (params) => {
                if (!params.value) return "";
                return new Date(params.value).toLocaleDateString("vi-VN");
            }
        },
        {
            headerName: t("translation:categories.actions"),
            pinned: "right",
            width: 90,
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
                                        setCategoryToDelete(params.data);
                                        setIsDeleteOpen(true);
                                    }}
                                >
                                    <Trash className="size-3.5" />
                                </Button>
                            </TooltipTrigger>
                            <TooltipContent>
                                <p>{t("translation:categories.deleteTooltip")}</p>
                            </TooltipContent>
                        </Tooltip>
                    </div>
                );
            }
        }
    ], [t]);

    const datasource = useMemo<IDatasource>(() => {
        return {
            getRows: async (params: IGetRowsParams) => {
                try {
                    setIsLoading(true);

                    const page = Math.floor(params.startRow / DEFAULT_PAGE_SIZE) + 1;

                    const response = await categoryService.searchCategories({
                        search: debouncedSearch || undefined,
                        page,
                        limit: DEFAULT_PAGE_SIZE
                    });

                    setIsLoading(false);

                    if (response.success && response.data) {
                        const { items, totalCount } = response.data;
                        params.successCallback(items, totalCount);
                    } else {
                        params.failCallback();
                    }
                } catch (error) {
                    console.error("Lỗi khi tải danh sách nhóm sản phẩm:", error);
                    setIsLoading(false);
                    params.failCallback();
                }
            }
        };
    }, [debouncedSearch]);

    const onCellValueChanged = async (event: any) => {
        if (isProcessingRef.current) return;

        const { data, colDef, newValue, oldValue } = event;
        if (newValue === oldValue) return;

        // Perform Client-side validations
        if (colDef.field === "name" && (!newValue || newValue.trim() === "")) {
            toast.error(t("translation:categories.errors.nameRequired"));
            isProcessingRef.current = true;
            try {
                event.node.setDataValue(colDef.field, oldValue);
            } finally {
                setTimeout(() => { isProcessingRef.current = false; }, 100);
            }
            return;
        }

        if (colDef.field === "name" && newValue.length > 200) {
            toast.error(t("translation:categories.errors.nameTooLong"));
            isProcessingRef.current = true;
            try {
                event.node.setDataValue(colDef.field, oldValue);
            } finally {
                setTimeout(() => { isProcessingRef.current = false; }, 100);
            }
            return;
        }

        if (colDef.field === "description" && newValue && newValue.length > 500) {
            toast.error(t("translation:categories.errors.descriptionTooLong"));
            isProcessingRef.current = true;
            try {
                event.node.setDataValue(colDef.field, oldValue);
            } finally {
                setTimeout(() => { isProcessingRef.current = false; }, 100);
            }
            return;
        }

        isProcessingRef.current = true;

        try {
            const updatePayload = {
                name: (data.name && data.name.trim()) || "",
                description: (data.description && data.description.trim()) || null
            };

            const response = await categoryService.updateCategory(data.id, updatePayload);
            if (response.success) {
                toast.success(t("translation:categories.updateSuccess"));
                gridRef.current?.api.refreshInfiniteCache();
            } else {
                toast.error(t("translation:categories.errors.generic"));
                event.node.setDataValue(colDef.field, oldValue);
            }
        } catch (error: any) {
            console.error("Error updating category:", error);
            const status = error.response?.status;
            let errorMsg = t("translation:categories.errors.generic");
            if (status === 400) {
                errorMsg = t("translation:categories.errors.validationFailed");
            }
            toast.error(errorMsg);
            event.node.setDataValue(colDef.field, oldValue);
        } finally {
            setTimeout(() => { isProcessingRef.current = false; }, 100);
        }
    };

    const confirmDeleteCategory = async () => {
        if (!categoryToDelete) return;
        try {
            setIsDeleting(true);
            const response = await categoryService.deleteCategory(categoryToDelete.id);
            if (response.success) {
                toast.success(t("translation:categories.deleteSuccess"));
                setIsDeleteOpen(false);
                setCategoryToDelete(null);
                gridRef.current?.api.refreshInfiniteCache();
            } else {
                toast.error(t("translation:categories.errors.generic"));
            }
        } catch (error: any) {
            console.error("Error deleting category:", error);
            const status = error.response?.status;
            const errorCode = error.response?.data?.code;
            let errorMsg = t("translation:categories.errors.generic");

            if (status === 400 && errorCode === "CATEGORY_IN_USE") {
                errorMsg = t("translation:categories.errors.categoryInUse");
            } else if (status === 400) {
                errorMsg = t("translation:categories.errors.validationFailed");
            }

            toast.error(errorMsg);
            setIsDeleteOpen(false);
            setCategoryToDelete(null);
        } finally {
            setIsDeleting(false);
        }
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setFormErrors({});

        const validation = createCategorySchema.safeParse({
            name: formValues.name,
            description: formValues.description || null
        });

        if (!validation.success) {
            const errors: Record<string, string> = {};
            validation.error.issues.forEach(err => {
                const fieldName = err.path[0] as string;
                if (fieldName) {
                    errors[fieldName] = err.message;
                }
            });
            setFormErrors(errors);
            return;
        }

        try {
            setIsSaving(true);
            const response = await categoryService.createCategory({
                name: validation.data.name,
                description: validation.data.description ?? null
            });

            if (response.success) {
                toast.success(t("translation:categories.createSuccess"));
                setIsCreateOpen(false);
                setFormValues({
                    name: "",
                    description: ""
                });
                gridRef.current?.api.refreshInfiniteCache();
            } else {
                toast.error(t("translation:categories.errors.generic"));
            }
        } catch (error: any) {
            console.error("Error creating category:", error);
            const status = error.response?.status;
            let errorMsg = t("translation:categories.errors.generic");
            if (status === 400) {
                errorMsg = t("translation:categories.errors.validationFailed");
            }
            toast.error(errorMsg);
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="h-full w-full flex flex-col overflow-hidden">
            {/* Header / Tools */}
            <div className="flex items-center justify-between gap-3 shrink-0 bg-card text-card-foreground p-3">
                <div className="relative flex-1 max-w-md">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                    <input
                        type="text"
                        placeholder={t("translation:categories.searchPlaceholder")}
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
                        {t("translation:categories.addCategory")}
                    </Button>
                </div>
            </div>

            {/* Grid container */}
            <div className="flex-1 w-full overflow-hidden relative">
                <div className="w-full h-full">
                    <AgGridReact
                        ref={gridRef}
                        columnDefs={columnDefs}
                        datasource={datasource}
                        rowModelType="infinite"
                        cacheBlockSize={DEFAULT_PAGE_SIZE}
                        maxConcurrentDatasourceRequests={1}
                        infiniteInitialRowCount={DEFAULT_PAGE_SIZE}
                        pagination={true}
                        paginationPageSize={DEFAULT_PAGE_SIZE}
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
                            sortable: true,
                            filter: true,
                            minWidth: 100
                        }}
                    />
                </div>
            </div>

            {/* Creation Dialog */}
            <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
                <DialogContent className="sm:max-w-[480px] bg-card border border-border text-card-foreground">
                    <DialogHeader>
                        <DialogTitle>{t("translation:categories.createTitle")}</DialogTitle>
                    </DialogHeader>
                    <form onSubmit={handleSubmit} className="space-y-4 py-2">
                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="name" className="text-sm font-medium text-foreground">
                                {t("translation:categories.categoryName")} <span className="text-destructive">*</span>
                            </label>
                            <Input
                                id="name"
                                value={formValues.name}
                                onChange={(e) => setFormValues(prev => ({ ...prev, name: e.target.value }))}
                                placeholder={t("translation:categories.categoryNamePlaceholder")}
                                className={`w-full bg-background border-border ${formErrors.name ? "border-destructive focus-visible:ring-destructive" : ""}`}
                            />
                            {formErrors.name ? (
                                <span className="text-xs text-destructive">{formErrors.name}</span>
                            ) : null}
                        </div>

                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="description" className="text-sm font-medium text-foreground">
                                {t("translation:categories.description")}
                            </label>
                            <textarea
                                id="description"
                                value={formValues.description}
                                onChange={(e) => setFormValues(prev => ({ ...prev, description: e.target.value }))}
                                placeholder={t("translation:categories.descriptionPlaceholder")}
                                rows={4}
                                className={`w-full bg-background border border-border rounded-md px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all ${formErrors.description ? "border-destructive focus-visible:ring-destructive" : ""}`}
                            />
                            {formErrors.description ? (
                                <span className="text-xs text-destructive">{formErrors.description}</span>
                            ) : null}
                        </div>

                        <DialogFooter className="pt-2">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setIsCreateOpen(false)}
                                disabled={isSaving}
                                className="border-border text-muted-foreground hover:text-foreground"
                            >
                                {t("translation:common.button.cancel")}
                            </Button>
                            <Button
                                type="submit"
                                disabled={isSaving}
                                className="bg-primary text-primary-foreground hover:bg-primary/95 flex items-center gap-1.5"
                            >
                                {isSaving ? <Loader2 className="size-4 animate-spin" /> : null}
                                {t("translation:common.button.save")}
                            </Button>
                        </DialogFooter>
                    </form>
                </DialogContent>
            </Dialog>

            {/* Deletion Dialog */}
            <Dialog open={isDeleteOpen} onOpenChange={setIsDeleteOpen}>
                <DialogContent className="sm:max-w-[400px] bg-card border border-border text-card-foreground">
                    <DialogHeader>
                        <DialogTitle>{t("translation:categories.deleteTitle")}</DialogTitle>
                    </DialogHeader>
                    <div className="py-4 text-sm text-muted-foreground flex flex-col gap-2">
                        <p>{t("translation:categories.confirmDelete")}</p>
                        {categoryToDelete ? (
                            <div className="p-2 rounded bg-muted/50 border border-border mt-1 font-semibold text-foreground text-xs">
                                {categoryToDelete.name}
                            </div>
                        ) : null}
                    </div>
                    <DialogFooter>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => {
                                setIsDeleteOpen(false);
                                setCategoryToDelete(null);
                            }}
                            disabled={isDeleting}
                            className="border-border text-muted-foreground hover:text-foreground"
                        >
                            {t("translation:common.button.cancel")}
                        </Button>
                        <Button
                            type="button"
                            variant="destructive"
                            onClick={confirmDeleteCategory}
                            disabled={isDeleting}
                            className="flex items-center gap-1.5"
                        >
                            {isDeleting ? <Loader2 className="size-4 animate-spin" /> : null}
                            {t("translation:common.button.delete")}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}
