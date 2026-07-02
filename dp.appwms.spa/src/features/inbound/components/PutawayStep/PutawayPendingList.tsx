import React from "react";
import { useTranslation } from "react-i18next";
import { Layers, Info } from "lucide-react";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Button } from "@/components/ui/button";
import type { PendingPutawayTask } from "../../models/inbound.model";
import type { SkuDto } from "@/features/master-data/skus/models/sku-dto.model";
import type { LocationOccupancy } from "@/components/MapLocation";

interface PutawayPendingListProps {
  pendingTasks: PendingPutawayTask[];
  selectedSku: SkuDto | null;
  onSelectPendingTask: (task: PendingPutawayTask) => void;
  selectedLocation: LocationOccupancy | null;
  onAddToDraft: () => void;
}

export const PutawayPendingList: React.FC<PutawayPendingListProps> = ({
  pendingTasks,
  selectedSku,
  onSelectPendingTask,
  selectedLocation,
  onAddToDraft,
}) => {
  const { t } = useTranslation();

  if (pendingTasks.length === 0) return null;

  const selectedTask = pendingTasks.find(task => task.sku.id === selectedSku?.id);

  return (
    <div className="bg-card text-card-foreground rounded-xl border border-border p-4 shadow-sm shrink-0 flex flex-col max-h-[350px] overflow-hidden">
      <div className="flex items-center gap-2 mb-3 border-b border-border pb-2">
        <Layers className="size-5 text-primary" aria-hidden="true" />
        <h2 className="text-sm font-bold uppercase tracking-wider text-foreground">
          {t("inbound.tabs.putaway")}
        </h2>
      </div>

      <ScrollArea className="flex-1 pr-1 overflow-y-auto">
        <div className="space-y-2 pb-1">
          {pendingTasks.map((task) => (
            <button
              key={task.id}
              type="button"
              onClick={() => onSelectPendingTask(task)}
              className={`w-full text-left flex items-center justify-between p-2.5 rounded-lg border border-border hover:bg-muted/50 cursor-pointer transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary ${selectedSku?.id === task.sku.id ? "bg-primary/5 border-primary" : "bg-background"
                }`}
            >
              <div className="space-y-1">
                <span className="font-bold text-xs text-foreground block">{task.sku.skuCode}</span>
                <span className="text-[10px] text-muted-foreground block truncate max-w-[200px]">
                  {task.sku.name}
                </span>
                {task.lotNumber && (
                  <span className="inline-block text-[9px] bg-muted px-1.5 py-0.5 rounded text-muted-foreground font-semibold">
                    {t("inbound.form.lotNumber")}: {task.lotNumber}
                  </span>
                )}
              </div>
              <div className="text-right">
                <span className="text-xs font-bold text-primary block">
                  {t("inbound.form.quantity")}: {task.quantity}
                </span>
                {task.supplier && (
                  <span className="text-[9px] text-muted-foreground block max-w-[100px] truncate">
                    {task.supplier.name}
                  </span>
                )}
              </div>
            </button>
          ))}
        </div>
      </ScrollArea>

      {/* Selected Task Action Footer */}
      {selectedTask && (
        <div className="mt-3 pt-3 border-t border-border flex flex-col gap-2 bg-muted/20 p-2.5 rounded-lg">
          <div className="flex items-center justify-between text-xs">
            <div className="flex items-center gap-1.5">
              <span className="text-muted-foreground">{t("inbound.form.targetLocation")}:</span>
              {selectedLocation ? (
                <span className="font-bold text-primary bg-primary/10 px-2 py-0.5 rounded">
                  {selectedLocation.name}
                </span>
              ) : (
                <span className="text-muted-foreground italic flex items-center gap-1">
                  <Info className="size-3.5 text-muted-foreground/70" aria-hidden="true" /> {t("inbound.form.selectLocationOnMap")}
                </span>
              )}
            </div>
            <Button
              type="button"
              onClick={onAddToDraft}
              disabled={!selectedLocation}
              size="sm"
              className="h-8 text-xs bg-primary text-primary-foreground hover:bg-primary/95 flex items-center gap-1 cursor-pointer font-bold px-3 transition-colors"
            >
              {t("inbound.form.addToDraft")}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};
