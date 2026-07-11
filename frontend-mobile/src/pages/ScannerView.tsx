import { useEffect, useState, useRef } from 'react';
import { Html5Qrcode } from 'html5-qrcode';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Flashlight, Camera, Barcode, Image as ImageIcon } from 'lucide-react';
import clsx from 'clsx';
import { compressImage } from '../utils/imageUtils';

export default function ScannerView() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [mode, setMode] = useState<'ean' | 'photo'>('ean');
  const [hasPermission, setHasPermission] = useState<boolean | null>(null);
  const stopPromiseRef = useRef<Promise<void> | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [photoScanMode, setPhotoScanMode] = useState<'Ingredients' | 'General'>('Ingredients');
  const html5QrCodeRef = useRef<Html5Qrcode | null>(null);
  const [torchOn, setTorchOn] = useState(false);

  const isInitializingRef = useRef(false);

  useEffect(() => {
    let html5QrCode: Html5Qrcode;
    let isMounted = true;
    let isStarting = false;

    const startCamera = async () => {
      if (isInitializingRef.current) return;
      if (stopPromiseRef.current) {
        await stopPromiseRef.current;
      }
      try {
        isInitializingRef.current = true;
        const devices = await Html5Qrcode.getCameras();
        if (devices && devices.length) {
          if (!isMounted) return;
          setHasPermission(true);
          html5QrCode = new Html5Qrcode("reader");
          html5QrCodeRef.current = html5QrCode;
          isStarting = true;
          
          if (mode === 'ean') {
            await html5QrCode.start(
              { facingMode: "environment" },
              { fps: 10, qrbox: { width: 250, height: 150 }, aspectRatio: 1.0 },
              (decodedText) => {
                if (html5QrCode.isScanning && !stopPromiseRef.current) {
                  stopPromiseRef.current = html5QrCode.stop().then(() => {
                    stopPromiseRef.current = null;
                    navigate(`/product/${encodeURIComponent(decodedText)}`);
                  }).catch((err) => {
                    stopPromiseRef.current = null;
                    console.error(err);
                  });
                }
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
          isStarting = false;

          if (!isMounted) {
            html5QrCode.stop().catch(console.error);
            return;
          }
        }
      } catch (err) {
        isStarting = false;
        if (!isMounted) return;
        console.error("Camera error:", err);
        setHasPermission(false);
      } finally {
        isInitializingRef.current = false;
      }
    };

    startCamera();

    return () => {
      isMounted = false;
      if (html5QrCodeRef.current && !isStarting && html5QrCodeRef.current.isScanning && !stopPromiseRef.current) {
        stopPromiseRef.current = html5QrCodeRef.current.stop().then(() => {
          stopPromiseRef.current = null;
          html5QrCodeRef.current = null;
        }).catch((err) => {
          stopPromiseRef.current = null;
          console.error(err);
        });
      }
    };
  }, [mode, navigate]);

  const toggleFlashlight = async () => {
    if (html5QrCodeRef.current && html5QrCodeRef.current.isScanning) {
      try {
        await html5QrCodeRef.current.applyVideoConstraints({ advanced: [{ torch: !torchOn } as any] });
        setTorchOn(!torchOn);
      } catch (err) {
        console.error("Torch not supported", err);
      }
    }
  };

  const triggerFileInput = (pmode: 'Ingredients' | 'General') => {
    setPhotoScanMode(pmode);
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const captureFrameFromVideo = async (pmode: 'Ingredients' | 'General') => {
    const video = document.querySelector('#reader video') as HTMLVideoElement;
    if (!video) return;

    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.drawImage(video, 0, 0);
      canvas.toBlob(async (blob) => {
        if (blob) {
          try {
            const file = new File([blob], "capture.jpg", { type: "image/jpeg" });
            const compressedBlob = await compressImage(file);
            navigate('/product/photo', { state: { imageBlob: compressedBlob, scanMode: pmode } });
          } catch (error) {
            console.error('Failed to compress image:', error);
          }
        }
      }, 'image/jpeg', 0.95);
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      try {
        const compressedBlob = await compressImage(file);
        navigate('/product/photo', { state: { imageBlob: compressedBlob, scanMode: photoScanMode } });
      } catch (error) {
        console.error('Failed to compress image:', error);
      }
    }
  };

  return (
    <div className="fixed inset-0 bg-black z-50 flex flex-col">
      <div className="absolute top-0 left-0 right-0 p-6 flex justify-between items-center z-10 bg-gradient-to-b from-black/60 to-transparent pt-12">
        <button onClick={() => navigate(-1)} className="text-white p-2 rounded-full bg-black/40 backdrop-blur-md active:scale-95 transition-transform">
          <ArrowLeft size={24} />
        </button>
        <button onClick={toggleFlashlight} className={clsx("p-2 rounded-full backdrop-blur-md active:scale-95 transition-transform", torchOn ? "bg-yellow-400 text-black" : "bg-black/40 text-white")}>
          <Flashlight size={24} />
        </button>
      </div>

      <div className="flex-1 relative overflow-hidden">
        {hasPermission === false && (
          <div className="absolute inset-0 flex items-center justify-center text-white text-center px-6 z-20 bg-black">
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

        {hasPermission !== false && (
          <div className="absolute top-1/4 left-0 w-full text-center pointer-events-none mt-20 z-20">
            <span className="bg-black/60 backdrop-blur-md text-white px-4 py-2 rounded-full text-sm font-medium tracking-wide shadow-lg">
              {mode === 'ean' ? t('placeBarcode') : t('takePhoto')}
            </span>
          </div>
        )}
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
          <div className="flex flex-col space-y-4 px-6 w-full max-w-sm mx-auto">
            <div className="flex space-x-3">
              <button 
                onClick={() => captureFrameFromVideo('Ingredients')}
                className="flex-1 bg-[var(--color-primary)] text-white py-4 rounded-xl font-semibold shadow-lg active:scale-95 transition-transform"
              >
                {t('scanIngredients')}
              </button>
              <button 
                onClick={() => triggerFileInput('Ingredients')}
                className="bg-[var(--color-primary)]/20 text-[var(--color-primary)] p-4 rounded-xl shadow-lg active:scale-95 transition-transform flex items-center justify-center"
              >
                <ImageIcon size={24} />
              </button>
            </div>
            
            <div className="flex space-x-3">
              <button 
                onClick={() => captureFrameFromVideo('General')}
                className="flex-1 bg-gray-800 text-white py-4 rounded-xl font-semibold shadow-lg border border-gray-700 active:scale-95 transition-transform"
              >
                {t('scanFrontPackaging')}
              </button>
              <button 
                onClick={() => triggerFileInput('General')}
                className="bg-gray-800 text-white p-4 rounded-xl shadow-lg border border-gray-700 active:scale-95 transition-transform flex items-center justify-center"
              >
                <ImageIcon size={24} />
              </button>
            </div>
            <input
              type="file"
              accept="image/*"
              capture="environment"
              className="hidden"
              ref={fileInputRef}
              onChange={handleFileChange}
            />
          </div>
        )}
      </div>
    </div>
  );
}
