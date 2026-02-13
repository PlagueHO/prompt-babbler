import { useState } from 'react';
import type { PromptTemplate } from '@/types';
import { Button } from '@/components/ui/button';
import { Sparkles } from 'lucide-react';
import { TemplatePicker } from './TemplatePicker';

interface PromptGeneratorProps {
  templates: PromptTemplate[];
  isGenerating: boolean;
  onGenerate: (template: PromptTemplate) => void;
  disabled?: boolean;
}

export function PromptGenerator({
  templates,
  isGenerating,
  onGenerate,
  disabled = false,
}: PromptGeneratorProps) {
  const [selectedId, setSelectedId] = useState(templates[0]?.id ?? '');

  const handleGenerate = () => {
    const template = templates.find((t) => t.id === selectedId);
    if (template) {
      onGenerate(template);
    }
  };

  return (
    <div className="flex flex-wrap items-center gap-3">
      <TemplatePicker
        templates={templates}
        selectedId={selectedId}
        onSelect={setSelectedId}
      />
      <Button
        onClick={handleGenerate}
        disabled={disabled || isGenerating || !selectedId}
      >
        <Sparkles className="size-4" />
        {isGenerating ? 'Generating…' : 'Generate Prompt'}
      </Button>
    </div>
  );
}
