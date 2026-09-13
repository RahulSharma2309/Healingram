import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
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
import { AdminDashboard } from "./pages/admin/AdminDashboard";
import { AvailabilityRequestReceived } from "./pages/customer/AvailabilityRequestReceived";
import { MyAvailabilityRequest } from "./pages/customer/MyAvailabilityRequest";
import { PaymentReady } from "./pages/customer/PaymentReady";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<CustomerLayout />}>
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
          <Route path="requests/:requestId/received" element={<AvailabilityRequestReceived />} />
          <Route path="requests/:requestId/payment" element={<PaymentReady />} />
          <Route path="requests/:requestId" element={<MyAvailabilityRequest />} />
          <Route path="checkout/:id" element={<Checkout />} />
          <Route path="payment-success" element={<PaymentSuccess />} />
          <Route path="booking-confirmation" element={<BookingConfirmation />} />
        </Route>
        <Route path="vendor" element={<VendorLayout />}>
          <Route index element={<VendorDashboard />} />
        </Route>
        <Route path="admin" element={<AdminLayout />}>
          <Route index element={<AdminDashboard />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
