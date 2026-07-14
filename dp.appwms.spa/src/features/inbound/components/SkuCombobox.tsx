import * as React from "react";
import { Check, ChevronsUpDown } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Skeleton } from "@/components/ui/skeleton";
import { skuService } from "@/features/master-data/skus/services/sku.service";

interface SkuComboboxProps {
  value: string;
  onChange: (id: string, code: string, name: string) => void;
  disabled?: boolean;
  placeholder?: string;
}

interface SkuOption {
  id: string;
  code: string;
  name: string;
}

export function SkuCombobox({
  value,
  onChange,
  disabled = false,
  placeholder = "Select SKU...",
}: SkuComboboxProps) {
  const [open, setOpen] = React.useState(false);
  const [skus, setSkus] = React.useState<SkuOption[]>([]);
  const [isLoading, setIsLoading] = React.useState(true);

  React.useEffect(() => {
    let isMounted = true;
    const fetchSkus = async () => {
      try {
        setIsLoading(true);
        const response = await skuService.skuLookup();
        if (isMounted && response.data) {
          setSkus(response.data);
        }
      } catch (error) {
        console.error("Failed to load SKUs", error);
      } finally {
        if (isMounted) setIsLoading(false);
      }
    };
    fetchSkus();
    return () => {
      isMounted = false;
    };
  }, []);

  const selectedSku = React.useMemo(
    () => skus.find((sku) => sku.id === value),
    [value, skus]
  );

  if (isLoading) {
    return <Skeleton className="h-10 w-full" />;
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          disabled={disabled}
          className="w-full justify-between font-normal"
        >
          {selectedSku ? selectedSku.code : placeholder}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[300px] p-0" align="start">
        <Command
          filter={(value, search) => {
            if (value.toLowerCase().includes(search.toLowerCase())) return 1;
            return 0;
          }}
        >
          <CommandInput placeholder="Search SKU..." />
          <CommandList>
            <CommandEmpty>No SKU found.</CommandEmpty>
            <CommandGroup>
              {skus.map((sku) => (
                <CommandItem
                  key={sku.id}
                  // We provide a composite string as the value so the internal filter can match both code and name
                  value={`${sku.code} ${sku.name}`}
                  onSelect={() => {
                    onChange(sku.id, sku.code, sku.name);
                    setOpen(false);
                  }}
                >
                  <Check
                    className={cn(
                      "mr-2 h-4 w-4",
                      value === sku.id ? "opacity-100" : "opacity-0"
                    )}
                  />
                  <span>
                    <strong>{sku.code}</strong> — {sku.name}
                  </span>
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
