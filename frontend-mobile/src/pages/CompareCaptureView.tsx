import { useState, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Camera, X, Check } from 'lucide-react';
import { compressImage } from '../utils/imageUtils';

export default function CompareCaptureView() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [images, setImages] = useState<{ url: string; blob: Blob }[]>([]);

  const handleCaptureClick = () => {
    if (images.length >= 5) return;
    fileInputRef.current?.click();
  };

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    
    // reset input
    if (fileInputRef.current) fileInputRef.current.value = '';

    try {
      const compressedBlob = await compressImage(file, 1200);
      const url = URL.createObjectURL(compressedBlob);
      setImages(prev => [...prev, { url, blob: compressedBlob }]);
    } catch (err) {
      console.error('Error compressing image:', err);
    }
  };

  const removeImage = (index: number) => {
    setImages(prev => {
      const newImages = [...prev];
      URL.revokeObjectURL(newImages[index].url);
      newImages.splice(index, 1);
      return newImages;
    });
  };

  const handleCompare = () => {
    if (images.length < 2) return;
    // Pass blobs to result view via state
    navigate('/compare-result', { state: { blobs: images.map(i => i.blob) } });
  };

  return (
    <div className="p-6 pt-12 min-h-screen bg-[var(--color-background)]">
      <h1 className="text-3xl font-bold text-gray-900 mb-2">{t('compare_title') || 'Compare Foods'}</h1>
      <p className="text-gray-500 mb-6">{t('compare_subtitle') || 'Take 2 to 5 photos to compare.'}</p>

      <div className="grid grid-cols-2 gap-4 mb-8">
        {images.map((img, idx) => (
          <div key={idx} className="relative rounded-2xl overflow-hidden aspect-square shadow-sm border border-gray-200">
            <img src={img.url} alt={`Capture ${idx}`} className="w-full h-full object-cover" />
            <button 
              onClick={() => removeImage(idx)}
              className="absolute top-2 right-2 p-1.5 bg-black/50 text-white rounded-full backdrop-blur-sm"
            >
              <X size={16} />
            </button>
          </div>
        ))}
        {images.length < 5 && (
          <button 
            onClick={handleCaptureClick}
            className="flex flex-col items-center justify-center rounded-2xl aspect-square border-2 border-dashed border-gray-300 bg-gray-50 text-gray-400 hover:text-emerald-500 hover:border-emerald-500 hover:bg-emerald-50 transition-colors"
          >
            <Camera size={32} className="mb-2" />
            <span className="text-sm font-medium">{t('add_photo') || 'Add Photo'}</span>
          </button>
        )}
      </div>

      <input
        type="file"
        accept="image/*"
        capture="environment"
        className="hidden"
        ref={fileInputRef}
        onChange={handleFileChange}
      />

      <div className="fixed bottom-24 left-0 w-full px-6 flex justify-center">
        <button
          onClick={handleCompare}
          disabled={images.length < 2}
          className="w-full max-w-sm py-4 rounded-2xl font-bold text-white shadow-lg transition-all flex items-center justify-center gap-2 
            bg-gradient-to-r from-emerald-500 to-teal-500 hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50 disabled:grayscale disabled:hover:scale-100"
        >
          <Check size={20} />
          {t('compare_action') || 'Compare Now'}
        </button>
      </div>
    </div>
  );
}
