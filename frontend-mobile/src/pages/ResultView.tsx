import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { ArrowLeft, Share, CheckCircle2, XCircle, ChevronDown, ChevronUp, AlertTriangle, Hourglass } from 'lucide-react';
import { fetchAnalysisResult, uploadImageForAnalysis } from '../api/scanApi';
import { VLMResponseContract } from '../types';
import LoadingOverlay from '../components/LoadingOverlay';
import { useAppStore } from '../store/useAppStore';

export default function ResultView() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const addScan = useAppStore(state => state.addScan);
  
  const [loading, setLoading] = useState(!!id);
  const [result, setResult] = useState<VLMResponseContract | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [ingredientsExpanded, setIngredientsExpanded] = useState(false);

  useEffect(() => {
    const state = location.state as { imageBlob?: Blob, scanMode?: 'Ingredients' | 'General' };
    
    if (!id && !state?.imageBlob) {
      setLoading(false);
      return;
    }
    
    let ignore = false;
    setError(null);
    setLoading(true);
    
    if (state?.imageBlob && state?.scanMode) {
      uploadImageForAnalysis(state.imageBlob, state.scanMode, i18n.language)
        .then(res => {
          if (!ignore) {
            setResult(res);
            addScan({
              id: Date.now().toString(),
              query: 'photo',
              timestamp: Date.now(),
              result: res
            });
          }
        })
        .catch(err => {
          if (!ignore) {
            console.error(err);
            if (err?.response?.status === 429) {
              setError(t('rate_limit_exceeded'));
            } else if (err?.response?.status === 422 && err?.response?.data?.errorCode === 'NO_PET_FOOD_FOUND') {
              setError(t('no_pet_food_found'));
            } else {
              const errorMsg = err?.response?.data?.message || err?.response?.statusText || err?.message || 'Wystąpił błąd podczas skanowania zdjęcia.';
              setError(errorMsg);
            }
          }
        })
        .finally(() => {
          if (!ignore) setLoading(false);
        });
    } else if (id) {
      fetchAnalysisResult(id, i18n.language)
        .then(res => {
          if (!ignore) {
            setResult(res);
            addScan({
              id: Date.now().toString(),
              query: id,
              timestamp: Date.now(),
              result: res
            });
          }
        })
        .catch(err => {
          if (!ignore) {
            if (err?.response?.status === 429) {
              setError(t('rate_limit_exceeded'));
            } else {
              const errorMsg = err?.response?.data?.message || err?.response?.statusText || err?.message || 'Wystąpił błąd podczas skanowania.';
              setError(errorMsg);
            }
          }
        })
        .finally(() => {
          if (!ignore) setLoading(false);
        });
    }
      
    return () => {
      ignore = true;
    };
  }, [id, location.state, i18n.language, addScan]);

  if (loading) {
    return <LoadingOverlay />;
  }

  if (error) {
    const isRateLimit = error === t('rate_limit_exceeded');

    return (
      <div className="min-h-screen bg-[var(--color-background)] flex flex-col items-center justify-center p-6">
        <div className={`bg-white p-8 rounded-3xl shadow-[0_8px_30px_rgb(0,0,0,0.04)] border max-w-sm w-full text-center ${isRateLimit ? 'border-amber-100' : 'border-red-100'}`}>
          <div className={`w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4 ${isRateLimit ? 'bg-amber-50 text-amber-500' : 'bg-red-50 text-red-500'}`}>
            {isRateLimit ? <Hourglass size={32} /> : <AlertTriangle size={32} />}
          </div>
          <h2 className="text-xl font-bold text-gray-900 mb-3">
            {isRateLimit ? t('rate_limit_title') : (t('scan_error') || 'Błąd skanowania')}
          </h2>
          <p className="text-gray-500 mb-8 leading-relaxed">{error}</p>
          <button onClick={() => navigate(-1)} className="w-full px-6 py-4 bg-gray-900 text-white rounded-2xl font-bold shadow-md active:scale-95 transition-transform flex items-center justify-center">
            <ArrowLeft size={20} className="mr-2" />
            {t('go_back')}
          </button>
        </div>
      </div>
    );
  }

  if (!id && !(location.state as any)?.imageBlob) {
    return (
      <div className="min-h-screen bg-[var(--color-background)] flex flex-col items-center justify-center p-6">
        <p className="text-xl text-gray-500 mb-6 text-center">{t('invalid_product_id')}</p>
        <button onClick={() => navigate(-1)} className="px-6 py-3 bg-[var(--color-primary)] text-white rounded-xl font-semibold shadow-lg active:scale-95 transition-transform">
          {t('go_back')}
        </button>
      </div>
    );
  }

  if (!result) {
    return (
      <div className="min-h-screen bg-[var(--color-background)] flex flex-col items-center justify-center p-6">
        <p className="text-xl text-gray-500 mb-6 text-center">{t('product_not_found')}</p>
        <button onClick={() => navigate(-1)} className="px-6 py-3 bg-[var(--color-primary)] text-white rounded-xl font-semibold shadow-lg active:scale-95 transition-transform">
          {t('go_back')}
        </button>
      </div>
    );
  }

  const isGood = result.rating >= 8;
  const isAverage = result.rating >= 5 && result.rating < 8;
  const gradient = isGood 
    ? "from-emerald-400 to-emerald-600" 
    : isAverage 
      ? "from-yellow-400 to-amber-500" 
      : "from-red-400 to-red-600";

  return (
    <div className="min-h-screen pb-24 bg-[var(--color-background)]">
      <header className="sticky top-0 z-10 bg-white/70 backdrop-blur-xl border-b border-gray-100 px-4 pt-12 pb-4 flex justify-between items-center shadow-sm">
        <button onClick={() => navigate(-1)} className="p-2 -ml-2 text-gray-800 hover:bg-gray-100 rounded-full transition-colors">
          <ArrowLeft size={24} />
        </button>
        <div className="flex-1 text-center font-bold text-gray-900 truncate px-4">
          {result.productName}
        </div>
        <button className="p-2 -mr-2 text-gray-800 hover:bg-gray-100 rounded-full transition-colors">
          <Share size={24} />
        </button>
      </header>

      <main className="px-4 pt-6 space-y-6">
        {/* Rating Hero Card */}
        <section className={`relative overflow-hidden rounded-3xl p-8 text-white shadow-xl bg-gradient-to-br ${gradient}`}>
          <div className="absolute top-0 left-0 w-full h-full bg-white/10 mix-blend-overlay"></div>
          <div className="relative z-10 flex flex-col items-center text-center">
            <span className="text-sm uppercase tracking-widest font-semibold mb-2 opacity-90">{t('rating')}</span>
            <div className="text-7xl font-black tracking-tighter mb-4 drop-shadow-md">
              {result.rating}<span className="text-4xl opacity-70">/10</span>
            </div>
            <p className="text-lg font-medium leading-relaxed drop-shadow-sm">
              {result.summary}
            </p>
          </div>
        </section>

        {/* Pros & Cons */}
        <section className="bg-white rounded-3xl p-6 shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-gray-100">
          <div className="space-y-6">
            <div>
              <h3 className="text-lg font-bold text-gray-900 mb-3 flex items-center">
                <CheckCircle2 className="text-emerald-500 mr-2" size={20} />
                {t('pros')}
              </h3>
              <ul className="space-y-2">
                {result.pros.map((pro, idx) => (
                  <li key={idx} className="flex items-start">
                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 mt-2 mr-3 flex-shrink-0"></span>
                    <span className="text-gray-700 leading-snug">{pro}</span>
                  </li>
                ))}
              </ul>
            </div>
            
            <div className="h-px bg-gray-100"></div>

            <div>
              <h3 className="text-lg font-bold text-gray-900 mb-3 flex items-center">
                <XCircle className="text-red-500 mr-2" size={20} />
                {t('cons')}
              </h3>
              <ul className="space-y-2">
                {result.cons.map((con, idx) => (
                  <li key={idx} className="flex items-start">
                    <span className="w-1.5 h-1.5 rounded-full bg-red-500 mt-2 mr-3 flex-shrink-0"></span>
                    <span className="text-gray-700 leading-snug">{con}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </section>

        {/* Raw Ingredients Accordion */}
        <section className="bg-white rounded-3xl shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-gray-100 overflow-hidden">
          <button 
            onClick={() => setIngredientsExpanded(!ingredientsExpanded)}
            className="w-full px-6 py-5 flex items-center justify-between font-bold text-gray-900 active:bg-gray-50 transition-colors"
          >
            <span>{t('rawIngredients')}</span>
            {ingredientsExpanded ? <ChevronUp size={20} className="text-gray-400" /> : <ChevronDown size={20} className="text-gray-400" />}
          </button>
          
          <div className={`px-6 pb-5 text-gray-600 leading-relaxed text-sm transition-all duration-300 ease-in-out ${ingredientsExpanded ? 'block' : 'hidden'}`}>
            {result.extractedIngredients}
          </div>
        </section>
      </main>
    </div>
  );
}
