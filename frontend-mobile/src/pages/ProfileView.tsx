import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { profileApi, UserProfile, CatCreateDto } from '../api/profileApi';
import { Trash2, Plus, X, Loader2 } from 'lucide-react';

const MAX_SCANS = 5;
const WARNING_THRESHOLD_PERCENTAGE = 20;

const getProgressBarClass = (isWarning: boolean) => {
  return isWarning 
    ? 'bg-red-500 shadow-[0_0_12px_rgba(239,68,68,0.8)]' 
    : 'bg-emerald-500';
};

const ProfileView: React.FC = () => {
  const { t } = useTranslation();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isAdding, setIsAdding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [newCat, setNewCat] = useState<CatCreateDto>({
    name: '',
    breed: '',
    weight: 0,
    allergies: ''
  });

  useEffect(() => {
    fetchProfile();
  }, []);

  const fetchProfile = async () => {
    try {
      setError(null);
      const data = await profileApi.getProfile();
      setProfile(data);
    } catch (err: any) {
      console.error(err);
      setError(err.message || t('errorLoadProfile'));
    } finally {
      setLoading(false);
    }
  };

  const handleAddCat = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newCat.name || !newCat.breed) return;
    setIsAdding(true);
    setError(null);
    try {
      const addedCat = await profileApi.addCat(newCat);
      if (profile) {
        setProfile({
          ...profile,
          cats: [...profile.cats, addedCat]
        });
      }
      setIsModalOpen(false);
      setNewCat({ name: '', breed: '', weight: 0, allergies: '' });
    } catch (err: any) {
      console.error(err);
      setError(err.message || t('errorAddCat'));
    } finally {
      setIsAdding(false);
    }
  };

  const handleDeleteCat = async (id: string) => {
    try {
      setError(null);
      await profileApi.deleteCat(id);
      if (profile) {
        setProfile({
          ...profile,
          cats: profile.cats.filter((c) => c.id !== id)
        });
      }
    } catch (err: any) {
      console.error(err);
      setError(err.message || t('errorDeleteCat'));
    }
  };

  if (loading || !profile) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center pt-20">
        <Loader2 className="w-10 h-10 animate-spin text-emerald-500 mb-4" />
        <p className="text-gray-500">{t('loadingReading')}</p>
      </div>
    );
  }

  if (error && !profile) {
    return (
      <div className="flex-1 px-4 pt-12 pb-32">
        <h1 className="text-4xl font-extrabold text-gray-900 tracking-tight mb-8">
          {t('yourProfile')}
        </h1>
        <div className="bg-red-50 text-red-600 p-4 rounded-xl mb-6">
          {error}
        </div>
      </div>
    );
  }

  if (!profile) return null;

  const scansPercentage = Math.max(0, Math.min(100, (profile.remainingScans / MAX_SCANS) * 100));
  const isWarningState = scansPercentage <= WARNING_THRESHOLD_PERCENTAGE;
  const progressBarClass = getProgressBarClass(isWarningState);

  return (
    <div className="flex-1 px-4 pt-12 pb-32">
      <h1 className="text-4xl font-extrabold text-gray-900 tracking-tight mb-8">
        {t('yourProfile')}
      </h1>

      {error && (
        <div className="bg-red-50 text-red-600 p-4 rounded-xl mb-6">
          {error}
        </div>
      )}

      {/* Quota Card */}
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6 mb-8 relative overflow-hidden">
        <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-2">
          {t('remainingScans')}
        </h2>
        <div className="flex items-end gap-2 mb-4">
          <span className="text-5xl font-black text-gray-900">{profile.remainingScans}</span>
          <span className="text-xl font-medium text-gray-400 mb-1">/ {MAX_SCANS}</span>
        </div>
        
        {/* Progress Bar */}
        <div className="h-3 w-full bg-gray-100 rounded-full overflow-hidden">
          <div 
            className={`h-full rounded-full transition-all duration-1000 ease-out ${progressBarClass}`}
            style={{ width: `${scansPercentage}%` }}
          />
        </div>
      </div>

      {/* Cats Section */}
      <h2 className="text-xl font-bold text-gray-900 mb-4">{t('yourCats')}</h2>
      
      {profile.cats.length > 0 ? (
        <ul className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden divide-y divide-gray-100">
          {profile.cats.map((cat) => (
            <li key={cat.id} className="p-4 flex items-center justify-between hover:bg-gray-50 transition-colors">
              <div>
                <h3 className="text-lg font-bold text-gray-900">{cat.name}</h3>
                <p className="text-sm text-gray-500 mt-1">
                  {t('breed')}: <span className="text-gray-700">{cat.breed}</span> &bull; {t('weight')}: <span className="text-gray-700">{cat.weight}</span>
                </p>
                {cat.allergies && (
                  <p className="text-sm text-gray-500 mt-0.5">
                    {t('allergies')}: <span className="text-red-500 font-medium">{cat.allergies}</span>
                  </p>
                )}
              </div>
              <button 
                onClick={() => handleDeleteCat(cat.id)}
                className="p-3 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-full transition-colors active:scale-95"
              >
                <Trash2 className="w-5 h-5" />
              </button>
            </li>
          ))}
        </ul>
      ) : (
        <div className="text-center py-8 bg-white rounded-2xl shadow-sm border border-gray-100 text-gray-500">
          {t('noCatsAdded')}
        </div>
      )}

      {/* Floating Add Button */}
      <div className="fixed bottom-24 left-0 right-0 px-4 flex justify-center z-10">
        <button 
          onClick={() => setIsModalOpen(true)}
          className="bg-emerald-500 hover:bg-emerald-600 text-white shadow-lg shadow-emerald-500/30 rounded-full py-4 px-8 font-bold text-lg flex items-center gap-2 transform active:scale-95 transition-all w-full max-w-sm justify-center"
        >
          <Plus className="w-6 h-6" />
          {t('addCat')}
        </button>
      </div>

      {/* Bottom Sheet / Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/40 backdrop-blur-sm transition-opacity">
          <div 
            className="bg-white w-full sm:max-w-md rounded-t-3xl sm:rounded-3xl p-6 transform transition-transform animate-slide-up sm:animate-fade-in"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-bold text-gray-900">{t('addCat')}</h2>
              <button 
                onClick={() => setIsModalOpen(false)}
                className="p-2 bg-gray-100 text-gray-500 rounded-full hover:bg-gray-200 active:scale-95 transition-all"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleAddCat} className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">{t('name')}</label>
                <input 
                  type="text" 
                  required
                  value={newCat.name}
                  onChange={(e) => setNewCat({...newCat, name: e.target.value})}
                  className="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-3 text-gray-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition-all"
                />
              </div>
              
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">{t('breed')}</label>
                <input 
                  type="text" 
                  required
                  value={newCat.breed}
                  onChange={(e) => setNewCat({...newCat, breed: e.target.value})}
                  className="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-3 text-gray-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition-all"
                />
              </div>
              
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">{t('weight')}</label>
                <input 
                  type="number" 
                  step="0.1"
                  required
                  value={newCat.weight || ''}
                  onChange={(e) => setNewCat({...newCat, weight: parseFloat(e.target.value)})}
                  className="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-3 text-gray-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition-all"
                />
              </div>
              
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">{t('allergies')}</label>
                <input 
                  type="text" 
                  value={newCat.allergies}
                  onChange={(e) => setNewCat({...newCat, allergies: e.target.value})}
                  className="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-3 text-gray-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition-all"
                  placeholder={t('allergiesPlaceholder')}
                />
              </div>

              <div className="pt-4 flex gap-3">
                <button 
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="flex-1 py-4 px-4 bg-gray-100 text-gray-700 rounded-xl font-bold hover:bg-gray-200 active:scale-95 transition-all"
                >
                  {t('cancel')}
                </button>
                <button 
                  type="submit"
                  disabled={isAdding}
                  className="flex-1 py-4 px-4 bg-emerald-500 text-white rounded-xl font-bold hover:bg-emerald-600 active:scale-95 transition-all shadow-md shadow-emerald-500/20 disabled:opacity-70 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                >
                  {isAdding && <Loader2 className="w-5 h-5 animate-spin" />}
                  {t('save')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default ProfileView;
