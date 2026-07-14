import { type Control, useFormContext } from "react-hook-form";
import { Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { SkuCombobox } from "./SkuCombobox";
import { SupplierSelect } from "./SupplierSelect";
import type { CreatePOFormValues } from "../models/create-po-form.model";

interface PurchaseOrderItemRowProps {
  index: number;
  control: Control<CreatePOFormValues>;
  onRemove: () => void;
}

export function PurchaseOrderItemRow({
  index,
  control,
  onRemove,
}: PurchaseOrderItemRowProps) {
  // We use useFormContext if needed, but since we have control passed down, we'll use that for FormField.
  // We also need setValue to update display-only fields (skuCode, skuName, supplierName) 
  // when the user selects something, so they are stored in the form state.
  const { setValue } = useFormContext<CreatePOFormValues>();

  return (
    <div className="flex flex-col sm:flex-row items-start gap-3 p-2 sm:p-3 border rounded-md bg-card relative">
      <div className="pt-2 font-medium text-sm text-muted-foreground w-6 shrink-0">
        #{index + 1}
      </div>

      {/* SKU Field */}
      <div className="flex-1 min-w-[250px]">
        <FormField
          control={control}
          name={`items.${index}.skuId`}
          render={({ field }) => (
            <FormItem className="flex flex-col">
              <FormControl>
                <SkuCombobox
                  value={field.value}
                  onChange={(id, code, name) => {
                    field.onChange(id);
                    setValue(`items.${index}.skuCode`, code, { shouldDirty: true });
                    setValue(`items.${index}.skuName`, name, { shouldDirty: true });
                  }}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>

      {/* Quantity Field */}
      <div className="w-full sm:w-[150px]">
        <FormField
          control={control}
          name={`items.${index}.quantity`}
          render={({ field }) => (
            <FormItem className="flex flex-col">
              <FormControl>
                <Input
                  className="h-8 text-sm"
                  type="number"
                  placeholder="Qty"
                  {...field}
                  onChange={(e) => field.onChange(e.target.valueAsNumber || 0)}
                  value={field.value || ""}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>

      {/* Supplier Field */}
      <div className="flex-1 min-w-[200px]">
        <FormField
          control={control}
          name={`items.${index}.supplierId`}
          render={({ field }) => (
            <FormItem className="flex flex-col">
              <FormControl>
                <SupplierSelect
                  value={field.value || null}
                  onChange={(id, name) => {
                    field.onChange(id);
                    setValue(`items.${index}.supplierName`, name, { shouldDirty: true });
                  }}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>

      {/* Remove Button */}
      <div className="pt-0.5 shrink-0">
        <Button
          type="button"
          variant="ghost"
          size="icon"
          onClick={onRemove}
          className="text-destructive hover:text-destructive hover:bg-destructive/10"
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
