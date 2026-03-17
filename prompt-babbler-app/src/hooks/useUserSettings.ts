import { useState, useCallback, useEffect, useRef } from 'react';
import type { UserProfile, UserSettings, ThemeMode } from '@/types';
import * as api from '@/services/api-client';
import { isAuthConfigured } from '@/auth/authConfig';
import { useAuthToken, useAccountCount } from '@/hooks/useAuthToken';
import {
  getThemeMode,
  setThemeMode,
  getSpeechLanguage,
  setSpeechLanguage,
} from '@/services/local-storage';

export function useUserSettings() {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const getAuthToken = useAuthToken();
  const accountCount = useAccountCount();
  const migrationDone = useRef(false);

  // Stabilize getAuthToken reference to avoid infinite re-render loops
  // when the MSAL provider returns new object references each render.
  const getAuthTokenRef = useRef(getAuthToken);
  getAuthTokenRef.current = getAuthToken;

  const fetchProfile = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const authToken = await getAuthTokenRef.current();

      if (isAuthConfigured && !authToken) {
        setProfile(null);
        return;
      }

      const data = await api.getUserProfile(authToken);

      // One-time migration: push localStorage settings to API if user profile has defaults
      // and localStorage has custom values.
      if (!migrationDone.current) {
        migrationDone.current = true;
        const localTheme = getThemeMode() as ThemeMode;
        const localLang = getSpeechLanguage();
        const hasLocalCustom =
          (localTheme && localTheme !== 'system') || localLang !== '';
        const hasDefaultSettings =
          data.settings.theme === 'system' && data.settings.speechLanguage === '';

        if (hasLocalCustom && hasDefaultSettings) {
          const migrated = await api.updateUserSettings(
            {
              theme: localTheme || 'system',
              speechLanguage: localLang,
            },
            authToken,
          );
          setProfile(migrated);
          return;
        }
      }

      // Sync API settings back to localStorage for instant load on next visit
      setThemeMode(data.settings.theme);
      setSpeechLanguage(data.settings.speechLanguage);

      setProfile(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load user settings');
      // Fall back to localStorage values
      setProfile({
        id: '',
        displayName: null,
        email: null,
        settings: {
          theme: (getThemeMode() as ThemeMode) || 'system',
          speechLanguage: getSpeechLanguage(),
        },
        createdAt: '',
        updatedAt: '',
      });
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isAuthConfigured && accountCount === 0) {
      // Not signed in yet — use localStorage defaults
      setProfile({
        id: '',
        displayName: null,
        email: null,
        settings: {
          theme: (getThemeMode() as ThemeMode) || 'system',
          speechLanguage: getSpeechLanguage(),
        },
        createdAt: '',
        updatedAt: '',
      });
      setLoading(false);
    } else {
      void fetchProfile();
    }
  }, [fetchProfile, accountCount]);

  const updateSettings = useCallback(
    async (partial: Partial<UserSettings>): Promise<void> => {
      const current = profile?.settings ?? {
        theme: 'system' as ThemeMode,
        speechLanguage: '',
      };
      const merged = { ...current, ...partial };

      // Optimistic update: apply to localStorage immediately for instant UI
      setThemeMode(merged.theme);
      setSpeechLanguage(merged.speechLanguage);
      setProfile((prev) =>
        prev ? { ...prev, settings: merged } : prev,
      );

      try {
        const authToken = await getAuthTokenRef.current();
        const updated = await api.updateUserSettings(merged, authToken);
        setProfile(updated);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to save settings');
      }
    },
    [profile],
  );

  const settings: UserSettings = profile?.settings ?? {
    theme: (getThemeMode() as ThemeMode) || 'system',
    speechLanguage: getSpeechLanguage(),
  };

  return {
    profile,
    settings,
    loading,
    error,
    updateSettings,
    refresh: fetchProfile,
  };
}
