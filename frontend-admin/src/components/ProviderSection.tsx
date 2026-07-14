import React, { useState } from 'react';
import { Server, Plus, AlertCircle } from 'lucide-react';
import { LlmProvider, CreateProviderDto } from '../types';
import { DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors, DragEndEvent } from '@dnd-kit/core';
import { arrayMove, SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy } from '@dnd-kit/sortable';
import SortableProviderKey from './SortableProviderKey';

interface ProviderSectionProps {
  providerName: string;
  keys: LlmProvider[];
  isGlobalPrimary: boolean;
  onSave: (id: string | undefined, data: CreateProviderDto) => Promise<void>;
  onTest: (id: string) => Promise<boolean>;
  onDelete: (id: string) => Promise<void>;
  onSetPrimary: (providerName: string, fallbackId?: string) => Promise<void>;
  onReorder: (providerName: string, orderedIds: string[]) => Promise<void>;
}

const ProviderSection: React.FC<ProviderSectionProps> = ({
  providerName,
  keys,
  isGlobalPrimary,
  onSave,
  onTest,
  onDelete,
  onSetPrimary,
  onReorder
}) => {
  const [isAddingNew, setIsAddingNew] = useState(false);
  const [localKeys, setLocalKeys] = useState(keys);

  // Sync local state when props change
  React.useEffect(() => {
    setLocalKeys(keys.sort((a, b) => a.priorityOrder - b.priorityOrder));
  }, [keys]);

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      const oldIndex = localKeys.findIndex((item) => item.id === active.id);
      const newIndex = localKeys.findIndex((item) => item.id === over.id);
      
      const newKeys = arrayMove(localKeys, oldIndex, newIndex);
      setLocalKeys(newKeys);
      
      try {
        await onReorder(providerName, newKeys.map(k => k.id));
      } catch (err) {
        // Revert on fail
        setLocalKeys(keys);
      }
    }
  };

  const handleSetPrimary = () => {
    const firstKey = keys[0];
    onSetPrimary(providerName, firstKey?.id);
  };

  return (
    <div className={`bg-surface rounded-2xl shadow-sm border p-6 flex flex-col gap-6 transition-colors ${isGlobalPrimary ? 'border-primary ring-1 ring-primary/20' : 'border-gray-200'}`}>
      <div className="flex items-center justify-between border-b border-gray-100 pb-4">
        <div className="flex items-center gap-3">
          <div className={`p-2 rounded-lg ${isGlobalPrimary ? 'bg-emerald-100 text-emerald-600' : 'bg-gray-100 text-gray-700'}`}>
            <Server className="w-6 h-6" />
          </div>
          <div>
            <h3 className="text-xl font-bold text-gray-900">{providerName}</h3>
            {isGlobalPrimary && <p className="text-xs font-semibold text-emerald-600 mt-0.5">Aktywny Routing LLM</p>}
          </div>
        </div>
        
        {!isGlobalPrimary && (
          <button
            onClick={handleSetPrimary}
            className="px-4 py-2 text-sm font-medium text-secondary bg-blue-50 hover:bg-blue-100 rounded-lg transition-colors"
          >
            Ustaw jako główny
          </button>
        )}
      </div>

      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <h4 className="font-semibold text-gray-800">Keys & Fallbacks</h4>
          <span className="text-xs text-gray-500 bg-gray-100 px-2 py-1 rounded-full">{localKeys.length} {localKeys.length === 1 ? 'key' : 'keys'}</span>
        </div>

        {localKeys.length === 0 && !isAddingNew && (
          <div className="flex flex-col items-center justify-center p-8 bg-gray-50 border border-dashed border-gray-300 rounded-xl">
            <AlertCircle className="w-8 h-8 text-gray-400 mb-2" />
            <p className="text-sm text-gray-500">Brak skonfigurowanych kluczy.</p>
          </div>
        )}

        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={localKeys.map(k => k.id)} strategy={verticalListSortingStrategy}>
            <div className="flex flex-col gap-3">
              {localKeys.map((keyData) => (
                <SortableProviderKey 
                  key={keyData.id} 
                  item={keyData} 
                  providerName={providerName}
                  onSave={onSave}
                  onTest={onTest}
                  onDelete={onDelete}
                />
              ))}
            </div>
          </SortableContext>
        </DndContext>

        {isAddingNew && (
          <div className="mt-2">
            <SortableProviderKey 
              item={{ id: 'new', isNew: true }} 
              providerName={providerName}
              onSave={onSave}
              onTest={onTest}
              onDelete={onDelete}
              onCancelNew={() => setIsAddingNew(false)}
            />
          </div>
        )}

        {!isAddingNew && (
          <button 
            onClick={() => setIsAddingNew(true)}
            className="mt-2 flex items-center justify-center gap-2 w-full py-3 border-2 border-dashed border-gray-200 rounded-xl text-sm font-medium text-gray-500 hover:border-gray-300 hover:bg-gray-50 hover:text-gray-700 transition-all"
          >
            <Plus className="w-4 h-4" />
            Dodaj klucz
          </button>
        )}
      </div>
    </div>
  );
};

export default ProviderSection;
