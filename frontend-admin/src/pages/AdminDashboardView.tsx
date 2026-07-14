import React, { useEffect, useState } from 'react';
import { providersApi } from '../api/providersApi';
import { LlmProvider, CreateProviderDto } from '../types';
import ProviderSection from '../components/ProviderSection';
import { ShieldCheck } from 'lucide-react';

const SUPPORTED_PROVIDERS = ['OpenAI', 'Anthropic', 'Gemini'];

const AdminDashboardView: React.FC = () => {
  const [providers, setProviders] = useState<LlmProvider[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);



  const fetchData = async () => {
    try {
      const data = await providersApi.getProviders();
      setProviders(data);

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

  const handleSetPrimary = async (providerName: string, id?: string) => {
    if (!id) {
      setError(`Cannot set ${providerName} as primary. Please add at least one key first.`);
      return;
    }
    try {
      await providersApi.setPrimary(id);
      await fetchData();
      setError(null);
    } catch (err: any) {
      setError('Error setting primary: ' + err.message);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await providersApi.deleteProvider(id);
      await fetchData();
      setError(null);
    } catch (err: any) {
      setError('Error deleting provider: ' + err.message);
    }
  };

  const handleReorder = async (providerName: string, orderedIds: string[]) => {
    try {
      await providersApi.reorderProviders(providerName, orderedIds);
      await fetchData();
      setError(null);
    } catch (err: any) {
      setError('Error reordering providers: ' + err.message);
      throw err;
    }
  };

  const primaryProvider = providers.find(p => p.isPrimary);



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



      <div>
        <h3 className="text-lg font-bold text-gray-900 mb-4">Dostępni Dostawcy</h3>
        <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6 items-start">
          {SUPPORTED_PROVIDERS.map(providerName => {
            const providerKeys = providers.filter(p => p.providerName === providerName);
            const isGlobalPrimary = primaryProvider?.providerName === providerName;
            return (
              <ProviderSection
                key={providerName}
                providerName={providerName}
                keys={providerKeys}
                isGlobalPrimary={isGlobalPrimary}
                onSave={handleSave}
                onTest={handleTest}
                onDelete={handleDelete}
                onSetPrimary={handleSetPrimary}
                onReorder={handleReorder}
              />
            );
          })}
        </div>
      </div>
    </div>
  );
};

export default AdminDashboardView;
