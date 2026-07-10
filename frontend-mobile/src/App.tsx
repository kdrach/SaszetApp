import { BrowserRouter, Routes, Route } from 'react-router-dom';
import HomeView from './pages/HomeView';
import ScannerView from './pages/ScannerView';
import ResultView from './pages/ResultView';
import BottomTabBar from './components/BottomTabBar';
import NetworkLogger from './components/NetworkLogger';
import { useTranslation } from 'react-i18next';


function App() {
  const { t } = useTranslation();

  return (
    <BrowserRouter>
      <NetworkLogger />
      <div className="flex flex-col min-h-screen bg-[var(--color-background)]">
        <div className="flex-1 overflow-x-hidden overflow-y-auto pb-20">
          <Routes>
            <Route path="/" element={<HomeView />} />
            <Route path="/scan" element={<ScannerView />} />
            <Route path="/product/:id" element={<ResultView />} />
            <Route path="/profile" element={<div className="p-6 text-center text-gray-500 mt-20">{t('profile_placeholder')}</div>} />
          </Routes>
        </div>
        <BottomTabBar />
      </div>
    </BrowserRouter>
  );
}

export default App;
