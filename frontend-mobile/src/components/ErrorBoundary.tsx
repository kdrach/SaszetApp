import React, { Component, ErrorInfo, ReactNode } from 'react';

interface Props {
  children?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Uncaught error:', error, errorInfo);
  }

  public render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen bg-white flex flex-col items-center justify-center p-6 relative z-[99999]">
          <h1 className="text-2xl font-bold text-red-600 mb-4">Wystąpił krytyczny błąd</h1>
          <p className="text-gray-700 mb-4 text-center">Aplikacja napotkała nieoczekiwany problem i nie mogła kontynuować działania.</p>
          <pre className="bg-gray-100 p-4 rounded text-xs text-left w-full overflow-auto max-h-64 text-red-800 whitespace-pre-wrap">
            {this.state.error?.message || 'Brak komunikatu błędu'}
            {'\n'}
            {this.state.error?.stack}
          </pre>
          <button 
            onClick={() => window.location.reload()} 
            className="mt-6 px-6 py-3 bg-blue-600 text-white rounded-xl font-bold active:scale-95 transition-transform"
          >
            Odśwież aplikację
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}
