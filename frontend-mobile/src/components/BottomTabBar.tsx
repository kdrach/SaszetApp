import { Link, useLocation } from 'react-router-dom';
import { Home, ScanLine, User } from 'lucide-react';
import clsx from 'clsx';

export default function BottomTabBar() {
  const location = useLocation();

  // Hide bottom tab bar on the immersive scanner view
  if (location.pathname === '/scan') {
    return null;
  }

  return (
    <div className="fixed bottom-0 left-0 w-full h-20 bg-white/70 backdrop-blur-md border-t border-gray-200/50 flex justify-around items-center px-6 z-50 shadow-[0_-4px_20px_rgba(0,0,0,0.05)]">
      <Link to="/" className={clsx("flex flex-col items-center justify-center space-y-1 transition-colors duration-300", location.pathname === '/' ? "text-[var(--color-primary)]" : "text-gray-400 hover:text-gray-600")}>
        <Home size={24} strokeWidth={location.pathname === '/' ? 2.5 : 2} />
      </Link>

      <Link to="/scan" className="relative -top-5 flex flex-col items-center justify-center w-16 h-16 rounded-full bg-gradient-to-tr from-[var(--color-primary)] to-emerald-400 text-white shadow-lg shadow-emerald-500/40 hover:scale-105 transition-transform duration-300">
        <ScanLine size={28} strokeWidth={2.5} />
      </Link>

      <Link to="/profile" className={clsx("flex flex-col items-center justify-center space-y-1 transition-colors duration-300", location.pathname === '/profile' ? "text-[var(--color-primary)]" : "text-gray-400 hover:text-gray-600")}>
        <User size={24} strokeWidth={location.pathname === '/profile' ? 2.5 : 2} />
      </Link>
    </div>
  );
}
