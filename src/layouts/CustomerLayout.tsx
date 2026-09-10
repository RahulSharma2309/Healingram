import { Outlet } from "react-router-dom";
import { DemoBanner } from "../components/DemoBanner";
import { CustomerHeader } from "../components/CustomerHeader";
import { CustomerFooter } from "../components/CustomerFooter";

export function CustomerLayout() {
  return (
    <div className="min-h-screen flex flex-col">
      <DemoBanner />
      <CustomerHeader />
      <main className="flex-1">
        <Outlet />
      </main>
      <CustomerFooter />
    </div>
  );
}

