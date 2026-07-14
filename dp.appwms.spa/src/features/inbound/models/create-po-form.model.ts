import { z } from "zod";
import {t } from "i18next";

export const CreatePOItemSchema = z.object({
  skuId: z.string().min(1, "SKU is required"),
  skuCode: z.string().optional(),
  skuName: z.string().optional(),
  quantity: z
    .number({ message: "Quantity is required" })
    .int("Quantity must be an integer")
    .positive("Quantity must be greater than 0"),
  supplierId: z.string().nullable().optional(),
  supplierName: z.string().nullable().optional(),
});

export const CreatePOFormSchema = z.object({
  orderNumber: z.string().optional(),
  expectedDate: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  items: z
    .array(CreatePOItemSchema)
    .min(1, t("inbound.po.errors.atLeastOneItem"))
    .superRefine((items, ctx) => {
      const seen = new Set<string>();
      items.forEach((item, index) => {
        if (!item.skuId) return; // Skip if skuId is missing
        
        // Use a composite key of skuId and supplierId
        const key = `${item.skuId}_${item.supplierId || "null"}`;
        
        if (seen.has(key)) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: t("inbound.po.errors.duplicateItem"),
            path: [index, "skuId"],
          });
        }
        seen.add(key);
      });
    }),
});

export type CreatePOItemFormValues = z.infer<typeof CreatePOItemSchema>;
export type CreatePOFormValues = z.infer<typeof CreatePOFormSchema>;
