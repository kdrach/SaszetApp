import { useState, useRef, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Home, History, User, Camera, ScanLine, QrCode, X } from 'lucide-react';
import clsx from 'clsx';
import { useNativeCameraScanner } from '../hooks/useNativeCameraScanner';

export default function BottomTabBar() {
  const location = useLocation();
  const { fileInputRef, triggerCamera, handleFileChange } = useNativeCameraScanner();
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // Close menu if clicked outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent | TouchEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("touchstart", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("touchstart", handleClickOutside);
    };
  }, []);

  // Hide bottom tab bar on the immersive scanner view
  if (location.pathname === '/scan') {
    return null;
  }

  return (
    <div className="fixed bottom-0 left-0 w-full h-20 bg-white/70 backdrop-blur-md border-t border-gray-200/50 flex justify-around items-center px-4 z-50 shadow-[0_-4px_20px_rgba(0,0,0,0.05)]">
      <Link to="/" className={clsx("flex flex-col items-center justify-center space-y-1 transition-colors duration-300 w-12", location.pathname === '/' ? "text-[var(--color-primary)]" : "text-gray-400 hover:text-gray-600")}>
        <Home size={24} strokeWidth={location.pathname === '/' ? 2.5 : 2} />
      </Link>

      <Link to="/history" className={clsx("flex flex-col items-center justify-center space-y-1 transition-colors duration-300 w-12", location.pathname === '/history' ? "text-[var(--color-primary)]" : "text-gray-400 hover:text-gray-600")}>
        <History size={24} strokeWidth={location.pathname === '/history' ? 2.5 : 2} />
      </Link>

      {/* Center FAB */}
      <div className="relative flex flex-col items-center justify-center" ref={menuRef}>
        <button 
          onClick={() => setIsMenuOpen(!isMenuOpen)}
          aria-label="Toggle Scan Menu"
          className="relative -top-5 flex flex-col items-center justify-center w-14 h-14 rounded-full bg-emerald-500 text-white shadow-lg shadow-emerald-500/40 hover:scale-105 transition-transform duration-300 z-50"
        >
          {isMenuOpen ? <X size={24} strokeWidth={2.5} /> : <QrCode size={24} strokeWidth={2.5} />}
        </button>

        {/* Popover Menu */}
        {isMenuOpen && (
          <div className="absolute bottom-12 bg-white rounded-2xl shadow-xl border border-gray-100 flex flex-col p-2 space-y-2 animate-in slide-in-from-bottom-2 fade-in duration-200 z-40 w-48 -translate-x-1/2 left-1/2">
             <Link 
                to="/scan" 
                onClick={() => setIsMenuOpen(false)}
                aria-label="Scan Barcode"
                className="flex items-center space-x-3 px-4 py-3 rounded-xl hover:bg-emerald-50 text-gray-700 hover:text-emerald-600 transition-colors"
             >
                <div className="bg-emerald-100 p-2 rounded-full text-emerald-600">
                  <ScanLine size={20} />
                </div>
                <span className="font-medium text-sm">Zeskanuj kod EAN</span>
             </Link>
             
             <button 
                onClick={() => {
                  triggerCamera();
                  setIsMenuOpen(false);
                }}
                aria-label="Take Photo"
                className="flex items-center space-x-3 px-4 py-3 rounded-xl hover:bg-blue-50 text-gray-700 hover:text-blue-600 transition-colors w-full text-left"
             >
                <div className="bg-blue-100 p-2 rounded-full text-blue-600">
                  <Camera size={20} />
                </div>
                <span className="font-medium text-sm">Zdjęcie etykiety</span>
             </button>
          </div>
        )}
      </div>

      <Link to="/compare" className={clsx("flex flex-col items-center justify-center space-y-1 transition-colors duration-300 w-12", location.pathname.startsWith('/compare') ? "text-[var(--color-primary)]" : "text-gray-400 hover:text-gray-600")} aria-label="Compare Foods">
        <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={location.pathname.startsWith('/compare') ? "2.5" : "2"} strokeLinecap="round" strokeLinejoin="round"><path d="M16 3h5v5"/><path d="M8 3H3v5"/><path d="M12 22v-8"/><path d="m21 3-6 6"/><path d="m3 3 6 6"/><path d="M12 14v-4"/></svg>
      </Link>

      <Link to="/profile" className={clsx("flex flex-col items-center justify-center space-y-1 transition-colors duration-300 w-12", location.pathname === '/profile' ? "text-[var(--color-primary)]" : "text-gray-400 hover:text-gray-600")}>
        <User size={24} strokeWidth={location.pathname === '/profile' ? 2.5 : 2} />
      </Link>
      
      <input
        type="file"
        accept="image/*"
        capture="environment"
        className="hidden"
        ref={fileInputRef}
        onChange={handleFileChange}
        title="Scan Ingredients"
      />
    </div>
  );
}
