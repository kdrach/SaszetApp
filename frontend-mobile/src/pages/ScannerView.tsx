import { useEffect, useState, useRef } from 'react';
import { Html5Qrcode } from 'html5-qrcode';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Flashlight } from 'lucide-react';
import clsx from 'clsx';

export default function ScannerView() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [hasPermission, setHasPermission] = useState<boolean | null>(null);
  const stopPromiseRef = useRef<Promise<void> | null>(null);
  const startPromiseRef = useRef<Promise<void> | null>(null);
  const html5QrCodeRef = useRef<Html5Qrcode | null>(null);
  const [torchOn, setTorchOn] = useState(false);

  useEffect(() => {
    let localHtml5QrCode: Html5Qrcode;
    let isMounted = true;
    let isStarting = false;

    const startCamera = async () => {
      while (startPromiseRef.current || stopPromiseRef.current) {
        if (startPromiseRef.current) await startPromiseRef.current;
        if (stopPromiseRef.current) await stopPromiseRef.current;
      }
      if (!isMounted) return;

      const p = (async () => {
        try {
          const devices = await Html5Qrcode.getCameras();
          if (devices && devices.length) {
            if (!isMounted) return;
            setHasPermission(true);
            localHtml5QrCode = new Html5Qrcode("reader");
            html5QrCodeRef.current = localHtml5QrCode;
            isStarting = true;
            
            await localHtml5QrCode.start(
              { facingMode: "environment" },
              { fps: 10, qrbox: { width: 250, height: 150 }, aspectRatio: 1.0 },
              (decodedText) => {
                if (localHtml5QrCode.isScanning && !stopPromiseRef.current) {
                  stopPromiseRef.current = localHtml5QrCode.stop().then(() => {
                    navigate(`/product/${encodeURIComponent(decodedText)}`);
                  }).catch((err) => {
                    console.error(err);
                  }).finally(() => {
                    stopPromiseRef.current = null;
                  });
                }
              },
              () => {}
            );
            
            isStarting = false;

            if (!isMounted) {
              if (localHtml5QrCode.isScanning && !stopPromiseRef.current) {
                stopPromiseRef.current = localHtml5QrCode.stop().catch(console.error).finally(() => { stopPromiseRef.current = null; });
              }
              return;
            }
          }
        } catch (err) {
          isStarting = false;
          if (!isMounted) return;
          console.error("Camera error:", err);
          setHasPermission(false);
        }
      })();
      
      startPromiseRef.current = p;
      await p;
      if (startPromiseRef.current === p) {
        startPromiseRef.current = null;
      }
    };

    startCamera();

    return () => {
      isMounted = false;
      if (localHtml5QrCode && !isStarting && localHtml5QrCode.isScanning && !stopPromiseRef.current) {
        stopPromiseRef.current = localHtml5QrCode.stop().then(() => {
          html5QrCodeRef.current = null;
        }).catch((err) => {
          console.error(err);
        }).finally(() => {
          stopPromiseRef.current = null;
        });
      }
    };
  }, [navigate]);

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
        <div className="absolute inset-0 pointer-events-none border-[100vh] border-black/50" style={{ borderWidth: 'calc(50vh - 75px) calc(50vw - 125px)' }}>
          <div className="w-full h-full border-2 border-[var(--color-primary)] rounded-lg shadow-[0_0_0_9999px_rgba(0,0,0,0.5)] box-content relative">
            <div className="absolute top-0 left-0 w-4 h-4 border-t-4 border-l-4 border-[var(--color-primary)] -mt-1 -ml-1"></div>
            <div className="absolute top-0 right-0 w-4 h-4 border-t-4 border-r-4 border-[var(--color-primary)] -mt-1 -mr-1"></div>
            <div className="absolute bottom-0 left-0 w-4 h-4 border-b-4 border-l-4 border-[var(--color-primary)] -mb-1 -ml-1"></div>
            <div className="absolute bottom-0 right-0 w-4 h-4 border-b-4 border-r-4 border-[var(--color-primary)] -mb-1 -mr-1"></div>
          </div>
        </div>

        {hasPermission !== false && (
          <div className="absolute top-1/4 left-0 w-full text-center pointer-events-none mt-20 z-20">
            <span className="bg-black/60 backdrop-blur-md text-white px-4 py-2 rounded-full text-sm font-medium tracking-wide shadow-lg">
              {t('placeBarcode')}
            </span>
          </div>
        )}
      </div>
    </div>
  );
}
