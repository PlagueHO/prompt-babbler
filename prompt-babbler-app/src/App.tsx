import { BrowserRouter, Routes, Route } from 'react-router';
import { Toaster } from 'sonner';
import { PageLayout } from '@/components/layout/PageLayout';
import { ErrorBoundary } from '@/components/layout/ErrorBoundary';
import { BrowserCheck } from '@/components/layout/BrowserCheck';
import { HomePage } from '@/pages/HomePage';
import { RecordPage } from '@/pages/RecordPage';
import { BabblePage } from '@/pages/BabblePage';
import { TemplatesPage } from '@/pages/TemplatesPage';
import { SettingsPage } from '@/pages/SettingsPage';

function App() {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <BrowserCheck />
        <PageLayout>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/record" element={<RecordPage />} />
            <Route path="/babble/:id" element={<BabblePage />} />
            <Route path="/templates" element={<TemplatesPage />} />
            <Route path="/settings" element={<SettingsPage />} />
          </Routes>
        </PageLayout>
        <Toaster richColors position="bottom-right" />
      </BrowserRouter>
    </ErrorBoundary>
  );
}

export default App;
