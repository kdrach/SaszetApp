import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Loader2 } from 'lucide-react';

export default function LoadingOverlay() {
  const { t } = useTranslation();
  const [tipIndex, setTipIndex] = useState(0);

  const tips = [
    t('loadingReading'),
    t('loadingConsulting'),
    t('loadingMeat')
  ];

  useEffect(() => {
    const interval = setInterval(() => {
      setTipIndex((prev) => (prev + 1) % tips.length);
    }, 2500);
    return () => clearInterval(interval);
  }, [tips.length]);

  return (
    <div className="fixed inset-0 bg-white/90 backdrop-blur-md z-50 flex flex-col items-center justify-center p-6">
      <div className="relative">
        <div className="absolute inset-0 rounded-full blur-xl bg-[var(--color-primary)]/20 animate-pulse"></div>
        <div className="bg-white p-6 rounded-full shadow-xl relative z-10">
          <Loader2 className="animate-spin text-[var(--color-primary)]" size={48} strokeWidth={2.5} />
        </div>
      </div>
      
      <div className="mt-10 h-12 flex items-center justify-center overflow-hidden">
        <p 
          key={tipIndex}
          className="text-lg font-semibold text-gray-800 animate-[slideUp_0.5s_ease-out] text-center"
        >
          {tips[tipIndex]}
        </p>
      </div>
      <style>{`
        @keyframes slideUp {
          from { opacity: 0; transform: translateY(10px); }
          to { opacity: 1; transform: translateY(0); }
        }
      `}</style>
    </div>
  );
}
