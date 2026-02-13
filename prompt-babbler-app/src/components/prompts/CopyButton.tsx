import { useState } from 'react';
import { Copy, Check } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';

interface CopyButtonProps {
  text: string;
  disabled?: boolean;
}

export function CopyButton({ text, disabled = false }: CopyButtonProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(text);
      setCopied(true);
      toast.success('Copied to clipboard');
      setTimeout(() => setCopied(false), 2000);
    } catch {
      toast.error('Failed to copy to clipboard');
    }
  };

  return (
    <Button
      size="sm"
      variant="outline"
      disabled={disabled || !text}
      onClick={() => void handleCopy()}
    >
      {copied ? (
        <Check className="size-4" />
      ) : (
        <Copy className="size-4" />
      )}
      {copied ? 'Copied' : 'Copy'}
    </Button>
  );
}
