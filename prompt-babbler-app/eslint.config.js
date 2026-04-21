import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    rules: {
      // Writing ref.current during render is intentional here: it implements the
      // MSAL callback-stabilisation pattern (useRef + assign during render) that
      // prevents infinite re-render loops caused by unstable useMsal() references.
      'react-hooks/refs': 'off',
      // Synchronous setState calls inside effects are used intentionally to reset
      // derived UI state (e.g. clear lists when the user is not authenticated).
      // React 18+ batches these updates, so no cascading-render issue exists.
      'react-hooks/set-state-in-effect': 'off',
    },
  },
  {
    files: ['src/components/ui/**/*.{ts,tsx}'],
    rules: {
      'react-refresh/only-export-components': 'off',
    },
  },
])
