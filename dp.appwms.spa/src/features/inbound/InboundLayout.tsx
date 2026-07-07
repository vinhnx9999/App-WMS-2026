import { Outlet } from "react-router-dom";
import { InboundSidebar } from "./components/InboundSidebar";

export default function InboundLayout() {
  return (
    <div className="h-full w-full flex overflow-hidden bg-background">
      <InboundSidebar />
      <div className="flex-1 min-w-0 h-full p-4 overflow-y-auto">
        <Outlet />
      </div>
    </div>
  );
}
