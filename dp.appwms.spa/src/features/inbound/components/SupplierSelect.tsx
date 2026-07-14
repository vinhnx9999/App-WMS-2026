import * as React from "react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { supplierService } from "@/features/master-data/suppliers/services/supplier.service";

interface SupplierSelectProps {
  value: string | null;
  onChange: (id: string | null, name: string | null) => void;
  disabled?: boolean;
}

interface SupplierOption {
  id: string;
  code: string;
  name: string;
}

export function SupplierSelect({
  value,
  onChange,
  disabled = false,
}: SupplierSelectProps) {
  const [suppliers, setSuppliers] = React.useState<SupplierOption[]>([]);
  const [isLoading, setIsLoading] = React.useState(true);

  React.useEffect(() => {
    let isMounted = true;
    const fetchSuppliers = async () => {
      try {
        setIsLoading(true);
        const response = await supplierService.supplierLookup();
        if (isMounted && response.data) {
          setSuppliers(response.data);
        }
      } catch (error) {
        console.error("Failed to load suppliers", error);
      } finally {
        if (isMounted) setIsLoading(false);
      }
    };
    fetchSuppliers();
    return () => {
      isMounted = false;
    };
  }, []);

  if (isLoading) {
    return <Skeleton className="h-10 w-full" />;
  }

  // Map null value to "no-supplier" for the Select component
  const selectValue = value === null ? "no-supplier" : value;

  const handleValueChange = (newValue: string) => {
    if (newValue === "no-supplier") {
      onChange(null, null);
    } else {
      const selected = suppliers.find((s) => s.id === newValue);
      if (selected) {
        onChange(selected.id, selected.name);
      }
    }
  };

  return (
    <Select
      value={selectValue}
      onValueChange={handleValueChange}
      disabled={disabled}
    >
      <SelectTrigger className="w-full">
        <SelectValue placeholder="Select Supplier" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="no-supplier">No Supplier</SelectItem>
        {suppliers.map((supplier) => (
          <SelectItem key={supplier.id} value={supplier.id}>
            {supplier.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
