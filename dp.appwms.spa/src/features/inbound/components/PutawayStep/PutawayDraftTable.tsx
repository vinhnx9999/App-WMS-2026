import React from "react";
import { useTranslation } from "react-i18next";
import { ClipboardList, Trash2, RefreshCw, CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { DraftItem } from "../../models/inbound.model";

interface PutawayDraftTableProps {
  draftItems: DraftItem[];
  onRemoveItem: (id: string) => void;
  onClearDraft: () => void;
  onConfirmPutaway: () => void;
  isSubmitting: boolean;
}

export const PutawayDraftTable: React.FC<PutawayDraftTableProps> = ({
  draftItems,
  onRemoveItem,
  onClearDraft,
  onConfirmPutaway,
  isSubmitting,
}) => {
  const { t } = useTranslation();

  return (
    <div className="bg-card text-card-foreground rounded-xl border border-border p-4 shadow-sm flex-1 min-h-[160px] flex flex-col overflow-hidden">
      <div className="flex items-center justify-between border-b border-border pb-2 mb-2">
        <div className="flex items-center gap-2">
          <ClipboardList className="size-5 text-primary" aria-hidden="true" />
          <h2 className="text-sm font-bold uppercase tracking-wider text-foreground">
            {t("inbound.draft.title")} ({draftItems.length})
          </h2>
        </div>
        {draftItems.length > 0 && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onClearDraft}
            className="h-7 text-xs text-destructive hover:bg-destructive/10 cursor-pointer transition-colors"
          >
            <Trash2 className="size-3.5 mr-1" aria-hidden="true" /> {t("inbound.draft.clearAll")}
          </Button>
        )}
      </div>

      {/* Table */}
      <div className="flex-1 overflow-auto min-h-[90px]">
        {draftItems.length === 0 ? (
          <div className="h-full w-full flex flex-col items-center justify-center text-muted-foreground gap-1.5 p-4 text-center">
            <ClipboardList className="size-8 text-muted-foreground/30" aria-hidden="true" />
            <p className="text-xs">{t("inbound.draft.empty")}</p>
          </div>
        ) : (
          <table className="w-full text-left text-xs border-collapse">
            <thead className="bg-muted text-muted-foreground sticky top-0 font-bold z-10">
              <tr>
                <th className="p-2 border-b border-border">{t("inbound.draft.headers.sku")}</th>
                <th className="p-2 border-b border-border">{t("inbound.draft.headers.qty")}</th>
                <th className="p-2 border-b border-border">{t("inbound.draft.headers.location")}</th>
                <th className="p-2 border-b border-border">{t("inbound.draft.headers.details")}</th>
                <th className="p-2 border-b border-border text-center">{t("inbound.draft.headers.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {draftItems.map((item) => (
                <tr key={item.id} className="hover:bg-muted/30 transition-colors border-b border-border">
                  <td className="p-2 font-medium">
                    <div>{item.sku.skuCode}</div>
                    <div className="text-[10px] text-muted-foreground truncate max-w-[140px]">{item.sku.name}</div>
                  </td>
                  <td className="p-2 font-bold text-foreground">{item.quantity}</td>
                  <td className="p-2">
                    <span className="font-semibold text-primary">{item.location.name}</span>
                  </td>
                  <td className="p-2 text-[10px] text-muted-foreground">
                    {item.lotNumber && <div>{t("inbound.form.lotNumber")}: {item.lotNumber}</div>}
                    {item.palletCode && <div>Pallet: {item.palletCode}</div>}
                    {!item.lotNumber && !item.palletCode && <span>-</span>}
                  </td>
                  <td className="p-2 text-center">
                    <button
                      onClick={() => onRemoveItem(item.id)}
                      aria-label={t("inbound.draft.remove", "Remove item")}
                      className="p-1 hover:bg-destructive/15 text-destructive rounded transition-colors cursor-pointer"
                    >
                      <Trash2 className="size-3.5" aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Submit Actions */}
      <div className="mt-4 pt-3 border-t border-border flex items-center justify-between bg-muted/20 p-2 rounded-lg">
        <div className="text-xs text-muted-foreground flex items-center gap-1">
          <kbd className="px-1.5 py-0.5 bg-muted border border-border rounded text-[10px] font-mono shadow-sm">Ctrl + Enter</kbd>
          <span>{t("inbound.form.submitHint")}</span>
        </div>
        <Button
          onClick={onConfirmPutaway}
          disabled={draftItems.length === 0 || isSubmitting}
          className="bg-primary text-primary-foreground hover:bg-primary/90 shadow-md font-bold text-xs px-4 h-9 cursor-pointer transition-[background-color,transform,box-shadow] flex items-center gap-1.5"
        >
          {isSubmitting ? (
            <RefreshCw className="size-3.5 animate-spin" aria-hidden="true" />
          ) : (
            <CheckCircle2 className="size-4" aria-hidden="true" />
          )}
          {t("inbound.form.submit")}
        </Button>
      </div>
    </div>
  );
};
