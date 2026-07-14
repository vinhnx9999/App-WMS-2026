import * as React from "react";
import { useTranslation } from "react-i18next";
import { ArrowLeft, Loader2, Plus } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { useForm, useFieldArray, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Separator } from "@/components/ui/separator";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from "@/components/ui/form";

import { PurchaseOrderItemRow } from "../components/PurchaseOrderItemRow";
import {
  CreatePOFormSchema,
  type CreatePOFormValues,
} from "../models/create-po-form.model";import type { CreatePORequest } from "../models/inbound.model";import { inboundService } from "../services/inbound.service";


export default function PurchaseOrderCreatePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  // We don't strictly need warehouseId for creating PO per schema, but keeping the store is fine.
  
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  const [showExitDialog, setShowExitDialog] = React.useState(false);

  const form = useForm<CreatePOFormValues>({
    resolver: zodResolver(CreatePOFormSchema),
    defaultValues: {
      orderNumber: "",
      expectedDate: "",
      notes: "",
      items: [],
    },
  });

  const {
    control,
    handleSubmit,
    formState: { isDirty, errors },
  } = form;

  const { fields, append, remove } = useFieldArray({
    control,
    name: "items",
  });

  const onSubmit = async (data: CreatePOFormValues) => {
    try {
      setIsSubmitting(true);
      const request: CreatePORequest = {
        orderNumber: data.orderNumber || undefined,
        expectedDate: data.expectedDate || undefined,
        notes: data.notes || undefined,
        items: data.items.map((item) => ({
          skuId: item.skuId,
          quantity: item.quantity,
          supplierId: item.supplierId || undefined,
        })),
      };

      const response = await inboundService.createPurchaseOrder(request);

      if (response.success && response.data) {
        toast.success(t("inbound.po.create.success", "Purchase order created successfully"));
        // Reset form so exit dialog doesn't trigger on navigate
        form.reset(data);
        navigate(`/inbound/po/${response.data.id}`);
      } else {
        toast.error(response.message || "Failed to create purchase order");
      }
    } catch (error) {
      toast.error("An unexpected error occurred");
      console.error(error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleBackClick = (e: React.MouseEvent) => {
    if (isDirty) {
      e.preventDefault();
      setShowExitDialog(true);
    }
  };

  const handleConfirmExit = () => {
    setShowExitDialog(false);
    navigate("/inbound/po");
  };

  return (
    <FormProvider {...form}>
      <form
        onSubmit={handleSubmit(onSubmit)}
        className="h-full w-full flex flex-col overflow-hidden bg-background"
      >
        {/* Sticky Header */}
        <div className="flex items-center justify-between p-4 border-b bg-secondary/10 shrink-0">
          <div className="flex items-center gap-4">
            <Link to="/inbound/po" onClick={handleBackClick}>
              <Button type="button" variant="ghost" size="icon-xs">
                <ArrowLeft className="size-4" />
              </Button>
            </Link>
            <h2 className="text-lg font-bold">
              {t("inbound.po.create.title", "Create Purchase Order")}
            </h2>
          </div>
          <div className="flex items-center gap-2">
            <Link to="/inbound/po" onClick={handleBackClick}>
              <Button type="button" variant="outline" size="sm">
                {t("common.button.cancel", "Cancel")}
              </Button>
            </Link>
            <Button type="submit" size="sm" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="size-4 mr-2 animate-spin" />}
              {t("common.button.submit", "Submit")}
            </Button>
          </div>
        </div>

        {/* Scrollable Body */}
        <div className="flex-1 flex flex-col min-h-0 p-4 sm:p-6 gap-6">
          <Card className="shrink-0">
            <CardHeader className="pb-4">
              <CardTitle className="text-base font-semibold">General Information</CardTitle>
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2">
              <FormField
                control={control}
                name="orderNumber"
                render={({ field }) => (
                  <FormItem>
                    <Label>PO Code</Label>
                    <FormControl>
                      <Input placeholder="Leave blank to auto-generate" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={control}
                name="expectedDate"
                render={({ field }) => (
                  <FormItem>
                    <Label>Expected Date</Label>
                    <FormControl>
                      <Input type="date" {...field} value={field.value || ""} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={control}
                name="notes"
                render={({ field }) => (
                  <FormItem className="sm:col-span-2">
                    <Label>Notes</Label>
                    <FormControl>
                      <Textarea
                        placeholder="Add any additional notes here..."
                        {...field}
                        value={field.value || ""}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </CardContent>
          </Card>

          <Card className="flex-1 flex flex-col min-h-0">
            <CardHeader className="pb-4 flex flex-row items-center justify-between shrink-0">
              <CardTitle className="text-base font-semibold flex items-center gap-2">
                Line Items
                <span className="bg-primary/10 text-primary text-xs px-2 py-0.5 rounded-full font-bold">
                  {fields.length}
                </span>
              </CardTitle>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() =>
                  append({
                    skuId: "",
                    quantity: 1,
                    supplierId: null,
                  })
                }
              >
                <Plus className="size-4 mr-1" />
                Add Item
              </Button>
            </CardHeader>
            <Separator className="shrink-0" />
            <CardContent className="flex-1 overflow-y-auto pt-6">
              {errors.items?.root && (
                <div className="mb-4 text-sm font-medium text-destructive">
                  {errors.items.root.message}
                </div>
              )}
              {fields.length === 0 ? (
                <div className="text-center py-12 border-2 border-dashed rounded-lg text-muted-foreground">
                  <p className="text-sm">No items added yet.</p>
                  <Button
                    type="button"
                    variant="link"
                    onClick={() =>
                      append({
                        skuId: "",
                        quantity: 1,
                        supplierId: null,
                      })
                    }
                  >
                    Click here to add the first item
                  </Button>
                </div>
              ) : (
                <div className="flex flex-col gap-4">
                  {fields.map((field, index) => (
                    <PurchaseOrderItemRow
                      key={field.id}
                      index={index}
                      control={control}
                      onRemove={() => remove(index)}
                    />
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </form>

      {/* Unsaved Changes Dialog */}
      <AlertDialog open={showExitDialog} onOpenChange={setShowExitDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Unsaved Changes</AlertDialogTitle>
            <AlertDialogDescription>
              You have unsaved changes. Are you sure you want to leave this page?
              Your changes will be lost.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Stay</AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirmExit} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Discard Changes
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </FormProvider>
  );
}
