import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import { msalInstance, isAuthConfigured } from './auth/authConfig'
import { initTelemetry } from './telemetry'
import './index.css'
import App from './App.tsx'

// Initialize OpenTelemetry before React renders — no-ops if OTLP endpoint is absent.
initTelemetry();

function renderApp() {
  const app = isAuthConfigured ? (
    <MsalProvider instance={msalInstance}>
      <App />
    </MsalProvider>
  ) : (
    <App />
  );

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      {app}
    </StrictMode>,
  )
}

if (isAuthConfigured) {
  msalInstance.initialize().then(renderApp)
} else {
  renderApp()
}
