import React, { useEffect, useState } from 'react';
import { providersApi } from '../api/providersApi';
import { LlmProvider, CreateProviderDto } from '../types';
import ProviderCard from '../components/ProviderCard';
import { ShieldCheck, Settings } from 'lucide-react';
import * as adminSettingsApi from '../api/adminSettingsApi';

const SUPPORTED_PROVIDERS = ['OpenAI', 'Anthropic', 'Gemini'];

const AdminDashboardView: React.FC = () => {
  const [providers, setProviders] = useState<LlmProvider[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [globalSettings, setGlobalSettings] = useState<adminSettingsApi.GlobalSettings>({ globalScanLimit: 5, scanLimitRollingDays: 7 });
  const [userLimitSearchId, setUserLimitSearchId] = useState('');
  const [userLimitResult, setUserLimitResult] = useState<adminSettingsApi.UserLimit | null>(null);
  const [userLimitInput, setUserLimitInput] = useState<number>(5);

  const fetchData = async () => {
    try {
      const data = await providersApi.getProviders();
      setProviders(data);
      const token = localStorage.getItem('access_token') || ''; // We should get the token from auth context, assuming it's managed somewhere
      if (token) {
          const settings = await adminSettingsApi.getGlobalSettings(token);
          setGlobalSettings(settings);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to fetch data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleSave = async (id: string | undefined, data: CreateProviderDto) => {
    try {
      if (id) {
        await providersApi.updateProvider(id, data);
      } else {
        await providersApi.createProvider(data);
      }
      await fetchData();
      setError(null);
    } catch (err: any) {
      setError('Error saving provider: ' + err.message);
      throw err;
    }
  };

  const handleTest = async (id: string): Promise<boolean> => {
    try {
      await providersApi.testConnection(id);
      return true;
    } catch (err) {
      return false;
    }
  };

  const handleSetPrimary = async (id: string) => {
    try {
      await providersApi.setPrimary(id);
      await fetchData();
      setError(null);
    } catch (err: any) {
      setError('Error setting primary: ' + err.message);
    }
  };

  const primaryProvider = providers.find(p => p.isPrimary);

  const handleSaveGlobalSettings = async () => {
      try {
          const token = localStorage.getItem('access_token') || '';
          await adminSettingsApi.updateGlobalSettings(token, globalSettings);
          alert('Zapisano ustawienia globalne!');
      } catch (err: any) {
          setError('Błąd zapisu ustawień: ' + err.message);
      }
  };

  const handleSearchUserLimit = async () => {
      if (!userLimitSearchId) return;
      try {
          const token = localStorage.getItem('access_token') || '';
          const result = await adminSettingsApi.getUserLimit(token, userLimitSearchId);
          setUserLimitResult(result);
          setUserLimitInput(result.maxScans);
      } catch (err: any) {
          if (err.response?.status === 404) {
              setUserLimitResult({ userId: userLimitSearchId, maxScans: globalSettings.globalScanLimit });
              setUserLimitInput(globalSettings.globalScanLimit);
          } else {
              setError('Błąd wyszukiwania: ' + err.message);
          }
      }
  };

  const handleSaveUserLimit = async () => {
      if (!userLimitResult) return;
      try {
          const token = localStorage.getItem('access_token') || '';
          await adminSettingsApi.updateUserLimit(token, userLimitResult.userId, userLimitInput);
          alert('Zapisano limit dla użytkownika!');
      } catch (err: any) {
          setError('Błąd zapisu limitu: ' + err.message);
      }
  };

  if (loading) return <div className="text-gray-500">Loading configurations...</div>;

  return (
    <div className="max-w-6xl mx-auto space-y-8">
      {error && (
        <div className="bg-red-100 border-l-4 border-red-500 text-red-700 p-4 rounded-md shadow-sm flex justify-between items-center">
          <div>
            <p className="font-bold">Error</p>
            <p>{error}</p>
          </div>
          <button onClick={() => setError(null)} className="text-2xl leading-none px-2 font-semibold hover:text-red-900">&times;</button>
        </div>
      )}
      {/* Global Route Selector Card */}
      <div className="bg-gradient-to-r from-emerald-500 to-emerald-600 rounded-2xl p-8 text-white shadow-lg flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold mb-2">Aktywny Routing LLM</h2>
          <p className="text-emerald-100">
            Wszystkie zapytania aplikacji mobilnej są obecnie kierowane do tego providera.
          </p>
        </div>
        <div className="bg-white/20 px-6 py-4 rounded-xl backdrop-blur-sm flex items-center gap-4 border border-white/30">
          <ShieldCheck className="w-8 h-8 text-white" />
          <div>
            <div className="text-sm text-emerald-100 font-medium">Primary Provider</div>
            <div className="text-xl font-bold">{primaryProvider?.providerName || 'Brak'}</div>
          </div>
        </div>
      </div>

      {/* Rate Limits Config */}
      <div className="bg-white rounded-2xl p-8 shadow-sm border border-gray-100">
        <div className="flex items-center gap-3 mb-6">
            <Settings className="text-blue-500" />
            <h3 className="text-xl font-bold text-gray-900">Limity Skanowań (Rate Limits)</h3>
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
            <div className="space-y-4">
                <h4 className="font-semibold text-gray-700">Ustawienia Globalne</h4>
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Domyślny limit skanowań</label>
                    <input type="number" value={globalSettings.globalScanLimit} onChange={e => setGlobalSettings({...globalSettings, globalScanLimit: parseInt(e.target.value)})} className="w-full px-4 py-2 border rounded-lg" />
                </div>
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Okres limitu (w dniach)</label>
                    <input type="number" value={globalSettings.scanLimitRollingDays} onChange={e => setGlobalSettings({...globalSettings, scanLimitRollingDays: parseInt(e.target.value)})} className="w-full px-4 py-2 border rounded-lg" />
                </div>
                <button onClick={handleSaveGlobalSettings} className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition">Zapisz Globalne</button>
            </div>
            <div className="space-y-4 border-t lg:border-t-0 lg:border-l pt-6 lg:pt-0 lg:pl-8 border-gray-200">
                <h4 className="font-semibold text-gray-700">Limit Użytkownika</h4>
                <div className="flex gap-2">
                    <input type="text" placeholder="ID Użytkownika..." value={userLimitSearchId} onChange={e => setUserLimitSearchId(e.target.value)} className="flex-1 px-4 py-2 border rounded-lg" />
                    <button onClick={handleSearchUserLimit} className="bg-gray-800 text-white px-4 py-2 rounded-lg hover:bg-gray-900">Szukaj</button>
                </div>
                {userLimitResult && (
                    <div className="mt-4 p-4 bg-gray-50 rounded-lg space-y-4 border border-gray-200">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Limit dla {userLimitResult.userId}</label>
                            <input type="number" value={userLimitInput} onChange={e => setUserLimitInput(parseInt(e.target.value))} className="w-full px-4 py-2 border rounded-lg" />
                        </div>
                        <button onClick={handleSaveUserLimit} className="w-full bg-emerald-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-emerald-700 transition">Ustaw Limit</button>
                    </div>
                )}
            </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-bold text-gray-900 mb-4">Dostępni Dostawcy</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {SUPPORTED_PROVIDERS.map(providerName => {
            const existing = providers.find(p => p.providerName === providerName);
            return (
              <ProviderCard
                key={providerName}
                providerName={providerName}
                existingData={existing}
                isGlobalPrimary={primaryProvider?.id || null}
                onSave={handleSave}
                onTest={handleTest}
                onSetPrimary={handleSetPrimary}
              />
            );
          })}
        </div>
      </div>
    </div>
  );
};

export default AdminDashboardView;
