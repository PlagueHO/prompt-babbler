import { useState, useCallback } from 'react';
import type { Babble } from '@/types';
import * as storage from '@/services/local-storage';

export function useBabbles() {
  const [babbles, setBabbles] = useState<Babble[]>(() => storage.getBabbles());

  const refresh = useCallback(() => {
    setBabbles(storage.getBabbles());
  }, []);

  const createBabble = useCallback(
    (babble: Babble): Babble => {
      storage.createBabble(babble);
      refresh();
      return babble;
    },
    [refresh]
  );

  const updateBabble = useCallback(
    (babble: Babble): Babble => {
      storage.updateBabble(babble);
      refresh();
      return babble;
    },
    [refresh]
  );

  const deleteBabble = useCallback(
    (id: string): void => {
      storage.deleteBabble(id);
      refresh();
    },
    [refresh]
  );

  return { babbles, createBabble, updateBabble, deleteBabble };
}
