import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { useKeycloak } from '@react-keycloak/web';
import AdminLayout from './layouts/AdminLayout';
import AdminDashboardView from './pages/AdminDashboardView';
import ProtectedRoute from './components/ProtectedRoute';

function App() {
  const { initialized } = useKeycloak();

  if (!initialized) {
    return <div className="flex items-center justify-center min-h-screen bg-background text-gray-900">Initializing Keycloak...</div>;
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<ProtectedRoute><AdminLayout /></ProtectedRoute>}>
          <Route index element={<AdminDashboardView />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
