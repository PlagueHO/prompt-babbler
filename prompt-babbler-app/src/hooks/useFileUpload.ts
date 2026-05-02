import { useCallback, useRef, useState } from 'react';
import { uploadAudioFile } from '@/services/api-client';
import { useAuthToken } from '@/hooks/useAuthToken';
import type { Babble } from '@/types';

export function useFileUpload() {
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const getAuthToken = useAuthToken();
  const getAuthTokenRef = useRef(getAuthToken);
  getAuthTokenRef.current = getAuthToken;

  const upload = useCallback(async (file: File, title?: string): Promise<Babble> => {
    setIsUploading(true);
    setError(null);
    try {
      const authToken = await getAuthTokenRef.current();
      const babble = await uploadAudioFile(file, title, authToken);
      return babble;
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Upload failed';
      setError(msg);
      throw err;
    } finally {
      setIsUploading(false);
    }
  }, []);

  return { upload, isUploading, error };
}
