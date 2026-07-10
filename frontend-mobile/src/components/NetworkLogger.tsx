import React, { useEffect, useState } from 'react';
import { useLogStore } from '../store/useLogStore';

export default function NetworkLogger() {
  const [isOpen, setIsOpen] = useState(false);
  const logs = useLogStore((state) => state.logs);
  const clearLogs = useLogStore((state) => state.clearLogs);

  useEffect(() => {
    let lastShake = 0;
    const handleMotion = (event: DeviceMotionEvent) => {
      const acc = event.accelerationIncludingGravity;
      if (!acc) return;
      const x = acc.x || 0;
      const y = acc.y || 0;
      const z = acc.z || 0;
      const acceleration = Math.sqrt(x * x + y * y + z * z);
      if (acceleration > 20) { // Shake detected
        const now = Date.now();
        if (now - lastShake > 1000) {
          lastShake = now;
          setIsOpen((prev) => !prev);
        }
      }
    };

    window.addEventListener('devicemotion', handleMotion);
    return () => window.removeEventListener('devicemotion', handleMotion);
  }, []);

  // Alternative way: hidden tap button
  const [tapCount, setTapCount] = useState(0);
  useEffect(() => {
    if (tapCount >= 5) {
      setIsOpen(true);
      setTapCount(0);
    }
    const timeout = setTimeout(() => setTapCount(0), 1000);
    return () => clearTimeout(timeout);
  }, [tapCount]);

  if (!isOpen) {
    return (
      <div 
        className="fixed top-0 left-1/2 -translate-x-1/2 w-32 h-16 z-[9999]" 
        onClick={() => setTapCount((c) => c + 1)}
      ></div>
    );
  }

  return (
    <div className="fixed inset-0 bg-white z-[99999] flex flex-col shadow-2xl overflow-hidden text-left">
      <div className="bg-gray-900 text-white p-4 flex justify-between items-center safe-area-pt">
        <h2 className="text-lg font-bold">Network Logger</h2>
        <div className="flex gap-4">
          <button onClick={clearLogs} className="text-red-400 text-sm font-bold">CLEAR</button>
          <button onClick={() => setIsOpen(false)} className="text-white text-sm font-bold bg-gray-700 px-3 py-1 rounded">CLOSE</button>
        </div>
      </div>
      <div className="flex-1 overflow-y-auto bg-gray-100 p-2 space-y-2">
        {logs.length === 0 && <p className="text-center text-gray-500 mt-10">Brak logów</p>}
        {logs.map((log) => (
          <div key={log.id} className="bg-white p-3 rounded shadow-sm text-sm border-l-4" style={{ borderColor: log.status && log.status >= 400 ? 'red' : 'green' }}>
            <div className="flex justify-between font-bold text-gray-800 mb-1">
              <span className="truncate pr-2">{log.method} {log.url}</span>
              <span className={log.status && log.status >= 400 ? 'text-red-500' : 'text-green-500'}>{log.status || 'ERR'}</span>
            </div>
            <div className="text-xs text-gray-500 mb-2">Duration: {log.duration}ms | Time: {new Date(log.timestamp).toLocaleTimeString()}</div>
            {log.error && <div className="text-red-500 font-mono text-xs overflow-x-auto p-1 bg-red-50 mb-2">{log.error}</div>}
            
            {log.requestData && (
              <details className="mb-2">
                <summary className="cursor-pointer text-blue-500 font-semibold mb-1">Request</summary>
                <pre className="bg-gray-50 p-2 rounded text-[10px] overflow-x-auto text-gray-800 font-mono">
                  {JSON.stringify(log.requestData, null, 2)}
                </pre>
              </details>
            )}

            {log.responseData && (
              <details>
                <summary className="cursor-pointer text-blue-500 font-semibold mb-1">Response</summary>
                <pre className="bg-gray-50 p-2 rounded text-[10px] overflow-x-auto text-gray-800 font-mono">
                  {JSON.stringify(log.responseData, null, 2)}
                </pre>
              </details>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
