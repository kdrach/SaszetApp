import React, { useState, useEffect, useMemo } from 'react';
import { Settings, Users, Edit3, X, Save, ShieldAlert, Activity, CheckCircle2 } from 'lucide-react';
import * as adminSettingsApi from '../api/adminSettingsApi';
import { useAuth } from '../components/AuthProvider';

const RateLimitsView: React.FC = () => {
  const { keycloak } = useAuth();
  
  // Global Settings State
  const [globalSettings, setGlobalSettings] = useState<adminSettingsApi.GlobalSettings>({ globalScanLimit: 5, scanLimitRollingDays: 7 });
  const [isSavingGlobal, setIsSavingGlobal] = useState(false);
  
  // Users List State
  const [users, setUsers] = useState<adminSettingsApi.UserLimitDto[]>([]);
  const [isLoadingUsers, setIsLoadingUsers] = useState(true);
  
  // Modal State
  const [editingUser, setEditingUser] = useState<adminSettingsApi.UserLimitDto | null>(null);
  const [newLimitInput, setNewLimitInput] = useState<number>(0);
  const [isSavingLimit, setIsSavingLimit] = useState(false);

  useEffect(() => {
    const fetchGlobal = async () => {
      try {
        const token = keycloak.token || localStorage.getItem('access_token') || '';
        if (token) {
          const settings = await adminSettingsApi.getGlobalSettings(token);
          setGlobalSettings(settings);
        }
      } catch (err) {
        console.error('Failed to load global settings', err);
      }
    };
    
    const fetchUsers = async () => {
      setIsLoadingUsers(true);
      try {
        const token = keycloak.token || localStorage.getItem('access_token') || '';
        const data = await adminSettingsApi.getAllUsersLimits(token);
        setUsers(data);
      } catch (err) {
        console.error('Failed to load users limits', err);
      } finally {
        setIsLoadingUsers(false);
      }
    };

    fetchGlobal();
    fetchUsers();
  }, [keycloak.token]);

  const handleSaveGlobalSettings = async () => {
    setIsSavingGlobal(true);
    try {
      const token = keycloak.token || localStorage.getItem('access_token') || '';
      await adminSettingsApi.updateGlobalSettings(token, globalSettings);
      alert('Zapisano ustawienia globalne!');
    } catch (err: any) {
      alert('Błąd zapisu ustawień: ' + err.message);
    } finally {
      setIsSavingGlobal(false);
    }
  };

  const handleOpenEditModal = (user: adminSettingsApi.UserLimitDto) => {
    setEditingUser(user);
    setNewLimitInput(user.maxScans);
  };

  const handleCloseEditModal = () => {
    setEditingUser(null);
  };

  const handleSaveUserLimit = async () => {
    if (!editingUser) return;
    setIsSavingLimit(true);
    try {
      const token = keycloak.token || localStorage.getItem('access_token') || '';
      await adminSettingsApi.updateUserLimit(token, editingUser.userId, newLimitInput);
      
      // Update local state to reflect change without refetching immediately
      setUsers(users.map(u => u.userId === editingUser.userId ? { ...u, maxScans: newLimitInput } : u));
      
      handleCloseEditModal();
    } catch (err: any) {
      alert('Błąd zapisu limitu: ' + err.message);
    } finally {
      setIsSavingLimit(false);
    }
  };

  return (
    <div className="max-w-7xl mx-auto space-y-10 animate-in fade-in slide-in-from-bottom-4 duration-700 ease-out">
      
      {/* Header Section */}
      <div className="flex items-center gap-4 border-b border-gray-200 pb-6">
        <div className="p-3 bg-emerald-100 rounded-2xl">
          <Activity className="w-8 h-8 text-emerald-600" />
        </div>
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight text-gray-900">Limity Skanowań</h1>
          <p className="text-gray-500 mt-1">Zarządzaj globalnymi limitami zapytań do modeli LLM oraz indywidualnymi przydziałami dla użytkowników.</p>
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-8">
        
        {/* Global Settings Panel */}
        <div className="xl:col-span-1 space-y-6">
          <div className="bg-white rounded-3xl p-8 shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-gray-100 relative overflow-hidden group">
            <div className="absolute top-0 right-0 w-32 h-32 bg-emerald-50 rounded-bl-full -z-10 transition-transform duration-500 group-hover:scale-110"></div>
            
            <div className="flex items-center gap-3 mb-8">
              <Settings className="w-6 h-6 text-emerald-500" />
              <h2 className="text-xl font-bold text-gray-800">Globalne Ustawienia</h2>
            </div>
            
            <div className="space-y-6">
              <div className="group/input">
                <label className="block text-sm font-semibold text-gray-700 mb-2 transition-colors group-focus-within/input:text-emerald-600">Domyślny limit skanowań</label>
                <div className="relative">
                  <input 
                    type="number" 
                    value={globalSettings.globalScanLimit} 
                    onChange={e => setGlobalSettings({...globalSettings, globalScanLimit: parseInt(e.target.value) || 0})} 
                    className="w-full pl-4 pr-12 py-3 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-all font-medium text-gray-900 outline-none"
                  />
                  <div className="absolute inset-y-0 right-0 flex items-center pr-4 text-gray-400 font-medium">
                    zapytań
                  </div>
                </div>
              </div>
              
              <div className="group/input">
                <label className="block text-sm font-semibold text-gray-700 mb-2 transition-colors group-focus-within/input:text-emerald-600">Okres odnawiania limitu</label>
                <div className="relative">
                  <input 
                    type="number" 
                    value={globalSettings.scanLimitRollingDays} 
                    onChange={e => setGlobalSettings({...globalSettings, scanLimitRollingDays: parseInt(e.target.value) || 0})} 
                    className="w-full pl-4 pr-12 py-3 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-all font-medium text-gray-900 outline-none"
                  />
                  <div className="absolute inset-y-0 right-0 flex items-center pr-4 text-gray-400 font-medium">
                    dni
                  </div>
                </div>
              </div>
              
              <button 
                onClick={handleSaveGlobalSettings} 
                disabled={isSavingGlobal}
                className="w-full flex items-center justify-center gap-2 bg-gray-900 hover:bg-emerald-600 text-white px-6 py-3.5 rounded-xl font-semibold transition-all duration-300 shadow-lg hover:shadow-emerald-500/30 hover:-translate-y-0.5 disabled:opacity-70 disabled:hover:translate-y-0"
              >
                {isSavingGlobal ? (
                  <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                ) : (
                  <>
                    <Save className="w-5 h-5" />
                    Zapisz Ustawienia
                  </>
                )}
              </button>
            </div>
          </div>

          <div className="bg-gradient-to-br from-emerald-50 to-teal-50 rounded-3xl p-6 border border-emerald-100 flex gap-4 items-start shadow-sm">
            <ShieldAlert className="w-6 h-6 text-emerald-600 shrink-0 mt-0.5" />
            <p className="text-sm text-emerald-900 font-medium leading-relaxed">
              Zmiana ustawień globalnych wpłynie na wszystkich nowych użytkowników oraz tych, którzy nie mają przypisanego limitu indywidualnego.
            </p>
          </div>
        </div>

        {/* Users Data Table */}
        <div className="xl:col-span-2">
          <div className="bg-white rounded-3xl shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-gray-100 overflow-hidden flex flex-col h-full">
            <div className="px-8 py-6 border-b border-gray-100 flex items-center justify-between bg-white/50 backdrop-blur-md">
              <div className="flex items-center gap-3">
                <Users className="w-6 h-6 text-blue-500" />
                <h2 className="text-xl font-bold text-gray-800">Zarządzanie Użytkownikami</h2>
              </div>
              <div className="text-sm font-medium text-gray-500 bg-gray-100 px-3 py-1 rounded-full">
                {users.length} użytkowników
              </div>
            </div>
            
            <div className="flex-1 overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-gray-50/50">
                    <th className="px-8 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider border-b border-gray-100">ID Użytkownika</th>
                    <th className="px-8 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider border-b border-gray-100 text-center">Zużycie Limitu</th>
                    <th className="px-8 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider border-b border-gray-100 text-center">Status</th>
                    <th className="px-8 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider border-b border-gray-100 text-right">Akcje</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {isLoadingUsers ? (
                    <tr>
                      <td colSpan={4} className="px-8 py-12 text-center text-gray-400">
                        <div className="flex flex-col items-center justify-center gap-3">
                          <div className="w-8 h-8 border-2 border-emerald-100 border-t-emerald-500 rounded-full animate-spin"></div>
                          <span className="font-medium animate-pulse">Ładowanie danych...</span>
                        </div>
                      </td>
                    </tr>
                  ) : users.length === 0 ? (
                    <tr>
                      <td colSpan={4} className="px-8 py-12 text-center text-gray-500 font-medium">Brak danych do wyświetlenia.</td>
                    </tr>
                  ) : (
                    users.map((user, idx) => {
                      const usagePercent = Math.min((user.usage / user.maxScans) * 100, 100);
                      const isNearingLimit = usagePercent >= 80;
                      const isBlocked = user.usage >= user.maxScans;

                      return (
                        <tr key={user.userId} className="hover:bg-gray-50/50 transition-colors group">
                          <td className="px-8 py-5">
                            <div className="font-mono text-sm font-semibold text-gray-700 bg-gray-100/50 inline-block px-2 py-1 rounded border border-gray-200/50">
                              {user.userId}
                            </div>
                          </td>
                          <td className="px-8 py-5">
                            <div className="flex flex-col items-center gap-2">
                              <span className="text-sm font-bold text-gray-700">
                                {user.usage} <span className="text-gray-400 font-medium">/ {user.maxScans}</span>
                              </span>
                              <div className="w-full max-w-[120px] h-2 bg-gray-100 rounded-full overflow-hidden">
                                <div 
                                  className={`h-full rounded-full transition-all duration-1000 ${isBlocked ? 'bg-red-500' : isNearingLimit ? 'bg-amber-500' : 'bg-emerald-500'}`}
                                  style={{ width: `${usagePercent}%` }}
                                ></div>
                              </div>
                            </div>
                          </td>
                          <td className="px-8 py-5 text-center">
                            {isBlocked ? (
                              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold bg-red-100 text-red-700 border border-red-200">
                                <span className="w-1.5 h-1.5 rounded-full bg-red-500 animate-pulse"></span>
                                Zablokowany
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold bg-emerald-100 text-emerald-700 border border-emerald-200">
                                <CheckCircle2 className="w-3 h-3" />
                                Aktywny
                              </span>
                            )}
                          </td>
                          <td className="px-8 py-5 text-right">
                            <button 
                              onClick={() => handleOpenEditModal(user)}
                              className="inline-flex items-center justify-center p-2 rounded-xl text-gray-400 hover:text-emerald-600 hover:bg-emerald-50 transition-all border border-transparent hover:border-emerald-100 group-hover:scale-105 active:scale-95"
                              title="Edytuj limit"
                            >
                              <Edit3 className="w-5 h-5" />
                            </button>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      {/* Edit Limit Modal - using glassmorphism */}
      {editingUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          {/* Backdrop */}
          <div 
            className="absolute inset-0 bg-gray-900/40 backdrop-blur-sm transition-opacity animate-in fade-in duration-300"
            onClick={handleCloseEditModal}
          ></div>
          
          {/* Modal Content */}
          <div className="relative bg-white rounded-3xl shadow-2xl border border-gray-100 w-full max-w-md overflow-hidden animate-in zoom-in-95 fade-in duration-300">
            <div className="px-6 py-5 border-b border-gray-100 flex items-center justify-between bg-gray-50/50">
              <h3 className="text-xl font-bold text-gray-900">Edytuj Limit Użytkownika</h3>
              <button 
                onClick={handleCloseEditModal}
                className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-200/50 rounded-full transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            
            <div className="p-6 space-y-6">
              <div>
                <p className="text-sm text-gray-500 mb-1">ID Użytkownika</p>
                <p className="font-mono text-sm font-semibold text-gray-800 bg-gray-100 px-3 py-2 rounded-lg inline-block">
                  {editingUser.userId}
                </p>
              </div>

              <div className="space-y-2">
                <label className="block text-sm font-semibold text-gray-700">Maksymalna liczba skanowań</label>
                <input 
                  type="number" 
                  value={newLimitInput} 
                  onChange={e => setNewLimitInput(parseInt(e.target.value) || 0)} 
                  className="w-full px-4 py-3 bg-white border border-gray-300 rounded-xl focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-all font-medium text-gray-900 outline-none text-lg"
                />
              </div>

              <div className="bg-blue-50 text-blue-800 p-4 rounded-xl text-sm font-medium border border-blue-100 flex items-start gap-3">
                <Activity className="w-5 h-5 shrink-0 text-blue-500" />
                <p>Ten użytkownik wykorzystał już {editingUser.usage} skanowań w bieżącym okresie.</p>
              </div>
            </div>

            <div className="px-6 py-5 bg-gray-50 border-t border-gray-100 flex items-center justify-end gap-3">
              <button 
                onClick={handleCloseEditModal}
                className="px-5 py-2.5 rounded-xl font-semibold text-gray-600 hover:bg-gray-200/50 transition-colors"
              >
                Anuluj
              </button>
              <button 
                onClick={handleSaveUserLimit}
                disabled={isSavingLimit}
                className="flex items-center gap-2 bg-emerald-500 hover:bg-emerald-600 text-white px-6 py-2.5 rounded-xl font-semibold transition-all shadow-md hover:shadow-emerald-500/30 hover:-translate-y-0.5 active:translate-y-0 disabled:opacity-70 disabled:hover:translate-y-0"
              >
                {isSavingLimit ? (
                  <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                ) : (
                  <CheckCircle2 className="w-4 h-4" />
                )}
                Zapisz Zmiany
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  );
};

export default RateLimitsView;
