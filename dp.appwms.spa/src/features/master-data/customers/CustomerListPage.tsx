import { useState, useMemo, useEffect, useRef, type SubmitEvent } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, IDatasource, IGetRowsParams } from "ag-grid-community";
import { useTranslation } from "react-i18next";
import { Search, Plus, Trash, Loader2 } from "lucide-react";
import { debounce } from "lodash";
import { z } from "zod";
import { toast } from "sonner";
import { customerService } from "./services/customer.service";
import type { CustomerDto } from "./models/customer.model";
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

export default function CustomerListPage() {
    const { t } = useTranslation();

    const createCustomerSchema = useMemo(() => z.object({
        code: z.string()
            .max(100, t("translation:customers.errors.codeTooLong"))
            .nullable()
            .optional()
            .or(z.literal("")),
        name: z.string()
            .min(1, t("translation:customers.errors.nameRequired"))
            .max(100, t("translation:customers.errors.nameTooLong")),
        address: z.string().max(250, t("translation:customers.errors.addressTooLong")).nullable().optional().or(z.literal("")),
        phone: z.string().max(50, t("translation:customers.errors.phoneTooLong")).nullable().optional().or(z.literal("")),
        type: z.string().max(50, t("translation:customers.errors.typeTooLong")).nullable().optional().or(z.literal(""))
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
        code: "",
        name: "",
        address: "",
        phone: "",
        type: ""
    });
    const [formErrors, setFormErrors] = useState<Record<string, string>>({});

    // Delete Modal State
    const [isDeleteOpen, setIsDeleteOpen] = useState(false);
    const [customerToDelete, setCustomerToDelete] = useState<CustomerDto | null>(null);
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

    const columnDefs = useMemo<ColDef<CustomerDto>[]>(() => [
        {
            field: "code",
            headerName: t("translation:customers.customerCode"),
            pinned: "left",
            width: 150,
            editable: false,
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
            field: "name",
            headerName: t("translation:customers.customerName"),
            width: 250,
            editable: true
        },
        {
            field: "address",
            headerName: t("translation:customers.address"),
            flex: 1,
            minWidth: 250,
            editable: true,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "phone",
            headerName: t("translation:customers.phone"),
            width: 150,
            editable: true,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "type",
            headerName: t("translation:customers.type"),
            width: 150,
            editable: true,
            valueFormatter: (params) => params.value || ""
        },
        {
            field: "createdAt",
            headerName: t("translation:customers.createdAt"),
            width: 150,
            editable: false,
            valueFormatter: (params) => {
                if (!params.value) return "";
                return new Date(params.value).toLocaleDateString("vi-VN");
            }
        },
        {
            headerName: t("translation:customers.actions"),
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
                                        setCustomerToDelete(params.data);
                                        setIsDeleteOpen(true);
                                    }}
                                >
                                    <Trash className="size-3.5" />
                                </Button>
                            </TooltipTrigger>
                            <TooltipContent>
                                <p>{t("translation:customers.deleteTooltip")}</p>
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

                    const response = await customerService.searchCustomers({
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
                    console.error("Lỗi khi tải danh sách khách hàng:", error);
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
            toast.error(t("translation:customers.errors.nameRequired"));
            isProcessingRef.current = true;
            try {
                event.node.setDataValue(colDef.field, oldValue);
            } finally {
                setTimeout(() => { isProcessingRef.current = false; }, 100);
            }
            return;
        }

        if (colDef.field === "name" && newValue.length > 100) {
            toast.error(t("translation:customers.errors.nameTooLong"));
            isProcessingRef.current = true;
            try {
                event.node.setDataValue(colDef.field, oldValue);
            } finally {
                setTimeout(() => { isProcessingRef.current = false; }, 100);
            }
            return;
        }

        if (colDef.field === "address" && newValue && newValue.length > 250) {
            toast.error(t("translation:customers.errors.addressTooLong"));
            isProcessingRef.current = true;
            try {
                event.node.setDataValue(colDef.field, oldValue);
            } finally {
                setTimeout(() => { isProcessingRef.current = false; }, 100);
            }
            return;
        }

        if (colDef.field === "phone" && newValue && newValue.length > 50) {
            toast.error(t("translation:customers.errors.phoneTooLong"));
            isProcessingRef.current = true;
            try {
                event.node.setDataValue(colDef.field, oldValue);
            } finally {
                setTimeout(() => { isProcessingRef.current = false; }, 100);
            }
            return;
        }

        if (colDef.field === "type" && newValue && newValue.length > 50) {
            toast.error(t("translation:customers.errors.typeTooLong"));
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
                address: (data.address && data.address.trim()) || null,
                phone: (data.phone && data.phone.trim()) || null,
                type: (data.type && data.type.trim()) || null
            };

            const response = await customerService.updateCustomer(data.id, updatePayload);
            if (response.success) {
                toast.success(t("translation:customers.updateSuccess"));
                gridRef.current?.api.refreshInfiniteCache();
            } else {
                toast.error(t("translation:customers.errors.generic"));
                event.node.setDataValue(colDef.field, oldValue);
            }
        } catch (error: any) {
            console.error("Error updating customer:", error);
            const status = error.response?.status;
            let errorMsg = t("translation:customers.errors.generic");
            if (status === 400) {
                errorMsg = t("translation:customers.errors.validationFailed");
            }
            toast.error(errorMsg);
            event.node.setDataValue(colDef.field, oldValue);
        } finally {
            setTimeout(() => { isProcessingRef.current = false; }, 100);
        }
    };

    const confirmDeleteCustomer = async () => {
        if (!customerToDelete) return;
        try {
            setIsDeleting(true);
            const response = await customerService.deleteCustomer(customerToDelete.id);
            if (response.success) {
                toast.success(t("translation:customers.deleteSuccess"));
                setIsDeleteOpen(false);
                setCustomerToDelete(null);
                gridRef.current?.api.refreshInfiniteCache();
            } else {
                toast.error(t("translation:customers.errors.generic"));
            }
        } catch (error: any) {
            console.error("Error deleting customer:", error);
            const status = error.response?.status;
            let errorMsg = t("translation:customers.errors.generic");
            if (status === 400) {
                errorMsg = t("translation:customers.errors.validationFailed");
            }
            toast.error(errorMsg);
            setIsDeleteOpen(false);
            setCustomerToDelete(null);
        } finally {
            setIsDeleting(false);
        }
    };

    const handleSubmit = async (e: SubmitEvent<HTMLFormElement>) => {
        e.preventDefault();
        setFormErrors({});

        const validation = createCustomerSchema.safeParse({
            code: formValues.code,
            name: formValues.name,
            address: formValues.address || null,
            phone: formValues.phone || null,
            type: formValues.type || null
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
            const response = await customerService.createCustomer({
                code: validation.data.code || null,
                name: validation.data.name,
                address: validation.data.address ?? null,
                phone: validation.data.phone ?? null,
                type: validation.data.type ?? null
            });

            if (response.success) {
                toast.success(t("translation:customers.createSuccess"));
                setIsCreateOpen(false);
                setFormValues({
                    code: "",
                    name: "",
                    address: "",
                    phone: "",
                    type: ""
                });
                gridRef.current?.api.refreshInfiniteCache();
            } else {
                toast.error(t("translation:customers.errors.generic"));
            }
        } catch (error: any) {
            console.error("Error creating customer:", error);
            const status = error.response?.status;
            const errorCode = error.response?.data?.code;
            let errorMsg = t("translation:customers.errors.generic");

            if (status === 400 && errorCode === "CUSTOMER_CODE_ALREADY_EXISTS") {
                errorMsg = t("translation:customers.errors.duplicateCode");
            } else if (status === 400) {
                errorMsg = t("translation:customers.errors.validationFailed");
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
                        placeholder={t("translation:customers.searchPlaceholder")}
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
                        {t("translation:customers.addCustomer")}
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
                        <DialogTitle>{t("translation:customers.createTitle")}</DialogTitle>
                    </DialogHeader>
                    <form onSubmit={handleSubmit} className="space-y-4 py-2">
                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="code" className="text-sm font-medium text-foreground">
                                {t("translation:customers.customerCode")}
                            </label>
                            <Input
                                id="code"
                                value={formValues.code}
                                onChange={(e) => setFormValues(prev => ({ ...prev, code: e.target.value }))}
                                placeholder={t("translation:customers.codePlaceholder")}
                                className={`w-full bg-background border-border ${formErrors.code ? "border-destructive focus-visible:ring-destructive" : ""}`}
                            />
                            {formErrors.code ? (
                                <span className="text-xs text-destructive">{formErrors.code}</span>
                            ) : null}
                        </div>

                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="name" className="text-sm font-medium text-foreground">
                                {t("translation:customers.customerName")} <span className="text-destructive">*</span>
                            </label>
                            <Input
                                id="name"
                                value={formValues.name}
                                onChange={(e) => setFormValues(prev => ({ ...prev, name: e.target.value }))}
                                placeholder={t("translation:customers.namePlaceholder")}
                                className={`w-full bg-background border-border ${formErrors.name ? "border-destructive focus-visible:ring-destructive" : ""}`}
                            />
                            {formErrors.name ? (
                                <span className="text-xs text-destructive">{formErrors.name}</span>
                            ) : null}
                        </div>

                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="address" className="text-sm font-medium text-foreground">
                                {t("translation:customers.address")}
                            </label>
                            <Input
                                id="address"
                                value={formValues.address}
                                onChange={(e) => setFormValues(prev => ({ ...prev, address: e.target.value }))}
                                placeholder={t("translation:customers.addressPlaceholder")}
                                className={`w-full bg-background border-border ${formErrors.address ? "border-destructive focus-visible:ring-destructive" : ""}`}
                            />
                            {formErrors.address ? (
                                <span className="text-xs text-destructive">{formErrors.address}</span>
                            ) : null}
                        </div>

                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="phone" className="text-sm font-medium text-foreground">
                                {t("translation:customers.phone")}
                            </label>
                            <Input
                                id="phone"
                                value={formValues.phone}
                                onChange={(e) => setFormValues(prev => ({ ...prev, phone: e.target.value }))}
                                placeholder={t("translation:customers.phonePlaceholder")}
                                className={`w-full bg-background border-border ${formErrors.phone ? "border-destructive focus-visible:ring-destructive" : ""}`}
                            />
                            {formErrors.phone ? (
                                <span className="text-xs text-destructive">{formErrors.phone}</span>
                            ) : null}
                        </div>

                        <div className="flex flex-col gap-1.5">
                            <label htmlFor="type" className="text-sm font-medium text-foreground">
                                {t("translation:customers.type")}
                            </label>
                            <Input
                                id="type"
                                value={formValues.type}
                                onChange={(e) => setFormValues(prev => ({ ...prev, type: e.target.value }))}
                                placeholder={t("translation:customers.typePlaceholder")}
                                className={`w-full bg-background border-border ${formErrors.type ? "border-destructive focus-visible:ring-destructive" : ""}`}
                            />
                            {formErrors.type ? (
                                <span className="text-xs text-destructive">{formErrors.type}</span>
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
                        <DialogTitle>{t("translation:customers.deleteTitle")}</DialogTitle>
                    </DialogHeader>
                    <div className="py-4 text-sm text-muted-foreground flex flex-col gap-2">
                        <p>{t("translation:customers.confirmDelete")}</p>
                        {customerToDelete ? (
                            <div className="p-2 rounded bg-muted/50 border border-border mt-1 font-semibold text-foreground text-xs">
                                {customerToDelete.code} - {customerToDelete.name}
                            </div>
                        ) : null}
                    </div>
                    <DialogFooter>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => {
                                setIsDeleteOpen(false);
                                setCustomerToDelete(null);
                            }}
                            disabled={isDeleting}
                            className="border-border text-muted-foreground hover:text-foreground"
                        >
                            {t("translation:common.button.cancel")}
                        </Button>
                        <Button
                            type="button"
                            variant="destructive"
                            onClick={confirmDeleteCustomer}
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