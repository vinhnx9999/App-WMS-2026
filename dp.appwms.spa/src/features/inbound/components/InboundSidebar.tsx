import { Link, useLocation } from "react-router-dom";
import { INBOUND_STEPS } from "../models/inbound.model";
import { useInboundWorkflow } from "@/hooks/use-inbound-workflow";
import { cn } from "@/lib/utils";
import { LayoutDashboard, FileText, Download, CheckCircle, Package } from "lucide-react";

export function InboundSidebar() {
  const { enabledSteps, isLoading } = useInboundWorkflow();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="w-64 border-r bg-muted/20 h-full flex flex-col gap-2 p-4 animate-pulse">
        <div className="h-10 bg-muted rounded-md w-full"></div>
        <div className="h-10 bg-muted rounded-md w-full"></div>
        <div className="h-10 bg-muted rounded-md w-full"></div>
      </div>
    );
  }

  const navItems = [
    {
      to: "/inbound",
      label: "Dashboard",
      icon: LayoutDashboard,
      exact: true
    }
  ];

  if (enabledSteps.includes(INBOUND_STEPS.PO)) {
    navItems.push({
      to: "/inbound/po",
      label: "Purchase Orders",
      icon: FileText,
      exact: false
    });
  }

  if (enabledSteps.includes(INBOUND_STEPS.RECEIVE)) {
    navItems.push({
      to: "/inbound/receive",
      label: "Receive",
      icon: Download,
      exact: false
    });
  }

  if (enabledSteps.includes(INBOUND_STEPS.QC)) {
    navItems.push({
      to: "/inbound/qc",
      label: "Quality Control",
      icon: CheckCircle,
      exact: false
    });
  }

  if (enabledSteps.includes(INBOUND_STEPS.PUTAWAY)) {
    navItems.push({
      to: "/inbound/putaway",
      label: "Putaway",
      icon: Package,
      exact: false
    });
  }

  return (
    <div className="w-64 border-r bg-muted/10 h-full flex flex-col p-4 gap-2">
      <div className="mb-4 px-2">
        <h2 className="text-lg font-semibold tracking-tight">Inbound Module</h2>
      </div>
      <nav className="flex flex-col gap-1">
        {navItems.map((item) => {
          const isActive = item.exact 
            ? location.pathname === item.to 
            : location.pathname.startsWith(item.to) && item.to !== '/inbound';
            
          return (
            <Link
              key={item.to}
              to={item.to}
              className={cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground",
                isActive ? "bg-primary text-primary-foreground hover:bg-primary/90 hover:text-primary-foreground" : "text-muted-foreground"
              )}
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
