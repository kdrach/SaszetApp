import { useEffect, useState } from 'react';
import { Html5Qrcode } from 'html5-qrcode';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Flashlight, Camera, Barcode } from 'lucide-react';
import clsx from 'clsx';

export default function ScannerView() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [mode, setMode] = useState<'ean' | 'photo'>('ean');
  const [hasPermission, setHasPermission] = useState<boolean | null>(null);

  useEffect(() => {
    let html5QrCode: Html5Qrcode;

    const startCamera = async () => {
      try {
        const devices = await Html5Qrcode.getCameras();
        if (devices && devices.length) {
          setHasPermission(true);
          html5QrCode = new Html5Qrcode("reader");
          
          if (mode === 'ean') {
            await html5QrCode.start(
              { facingMode: "environment" },
              { fps: 10, qrbox: { width: 250, height: 150 }, aspectRatio: 1.0 },
              (decodedText) => {
                html5QrCode.stop();
                navigate(`/product/${decodedText}`);
              },
              () => {}
            );
          } else {
            await html5QrCode.start(
              { facingMode: "environment" },
              { fps: 10, aspectRatio: 1.0 },
              () => {},
              () => {}
            );
          }
        }
      } catch (err) {
        console.error("Camera error:", err);
        setHasPermission(false);
      }
    };

    startCamera();

    return () => {
      if (html5QrCode && html5QrCode.isScanning) {
        html5QrCode.stop().catch(console.error);
      }
    };
  }, [mode, navigate]);

  const handleCapturePhoto = () => {
    // In a real app we'd capture the frame. For this mock, we'll simulate a scan.
    navigate(`/product/simulated_photo`);
  };

  return (
    <div className="fixed inset-0 bg-black z-50 flex flex-col">
      <div className="absolute top-0 left-0 right-0 p-6 flex justify-between items-center z-10 bg-gradient-to-b from-black/60 to-transparent pt-12">
        <button onClick={() => navigate(-1)} className="text-white p-2 rounded-full bg-black/40 backdrop-blur-md active:scale-95 transition-transform">
          <ArrowLeft size={24} />
        </button>
        <button className="text-white p-2 rounded-full bg-black/40 backdrop-blur-md active:scale-95 transition-transform">
          <Flashlight size={24} />
        </button>
      </div>

      <div className="flex-1 relative overflow-hidden">
        {hasPermission === false && (
          <div className="absolute inset-0 flex items-center justify-center text-white text-center px-6">
            <p className="bg-red-500/80 px-4 py-3 rounded-xl">{t('cameraPermission')}</p>
          </div>
        )}
        <div id="reader" className="w-full h-full object-cover"></div>
        
        {/* Overlay Cutout for EAN */}
        {mode === 'ean' && (
          <div className="absolute inset-0 pointer-events-none border-[100vh] border-black/50" style={{ borderWidth: 'calc(50vh - 75px) calc(50vw - 125px)' }}>
            <div className="w-full h-full border-2 border-[var(--color-primary)] rounded-lg shadow-[0_0_0_9999px_rgba(0,0,0,0.5)] box-content relative">
              <div className="absolute top-0 left-0 w-4 h-4 border-t-4 border-l-4 border-[var(--color-primary)] -mt-1 -ml-1"></div>
              <div className="absolute top-0 right-0 w-4 h-4 border-t-4 border-r-4 border-[var(--color-primary)] -mt-1 -mr-1"></div>
              <div className="absolute bottom-0 left-0 w-4 h-4 border-b-4 border-l-4 border-[var(--color-primary)] -mb-1 -ml-1"></div>
              <div className="absolute bottom-0 right-0 w-4 h-4 border-b-4 border-r-4 border-[var(--color-primary)] -mb-1 -mr-1"></div>
            </div>
          </div>
        )}

        <div className="absolute top-1/4 left-0 w-full text-center pointer-events-none mt-20">
          <span className="bg-black/60 backdrop-blur-md text-white px-4 py-2 rounded-full text-sm font-medium tracking-wide shadow-lg">
            {mode === 'ean' ? t('placeBarcode') : t('takePhoto')}
          </span>
        </div>
      </div>

      <div className="bg-black/80 backdrop-blur-xl rounded-t-[2.5rem] p-8 pb-12 shadow-[0_-10px_40px_rgba(0,0,0,0.3)]">
        <div className="flex justify-center space-x-6 mb-8">
          <button 
            onClick={() => setMode('ean')}
            className={clsx("flex flex-col items-center space-y-2 transition-colors", mode === 'ean' ? "text-[var(--color-primary)]" : "text-gray-400")}
          >
            <div className={clsx("p-4 rounded-full transition-colors", mode === 'ean' ? "bg-[var(--color-primary)]/20" : "bg-gray-800")}>
              <Barcode size={28} />
            </div>
            <span className="text-xs font-semibold uppercase tracking-wider">{t('modeEan')}</span>
          </button>
          
          <button 
            onClick={() => setMode('photo')}
            className={clsx("flex flex-col items-center space-y-2 transition-colors", mode === 'photo' ? "text-[var(--color-primary)]" : "text-gray-400")}
          >
            <div className={clsx("p-4 rounded-full transition-colors", mode === 'photo' ? "bg-[var(--color-primary)]/20" : "bg-gray-800")}>
              <Camera size={28} />
            </div>
            <span className="text-xs font-semibold uppercase tracking-wider">{t('modePhoto')}</span>
          </button>
        </div>

        {mode === 'photo' && (
          <div className="flex justify-center">
            <button 
              onClick={handleCapturePhoto}
              className="w-20 h-20 rounded-full border-4 border-white flex items-center justify-center relative active:scale-90 transition-transform"
            >
              <div className="w-16 h-16 bg-white rounded-full"></div>
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
