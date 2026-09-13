import { BrowserRouter, Routes, Route, Navigate, useLocation } from "react-router-dom";
import { CustomerLayout } from "./layouts/CustomerLayout";
import { VendorLayout } from "./layouts/VendorLayout";
import { AdminLayout } from "./layouts/AdminLayout";
import { Home } from "./pages/customer/Home";
import { RetreatList } from "./pages/customer/RetreatList";
import { SearchResults } from "./pages/customer/SearchResults";
import { RetreatDetail } from "./pages/customer/RetreatDetail";
import { Questionnaire } from "./pages/customer/Questionnaire";
import { Checkout } from "./pages/customer/Checkout";
import { PaymentSuccess } from "./pages/customer/PaymentSuccess";
import { BookingConfirmation } from "./pages/customer/BookingConfirmation";
import { Login } from "./pages/customer/Login";
import { Signup } from "./pages/customer/Signup";
import { UserDashboard } from "./pages/customer/UserDashboard";
import { StaticPage } from "./pages/customer/StaticPage";
import { TalkToExpert } from "./pages/customer/TalkToExpert";
import { Therapies } from "./pages/customer/Therapies";
import { Destinations } from "./pages/customer/Destinations";
import { Blog } from "./pages/customer/Blog";
import { VendorDashboard } from "./pages/vendor/VendorDashboard";
import { VendorLogin } from "./pages/vendor/VendorLogin";
import { AdminDashboard } from "./pages/admin/AdminDashboard";
import { AdminLogin } from "./pages/admin/AdminLogin";
import { AdminPortalGuard, CustomerPortalGuard, VendorPortalGuard } from "./components/auth/PortalGuards";
import { resolvePortal, shouldRedirectStaffPath } from "./lib/runtimeConfig";
import { AvailabilityRequestReceived } from "./pages/customer/AvailabilityRequestReceived";
import { MyRequest } from "./pages/customer/MyRequest";
import { MyAvailabilityRequest } from "./pages/customer/MyAvailabilityRequest";
import { PaymentReady } from "./pages/customer/PaymentReady";

function HostRedirect() {
  const location = useLocation();
  const portal = resolvePortal(window.location.hostname, location.pathname);
  const target = shouldRedirectStaffPath(portal, location.pathname);
  if (target) {
    window.location.replace(target);
  }
  return null;
}

function VendorRoutes() {
  return (
    <Routes>
      <Route path="login" element={<VendorLogin />} />
      <Route path="vendor/login" element={<VendorLogin />} />
      <Route
        path="/"
        element={
          <VendorPortalGuard>
            <VendorLayout />
          </VendorPortalGuard>
        }
      >
        <Route index element={<VendorDashboard />} />
      </Route>
      <Route
        path="vendor"
        element={
          <VendorPortalGuard>
            <VendorLayout />
          </VendorPortalGuard>
        }
      >
        <Route index element={<VendorDashboard />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

function AdminRoutes() {
  return (
    <Routes>
      <Route path="login" element={<AdminLogin />} />
      <Route path="admin/login" element={<AdminLogin />} />
      <Route
        path="/"
        element={
          <AdminPortalGuard>
            <AdminLayout />
          </AdminPortalGuard>
        }
      >
        <Route index element={<AdminDashboard />} />
      </Route>
      <Route
        path="admin"
        element={
          <AdminPortalGuard>
            <AdminLayout />
          </AdminPortalGuard>
        }
      >
        <Route index element={<AdminDashboard />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

function PortalApp() {
  const location = useLocation();
  const portal = resolvePortal(window.location.hostname, location.pathname);
  return (
    <>
      <HostRedirect />
      {portal === "vendor" ? (
        <VendorRoutes />
      ) : portal === "admin" ? (
        <AdminRoutes />
      ) : (
      <Routes>
        <Route element={<CustomerPortalGuard><CustomerLayout /></CustomerPortalGuard>}>
          <Route index element={<Home />} />
          <Route path="retreats" element={<RetreatList />} />
          <Route path="retreats/:id" element={<RetreatDetail />} />
          <Route path="search" element={<SearchResults />} />
          <Route path="therapies" element={<Therapies />} />
          <Route path="destinations" element={<Destinations />} />
          <Route path="questionnaire" element={<Questionnaire />} />
          <Route path="blog" element={<Blog />} />
          <Route path="about" element={<StaticPage title="About Us" />} />
          <Route path="contact" element={<TalkToExpert />} />
          <Route path="terms" element={<StaticPage title="Terms" />} />
          <Route path="privacy" element={<StaticPage title="Privacy" />} />
          <Route path="faq" element={<StaticPage title="FAQs" faq />} />
          <Route path="login" element={<Login />} />
          <Route path="signup" element={<Signup />} />
          <Route path="dashboard" element={<UserDashboard />} />
          <Route path="my-request" element={<MyRequest />} />
          <Route path="requests/:requestId/received" element={<AvailabilityRequestReceived />} />
          <Route path="requests/:requestId/verify" element={<MyRequest />} />
          <Route path="requests/:requestId/payment" element={<PaymentReady />} />
          <Route path="requests/:requestId" element={<MyAvailabilityRequest />} />
          <Route path="checkout/:id" element={<Checkout />} />
          <Route path="payment-success" element={<PaymentSuccess />} />
          <Route path="booking-confirmation" element={<BookingConfirmation />} />
        </Route>
        <Route path="vendor/login" element={<VendorLogin />} />
        <Route
          path="vendor"
          element={
            <VendorPortalGuard>
              <VendorLayout />
            </VendorPortalGuard>
          }
        >
          <Route index element={<VendorDashboard />} />
        </Route>
        <Route path="admin/login" element={<AdminLogin />} />
        <Route
          path="admin"
          element={
            <AdminPortalGuard>
              <AdminLayout />
            </AdminPortalGuard>
          }
        >
          <Route index element={<AdminDashboard />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
      )}
    </>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <PortalApp />
    </BrowserRouter>
  );
}
