import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { ArrowLeft, CheckCircle2, XCircle } from 'lucide-react';
import { compareProducts } from '../api/scanApi';
import { VLMResponseContract } from '../types';
import clsx from 'clsx';

export default function CompareResultView() {
  const { t, i18n } = useTranslation();
  const location = useLocation();
  const navigate = useNavigate();
  const [results, setResults] = useState<VLMResponseContract[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const blobs = location.state?.blobs as Blob[];
    if (!blobs || blobs.length < 2) {
      navigate('/compare', { replace: true });
      return;
    }

    const controller = new AbortController();
    
    const fetchComparison = async () => {
      try {
        setLoading(true);
        const data = await compareProducts(blobs, i18n.language, controller.signal);
        setResults(data);
      } catch (err: any) {
        if (err.name !== 'CanceledError') {
          console.error(err);
          setError(t('error_comparing') || 'Failed to compare products.');
        }
      } finally {
        setLoading(false);
      }
    };

    fetchComparison();

    return () => controller.abort();
  }, [location.state, navigate, i18n.language, t]);

  const getGradient = (rating: number) => {
    if (rating >= 8) return 'from-emerald-400 to-emerald-600';
    if (rating >= 5) return 'from-yellow-400 to-yellow-600';
    return 'from-red-400 to-red-600';
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-[var(--color-background)] flex flex-col items-center justify-center p-6">
        <div className="w-16 h-16 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin mb-4"></div>
        <p className="text-gray-600 font-medium animate-pulse">{t('consulting_ai_compare') || 'Consulting AI for Comparison...'}</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-[var(--color-background)] p-6 pt-12">
        <button onClick={() => navigate(-1)} className="mb-6 p-2 rounded-full bg-white shadow-sm border border-gray-100">
          <ArrowLeft size={24} className="text-gray-800" />
        </button>
        <div className="bg-red-50 p-4 rounded-2xl border border-red-100 text-red-600 text-center font-medium">
          {error}
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[var(--color-background)]">
      <div className="sticky top-0 z-10 bg-white/80 backdrop-blur-md border-b border-gray-200/50 p-4 flex items-center">
        <button onClick={() => navigate(-1)} className="p-2 -ml-2 rounded-full hover:bg-gray-100 transition-colors">
          <ArrowLeft size={24} className="text-gray-800" />
        </button>
        <h1 className="text-lg font-bold ml-2 text-gray-900">{t('comparison_results') || 'Comparison'}</h1>
      </div>

      <div className="p-4 overflow-x-auto snap-x snap-mandatory flex gap-4 pb-24">
        {results.map((product, idx) => (
          <div key={idx} className="min-w-[85vw] md:min-w-[300px] snap-center bg-white rounded-3xl shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-gray-100/50 p-5 flex flex-col">
            <h2 className="text-xl font-bold text-gray-900 mb-4 h-14 line-clamp-2">{product.productName}</h2>
            
            <div className={clsx("w-16 h-16 rounded-2xl flex items-center justify-center text-white font-bold text-2xl shadow-lg mb-6 bg-gradient-to-br", getGradient(product.rating))}>
              {product.rating}
            </div>

            <p className="text-gray-600 text-sm mb-6 leading-relaxed line-clamp-4">{product.summary}</p>

            <div className="flex-1 space-y-6">
              <div>
                <h3 className="font-semibold text-emerald-600 flex items-center gap-2 mb-3">
                  <CheckCircle2 size={18} /> {t('pros') || 'Pros'}
                </h3>
                <ul className="space-y-2">
                  {product.pros.map((p, i) => (
                    <li key={i} className="text-sm text-gray-700 flex items-start gap-2">
                      <span className="text-emerald-500 mt-0.5">•</span>
                      <span>{p}</span>
                    </li>
                  ))}
                </ul>
              </div>

              {product.cons.length > 0 && (
                <div>
                  <h3 className="font-semibold text-red-500 flex items-center gap-2 mb-3">
                    <XCircle size={18} /> {t('cons') || 'Cons'}
                  </h3>
                  <ul className="space-y-2">
                    {product.cons.map((c, i) => (
                      <li key={i} className="text-sm text-gray-700 flex items-start gap-2">
                        <span className="text-red-400 mt-0.5">•</span>
                        <span>{c}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
