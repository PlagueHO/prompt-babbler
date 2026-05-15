import { BrowserRouter, Routes, Route } from 'react-router';
import { Toaster } from 'sonner';
import { PageLayout } from '@/components/layout/PageLayout';
import { ErrorBoundary } from '@/components/layout/ErrorBoundary';
import { BrowserCheck } from '@/components/layout/BrowserCheck';
import { ThemeProvider } from '@/components/layout/ThemeProvider';
import { AccessCodeDialog } from '@/components/layout/AccessCodeDialog';
import { useTheme } from '@/hooks/useTheme';
import { useAccessCode } from '@/hooks/useAccessCode';
import { HomePage } from '@/pages/HomePage';
import { RecordPage } from '@/pages/RecordPage';
import { BabblePage } from '@/pages/BabblePage';
import { TemplatesPage } from '@/pages/TemplatesPage';
import { SettingsPage } from '@/pages/SettingsPage';

function ThemedToaster() {
  const { resolvedTheme } = useTheme();
  return <Toaster richColors position="bottom-right" theme={resolvedTheme} />;
}

function AppContent() {
  const { accessCodeRequired, isVerified, isLoading, error, submitCode } = useAccessCode();

  if (isLoading && !accessCodeRequired) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
      </div>
    );
  }

  if (accessCodeRequired && !isVerified) {
    return (
      <AccessCodeDialog
        open={true}
        onSubmit={submitCode}
        isLoading={isLoading}
        error={error}
      />
    );
  }

  return (
    <BrowserRouter>
      <BrowserCheck />
      <PageLayout>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/record" element={<RecordPage />} />
          <Route path="/record/:babbleId" element={<RecordPage />} />
          <Route path="/babble/:id" element={<BabblePage />} />
          <Route path="/templates" element={<TemplatesPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
      </PageLayout>
      <ThemedToaster />
    </BrowserRouter>
  );
}

function App() {
  return (
    <ErrorBoundary>
      <ThemeProvider>
        <AppContent />
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;
