import { useState, useEffect, useMemo } from "react";
import { themeQuartz, colorSchemeDark } from "ag-grid-community";

export function useAgGridTheme() {
  const [isDark, setIsDark] = useState(() =>
    document.documentElement.classList.contains("dark")
  );

  useEffect(() => {
    const observer = new MutationObserver(() => {
      setIsDark(document.documentElement.classList.contains("dark"));
    });
    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ["class"],
    });
    return () => observer.disconnect();
  }, []);

  const gridTheme = useMemo(() => {
    const baseTheme = isDark ? themeQuartz.withPart(colorSchemeDark) : themeQuartz;
    return baseTheme.withParams({
      backgroundColor: "var(--background)",
      headerBackgroundColor: "var(--muted)",
      borderColor: "var(--border)",
      rowHoverColor: "var(--accent)",
      textColor: "var(--foreground)",
    });
  }, [isDark]);

  return gridTheme;
}
