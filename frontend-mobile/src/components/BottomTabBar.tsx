import React, { useRef } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Home, ScanLine, User, Camera } from 'lucide-react';
import clsx from 'clsx';
import { compressImage } from '../utils/imageUtils';

export default function BottomTabBar() {
  const location = useLocation();
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Hide bottom tab bar on the immersive scanner view
  if (location.pathname === '/scan') {
    return null;
  }

  const triggerFileInput = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      try {
        const compressedBlob = await compressImage(file);
        navigate('/product/photo', { state: { imageBlob: compressedBlob, scanMode: 'Ingredients' } });
      } catch (error) {
        console.error('Failed to compress image:', error);
      }
    }
    // Clear the input so the same file can be selected again if needed
    e.target.value = '';
  };

  return (
    <div className="fixed bottom-0 left-0 w-full h-20 bg-white/70 backdrop-blur-md border-t border-gray-200/50 flex justify-around items-center px-4 z-50 shadow-[0_-4px_20px_rgba(0,0,0,0.05)]">
      <Link to="/" className={clsx("flex flex-col items-center justify-center space-y-1 transition-colors duration-300 w-12", location.pathname === '/' ? "text-[var(--color-primary)]" : "text-gray-400 hover:text-gray-600")}>
        <Home size={24} strokeWidth={location.pathname === '/' ? 2.5 : 2} />
      </Link>

      <Link to="/scan" className="relative -top-5 flex flex-col items-center justify-center w-14 h-14 rounded-full bg-gradient-to-tr from-[var(--color-primary)] to-emerald-400 text-white shadow-lg shadow-emerald-500/40 hover:scale-105 transition-transform duration-300" aria-label="Scan Barcode">
        <ScanLine size={24} strokeWidth={2.5} />
      </Link>

      <button onClick={triggerFileInput} className="relative -top-5 flex flex-col items-center justify-center w-14 h-14 rounded-full bg-gradient-to-tr from-blue-500 to-cyan-400 text-white shadow-lg shadow-blue-500/40 hover:scale-105 transition-transform duration-300" aria-label="Scan Ingredients">
        <Camera size={24} strokeWidth={2.5} />
      </button>

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
