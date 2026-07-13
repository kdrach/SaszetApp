import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { useAuth } from './components/AuthProvider';
import AdminLayout from './layouts/AdminLayout';
import AdminDashboardView from './pages/AdminDashboardView';
import ProtectedRoute from './components/ProtectedRoute';

import RateLimitsView from './pages/RateLimitsView';

function App() {
  const { initialized } = useAuth();

  if (!initialized) {
    return <div className="flex items-center justify-center min-h-screen bg-background text-gray-900">Loading authentication...</div>;
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<ProtectedRoute><AdminLayout /></ProtectedRoute>}>
          <Route index element={<AdminDashboardView />} />
          <Route path="rate-limits" element={<RateLimitsView />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
