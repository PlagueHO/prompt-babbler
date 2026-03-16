import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import { msalInstance, isAuthConfigured } from './auth/authConfig'
import './index.css'
import App from './App.tsx'

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
