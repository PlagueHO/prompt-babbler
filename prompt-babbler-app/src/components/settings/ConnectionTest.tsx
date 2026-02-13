import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Plug, CheckCircle, XCircle } from 'lucide-react';
import type { TestConnectionResponse } from '@/types';

interface ConnectionTestProps {
  onTest: () => Promise<TestConnectionResponse>;
}

export function ConnectionTest({ onTest }: ConnectionTestProps) {
  const [isTesting, setIsTesting] = useState(false);
  const [result, setResult] = useState<TestConnectionResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleTest = async () => {
    setIsTesting(true);
    setResult(null);
    setError(null);
    try {
      const res = await onTest();
      setResult(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Test failed');
    } finally {
      setIsTesting(false);
    }
  };

  return (
    <div className="space-y-3">
      <Button
        variant="outline"
        onClick={() => void handleTest()}
        disabled={isTesting}
      >
        <Plug className="size-4" />
        {isTesting ? 'Testing…' : 'Test Connection'}
      </Button>
      {result && (
        <div
          className={`flex items-center gap-2 rounded-md p-3 text-sm ${
            result.success
              ? 'bg-green-50 text-green-800 dark:bg-green-900/20 dark:text-green-200'
              : 'bg-red-50 text-red-800 dark:bg-red-900/20 dark:text-red-200'
          }`}
        >
          {result.success ? (
            <CheckCircle className="size-4" />
          ) : (
            <XCircle className="size-4" />
          )}
          <span>{result.message}</span>
          {result.latencyMs !== null && (
            <span className="text-xs opacity-75">({result.latencyMs}ms)</span>
          )}
        </div>
      )}
      {error && (
        <div className="flex items-center gap-2 rounded-md bg-red-50 p-3 text-sm text-red-800 dark:bg-red-900/20 dark:text-red-200">
          <XCircle className="size-4" />
          <span>{error}</span>
        </div>
      )}
    </div>
  );
}
