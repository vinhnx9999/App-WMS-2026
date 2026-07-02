import React, { useState, useEffect, useRef } from "react";
import { useTranslation } from "react-i18next";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Loader2 } from "lucide-react";

export interface LookupItem {
  id: string;
  code: string;
  name: string;
}

interface LookupSearchInputProps {
  label: string;
  placeholder?: string;
  required?: boolean;
  fetchData: () => Promise<LookupItem[]>;
  value: LookupItem | null;
  onChange: (item: LookupItem | null) => void;
  inputRef?: React.RefObject<HTMLInputElement | null>;
  onKeyDown?: (e: React.KeyboardEvent<HTMLInputElement>) => void;
  className?: string;
}

export const LookupSearchInput: React.FC<LookupSearchInputProps> = ({
  label,
  placeholder = "",
  required = false,
  fetchData,
  value,
  onChange,
  inputRef,
  onKeyDown,
  className = "",
}) => {
  const { t } = useTranslation();
  const [items, setItems] = useState<LookupItem[]>([]);
  const [search, setSearch] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [hasLoaded, setHasLoaded] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);

  const containerRef = useRef<HTMLDivElement>(null);
  const localInputRef = useRef<HTMLInputElement>(null);
  const resolvedInputRef = inputRef || localInputRef;

  // Sync search text with selected value
  useEffect(() => {
    if (value) {
      setSearch(`${value.name} (${value.code})`);
    } else {
      setSearch("");
    }
  }, [value]);

  // Load data once when component is focused or clicked, if not already loaded
  const loadDataIfNeeded = async () => {
    if (hasLoaded || isLoading) return;
    setIsLoading(true);
    try {
      const data = await fetchData();
      setItems(data);
      setHasLoaded(true);
    } catch (error) {
      console.error("Error loading lookup data:", error);
    } finally {
      setIsLoading(false);
    }
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Filter items in-memory based on search text (only if not showing selected item's full text)
  const filteredItems = React.useMemo(() => {
    const query = search.toLowerCase().trim();
    if (!query || (value && search === `${value.name} (${value.code})`)) {
      return items;
    }
    return items.filter(
      (item) =>
        item.code.toLowerCase().includes(query) ||
        item.name.toLowerCase().includes(query)
    );
  }, [items, search, value]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newVal = e.target.value;
    setSearch(newVal);
    if (value) {
      onChange(null); // Clear selected item if user starts typing
    }
    setIsOpen(true);
    setActiveIndex(-1);
    loadDataIfNeeded();
  };

  const handleSelectItem = (item: LookupItem) => {
    onChange(item);
    setSearch(`${item.name} (${item.code})`);
    setIsOpen(false);
    setActiveIndex(-1);
  };

  const handleKeyDownInternal = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (onKeyDown) {
      onKeyDown(e);
    }

    if (e.defaultPrevented) return;

    if (e.key === "ArrowDown") {
      e.preventDefault();
      if (!isOpen) {
        setIsOpen(true);
        loadDataIfNeeded();
      } else if (filteredItems.length > 0) {
        setActiveIndex((prev) => Math.min(prev + 1, filteredItems.length - 1));
      }
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      if (isOpen && filteredItems.length > 0) {
        setActiveIndex((prev) => Math.max(prev - 1, 0));
      }
    } else if (e.key === "Enter") {
      if (isOpen && activeIndex >= 0 && activeIndex < filteredItems.length) {
        e.preventDefault();
        handleSelectItem(filteredItems[activeIndex]);
      }
    } else if (e.key === "Escape") {
      setIsOpen(false);
      setActiveIndex(-1);
    }
  };

  return (
    <div className={`relative ${className}`} ref={containerRef}>
      <label className="text-xs font-semibold text-muted-foreground block mb-1">
        {label} {required && <span className="text-destructive">*</span>}
      </label>
      <div className="relative">
        <Input
          ref={resolvedInputRef}
          value={search}
          onChange={handleInputChange}
          onFocus={() => {
            setIsOpen(true);
            loadDataIfNeeded();
          }}
          onKeyDown={handleKeyDownInternal}
          placeholder={placeholder}
          className="h-9 pr-8 text-xs bg-background border-border focus-visible:ring-primary w-full"
        />
        {isLoading && (
          <div className="absolute right-2.5 top-2.5">
            <Loader2 className="size-4 animate-spin text-muted-foreground" />
          </div>
        )}
      </div>

      {isOpen && (filteredItems.length > 0 || isLoading) && (
        <div className="absolute top-full left-0 w-full z-50 mt-1 border border-border bg-popover text-popover-foreground rounded-md shadow-lg overflow-hidden">
          <ScrollArea className="max-h-48">
            <div className="p-1">
              {isLoading ? (
                <div className="p-2 text-xs text-muted-foreground text-center">{t("common.loading", "Đang tải")}</div>
              ) : filteredItems.length === 0 ? (
                <div className="p-2 text-xs text-muted-foreground text-center">{t("common.noData", "Không tìm thấy kết quả")}</div>
              ) : (
                filteredItems.map((item, index) => (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => handleSelectItem(item)}
                    className={`w-full text-left px-3 py-2 text-xs rounded-sm hover:bg-accent hover:text-accent-foreground text-foreground transition-colors flex justify-between items-center ${index === activeIndex ? "bg-accent text-accent-foreground" : ""
                      }`}
                  >
                    <span className="font-semibold">{item.code}</span>
                    <span className="text-[10px] text-muted-foreground truncate max-w-[200px]">{item.name}</span>
                  </button>
                ))
              )}
            </div>
          </ScrollArea>
        </div>
      )}
    </div>
  );
};
