import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppStore } from '../store/useAppStore';
import { Search, ChevronRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

export default function HomeView() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const recentScans = useAppStore(state => state.recentScans);
  const [query, setQuery] = useState('');

  const toggleLanguage = () => {
    i18n.changeLanguage(i18n.language === 'pl' ? 'en' : 'pl');
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) {
      navigate(`/product/${encodeURIComponent(query)}`);
    }
  };

  return (
    <div className="min-h-screen pt-12 px-4 pb-24">
      <header className="flex justify-between items-center mb-8">
        <h1 className="text-4xl font-extrabold tracking-tight text-gray-900 leading-tight">
          {t('discover')}
        </h1>
        <button 
          onClick={toggleLanguage}
          className="flex items-center space-x-1 bg-white/60 backdrop-blur-sm border border-gray-200 px-3 py-1.5 rounded-full text-sm font-medium shadow-sm active:scale-95 transition-transform"
        >
          <span className={i18n.language === 'pl' ? 'text-[var(--color-primary)]' : 'text-gray-400'}>PL</span>
          <span className="text-gray-300">/</span>
          <span className={i18n.language === 'en' ? 'text-[var(--color-primary)]' : 'text-gray-400'}>EN</span>
        </button>
      </header>

      <form onSubmit={handleSearch} className="relative mb-10 group">
        <div className="absolute inset-y-0 left-4 flex items-center pointer-events-none">
          <Search className="text-gray-400 group-focus-within:text-[var(--color-primary)] transition-colors" size={20} />
        </div>
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={t('searchPlaceholder')}
          className="w-full pl-12 pr-4 py-4 bg-white rounded-2xl shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-gray-100 focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)]/20 focus:border-[var(--color-primary)]/50 transition-all text-lg"
        />
      </form>

      {recentScans.length > 0 && (
        <section>
          <h2 className="text-lg font-semibold text-gray-800 mb-4 px-2">{t('recentScans')}</h2>
          <div className="bg-white rounded-3xl overflow-hidden shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-gray-100">
            <ul className="divide-y divide-gray-100">
              {recentScans.map((scan) => (
                <li key={scan.id}>
                  <button 
                    onClick={() => navigate(`/product/${encodeURIComponent(scan.query)}`)}
                    className="w-full text-left px-5 py-4 flex items-center justify-between hover:bg-gray-50 active:bg-gray-100 transition-colors"
                  >
                    <div className="flex flex-col">
                      <span className="font-medium text-gray-900 truncate max-w-[200px]">{scan.result?.productName || scan.query}</span>
                      <span className="text-sm text-gray-500 mt-0.5">{new Date(scan.timestamp).toLocaleDateString()}</span>
                    </div>
                    <div className="flex items-center space-x-3">
                      {scan.result && (
                        <span className={`px-2 py-1 rounded-md text-xs font-bold ${
                          scan.result.rating >= 8 ? 'bg-emerald-100 text-emerald-700' : 
                          scan.result.rating >= 5 ? 'bg-yellow-100 text-yellow-700' : 'bg-red-100 text-red-700'
                        }`}>
                          {scan.result.rating}/10
                        </span>
                      )}
                      <ChevronRight className="text-gray-300" size={20} />
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        </section>
      )}
    </div>
  );
}
