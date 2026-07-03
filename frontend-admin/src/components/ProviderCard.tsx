import React, { useState, useEffect } from 'react';
import { Server, Save, Activity, CheckCircle, XCircle } from 'lucide-react';
import { LlmProvider, CreateProviderDto } from '../types';

interface ProviderCardProps {
  providerName: string;
  existingData?: LlmProvider;
  onSave: (data: CreateProviderDto) => Promise<void>;
  onTest: (id: string) => Promise<boolean>;
  onSetPrimary: (id: string) => Promise<void>;
  isGlobalPrimary: string | null;
}

const ProviderCard: React.FC<ProviderCardProps> = ({ 
  providerName, 
  existingData, 
  onSave, 
  onTest,
  onSetPrimary,
  isGlobalPrimary 
}) => {
  const [modelName, setModelName] = useState(existingData?.modelName || '');
  const [apiKey, setApiKey] = useState('');
  const [isActive, setIsActive] = useState(existingData?.isActive ?? true);
  const [testStatus, setTestStatus] = useState<'idle' | 'testing' | 'success' | 'error'>('idle');
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (existingData) {
      setModelName(existingData.modelName);
      setIsActive(existingData.isActive);
    }
  }, [existingData]);

  const isPrimary = isGlobalPrimary === existingData?.id;

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await onSave({
        providerName,
        modelName,
        apiKey: apiKey || 'KEEP_EXISTING', // Assuming backend logic handles this, or just require key
        isPrimary: existingData?.isPrimary || false,
        isActive
      });
      setApiKey(''); // clear key after save
    } finally {
      setIsSaving(false);
    }
  };

  const handleTest = async () => {
    if (!existingData?.id) return;
    setTestStatus('testing');
    const success = await onTest(existingData.id);
    setTestStatus(success ? 'success' : 'error');
  };

  return (
    <div className={`bg-surface rounded-xl shadow-sm border p-6 flex flex-col gap-4 transition-colors ${isPrimary ? 'border-primary ring-1 ring-primary/20' : 'border-gray-200'}`}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-gray-100 rounded-lg">
            <Server className="w-6 h-6 text-gray-700" />
          </div>
          <h3 className="text-lg font-semibold">{providerName}</h3>
        </div>
        <label className="relative inline-flex items-center cursor-pointer">
          <input 
            type="checkbox" 
            className="sr-only peer" 
            checked={isActive}
            onChange={(e) => setIsActive(e.target.checked)}
          />
          <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
        </label>
      </div>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Model Name</label>
          <input 
            type="text" 
            value={modelName}
            onChange={(e) => setModelName(e.target.value)}
            className="w-full rounded-lg border-gray-300 border px-3 py-2 focus:ring-primary focus:border-primary text-sm"
            placeholder={providerName === 'OpenAI' ? 'gpt-4o' : 'gemini-1.5-flash'}
          />
        </div>
        
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">API Key</label>
          <input 
            type="password" 
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            className="w-full rounded-lg border-gray-300 border px-3 py-2 focus:ring-primary focus:border-primary text-sm"
            placeholder={existingData ? '••••••••••••••••' : 'Enter API Key'}
          />
        </div>
      </div>

      <div className="mt-auto pt-4 flex items-center justify-between border-t border-gray-100">
        <div className="flex gap-2">
          <button 
            onClick={handleTest}
            disabled={!existingData || testStatus === 'testing'}
            className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 disabled:opacity-50"
          >
            <Activity className="w-4 h-4" />
            Testuj
          </button>
          
          {testStatus === 'success' && <CheckCircle className="w-5 h-5 text-emerald-500 my-auto" />}
          {testStatus === 'error' && <XCircle className="w-5 h-5 text-red-500 my-auto" />}
        </div>
        
        <button 
          onClick={handleSave}
          disabled={isSaving || (!existingData && !apiKey)}
          className="flex items-center gap-1.5 px-4 py-1.5 text-sm font-medium text-white bg-primary rounded-lg hover:bg-emerald-600 transition-colors disabled:opacity-50"
        >
          <Save className="w-4 h-4" />
          Zapisz
        </button>
      </div>

      {existingData && !isPrimary && (
        <button
          onClick={() => onSetPrimary(existingData.id)}
          className="w-full mt-2 py-2 text-sm font-medium text-secondary bg-blue-50 hover:bg-blue-100 rounded-lg transition-colors"
        >
          Ustaw jako główny
        </button>
      )}
      {isPrimary && (
        <div className="w-full mt-2 py-2 text-sm font-medium text-center text-primary bg-emerald-50 rounded-lg">
          Główny Provider
        </div>
      )}
    </div>
  );
};

export default ProviderCard;
