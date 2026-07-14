import React, { useState } from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Save, Activity, CheckCircle, XCircle, Trash2, GripVertical } from 'lucide-react';
import { LlmProvider, CreateProviderDto } from '../types';

interface SortableProviderKeyProps {
  item: LlmProvider | { id: string, isNew: true };
  providerName: string;
  onSave: (id: string | undefined, data: CreateProviderDto) => Promise<void>;
  onTest: (id: string) => Promise<boolean>;
  onDelete: (id: string) => Promise<void>;
  onCancelNew?: () => void;
}

const SortableProviderKey: React.FC<SortableProviderKeyProps> = ({ item, providerName, onSave, onTest, onDelete, onCancelNew }) => {
  const isNew = 'isNew' in item;
  const existingData = isNew ? undefined : (item as LlmProvider);
  
  const [modelName, setModelName] = useState(existingData?.modelName || '');
  const [apiKey, setApiKey] = useState('');
  const [isActive, setIsActive] = useState(existingData?.isActive ?? true);
  const [testStatus, setTestStatus] = useState<'idle' | 'testing' | 'success' | 'error'>('idle');
  const [isSaving, setIsSaving] = useState(false);

  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ 
    id: item.id,
    disabled: isNew // Disable dragging for new un-saved items
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    zIndex: isDragging ? 10 : 1,
    opacity: isDragging ? 0.8 : 1,
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await onSave(existingData?.id, {
        providerName,
        modelName,
        apiKey: apiKey || 'KEEP_EXISTING',
        isPrimary: existingData?.isPrimary || false,
        isActive
      });
      setApiKey('');
      if (isNew && onCancelNew) onCancelNew(); // Remove the "new" form once saved, parent will re-fetch
    } catch (err) {
      // handled by parent
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
    <div 
      ref={setNodeRef} 
      style={style} 
      className={`relative bg-white border rounded-xl p-4 shadow-sm flex flex-col gap-3 ${isDragging ? 'ring-2 ring-primary border-primary' : 'border-gray-200'}`}
    >
      <div className="flex items-center gap-3">
        {!isNew && (
          <div {...attributes} {...listeners} className="cursor-grab hover:bg-gray-100 p-1 rounded">
            <GripVertical className="w-5 h-5 text-gray-400" />
          </div>
        )}
        <div className="flex-1 font-medium text-gray-700">
          {isNew ? 'New Key Setup' : `Model: ${existingData?.modelName}`}
        </div>
        <div className="flex items-center gap-2">
          <label className="relative inline-flex items-center cursor-pointer">
            <input 
              type="checkbox" 
              className="sr-only peer" 
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
            />
            <div className="w-9 h-5 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-emerald-500"></div>
          </label>
          {!isNew && existingData && (
            <button onClick={() => onDelete(existingData.id)} className="p-1.5 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors">
              <Trash2 className="w-4 h-4" />
            </button>
          )}
          {isNew && (
            <button onClick={onCancelNew} className="p-1.5 text-gray-400 hover:text-gray-600 transition-colors">
              <XCircle className="w-5 h-5" />
            </button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Model Name</label>
          <input 
            type="text" 
            value={modelName}
            onChange={(e) => setModelName(e.target.value)}
            className="w-full rounded-lg border-gray-300 border px-3 py-2 focus:ring-primary focus:border-primary text-sm"
            placeholder={providerName === 'OpenAI' ? 'gpt-4o' : 'gemini-1.5-flash'}
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">API Key</label>
          <input 
            type="password" 
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            className="w-full rounded-lg border-gray-300 border px-3 py-2 focus:ring-primary focus:border-primary text-sm"
            placeholder={!isNew ? '••••••••••••••••' : 'Enter API Key'}
          />
        </div>
      </div>

      <div className="flex items-center justify-between pt-2 border-t border-gray-50">
        <div className="flex gap-2">
          {!isNew && existingData && (
            <>
              <button 
                onClick={handleTest}
                disabled={testStatus === 'testing'}
                className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 disabled:opacity-50"
              >
                <Activity className="w-3.5 h-3.5" />
                Test
              </button>
              {testStatus === 'success' && <CheckCircle className="w-4 h-4 text-emerald-500 my-auto" />}
              {testStatus === 'error' && <XCircle className="w-4 h-4 text-red-500 my-auto" />}
            </>
          )}
        </div>
        <button 
          onClick={handleSave}
          disabled={isSaving || (!existingData && !apiKey)}
          className="flex items-center gap-1.5 px-4 py-1.5 text-xs font-medium text-white bg-primary rounded-lg hover:bg-emerald-600 transition-colors disabled:opacity-50"
        >
          <Save className="w-3.5 h-3.5" />
          Zapisz
        </button>
      </div>
    </div>
  );
};

export default SortableProviderKey;
