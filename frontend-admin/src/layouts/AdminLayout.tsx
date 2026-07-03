import React from 'react';
import { Outlet } from 'react-router-dom';
import { useKeycloak } from '@react-keycloak/web';
import { LogOut, Activity } from 'lucide-react';

const AdminLayout: React.FC = () => {
  const { keycloak } = useKeycloak();

  const handleLogout = () => {
    keycloak.logout();
  };

  return (
    <div className="flex h-screen bg-background text-gray-900">
      {/* Sidebar */}
      <aside className="w-64 bg-surface border-r border-gray-200 flex flex-col">
        <div className="p-6">
          <h1 className="text-2xl font-bold text-primary flex items-center gap-2">
            <Activity className="w-6 h-6" />
            Admin
          </h1>
        </div>
        
        <nav className="flex-1 px-4 py-4">
          <ul className="space-y-2">
            <li>
              <a href="#" className="flex items-center gap-3 px-3 py-2 text-primary bg-emerald-50 rounded-lg font-medium">
                Konfiguracja LLM
              </a>
            </li>
          </ul>
        </nav>
        
        <div className="p-4 border-t border-gray-200">
          <div className="flex items-center justify-between">
            <div className="text-sm font-medium truncate pr-2">
              {keycloak.tokenParsed?.preferred_username || 'Admin User'}
            </div>
            <button 
              onClick={handleLogout}
              className="p-2 text-gray-500 hover:text-warning hover:bg-red-50 rounded-lg transition-colors"
              title="Wyloguj"
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Topbar */}
        <header className="bg-surface border-b border-gray-200 h-16 flex items-center px-8 justify-between">
          <h2 className="text-xl font-semibold">Konfiguracja LLM</h2>
          <div className="flex items-center gap-2">
            <span className="flex h-3 w-3 relative">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-3 w-3 bg-emerald-500"></span>
            </span>
            <span className="text-sm font-medium text-gray-600">Connected</span>
          </div>
        </header>

        {/* Content Area */}
        <main className="flex-1 overflow-y-auto p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AdminLayout;
