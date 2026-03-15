import { useState } from 'react';
import type { PromptTemplate, PromptFormat } from '@/types';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Sparkles, Loader2 } from 'lucide-react';
import { TemplatePicker } from './TemplatePicker';

export interface PromptGenerateOptions {
  template: PromptTemplate;
  promptFormat: PromptFormat;
  allowEmojis: boolean;
}

interface PromptGeneratorProps {
  templates: PromptTemplate[];
  isGenerating: boolean;
  onGenerate: (options: PromptGenerateOptions) => void;
  disabled?: boolean;
}

export function PromptGenerator({
  templates,
  isGenerating,
  onGenerate,
  disabled = false,
}: PromptGeneratorProps) {
  const [selectedId, setSelectedId] = useState(templates[0]?.id ?? '');
  const [promptFormat, setPromptFormat] = useState<PromptFormat>('text');
  const [allowEmojis, setAllowEmojis] = useState(false);

  const handleGenerate = () => {
    const template = templates.find((t) => t.id === selectedId);
    if (template) {
      onGenerate({ template, promptFormat, allowEmojis });
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-3">
        <TemplatePicker
          templates={templates}
          selectedId={selectedId}
          onSelect={setSelectedId}
        />
        <Select value={promptFormat} onValueChange={(v) => setPromptFormat(v as PromptFormat)}>
          <SelectTrigger className="w-[140px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="text">Text</SelectItem>
            <SelectItem value="markdown">Markdown</SelectItem>
          </SelectContent>
        </Select>
        <div className="flex items-center gap-2">
          <Checkbox
            id="allow-emojis"
            checked={allowEmojis}
            onCheckedChange={(checked) => setAllowEmojis(checked === true)}
          />
          <Label htmlFor="allow-emojis" className="text-sm font-normal">
            Allow emojis
          </Label>
        </div>
        <Button
          onClick={handleGenerate}
          disabled={disabled || isGenerating || !selectedId}
        >
          {isGenerating ? (
            <Loader2 className="size-4 animate-spin" />
          ) : (
            <Sparkles className="size-4" />
          )}
          {isGenerating ? 'Generating…' : 'Generate Prompt'}
        </Button>
      </div>
    </div>
  );
}
