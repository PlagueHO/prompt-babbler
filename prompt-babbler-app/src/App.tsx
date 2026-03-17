import { BrowserRouter, Routes, Route } from 'react-router';
import { Toaster } from 'sonner';
import { PageLayout } from '@/components/layout/PageLayout';
import { ErrorBoundary } from '@/components/layout/ErrorBoundary';
import { BrowserCheck } from '@/components/layout/BrowserCheck';
import { ThemeProvider } from '@/components/layout/ThemeProvider';
import { useTheme } from '@/hooks/useTheme';
import { HomePage } from '@/pages/HomePage';
import { RecordPage } from '@/pages/RecordPage';
import { BabblePage } from '@/pages/BabblePage';
import { TemplatesPage } from '@/pages/TemplatesPage';
import { SettingsPage } from '@/pages/SettingsPage';

function ThemedToaster() {
  const { resolvedTheme } = useTheme();
  return <Toaster richColors position="bottom-right" theme={resolvedTheme} />;
}

function App() {
  return (
    <ErrorBoundary>
      <ThemeProvider>
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
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;
