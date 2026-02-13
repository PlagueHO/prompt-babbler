import type { PromptTemplate } from '@/types';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

interface TemplatePickerProps {
  templates: PromptTemplate[];
  selectedId: string;
  onSelect: (id: string) => void;
}

export function TemplatePicker({
  templates,
  selectedId,
  onSelect,
}: TemplatePickerProps) {
  return (
    <Select value={selectedId} onValueChange={onSelect}>
      <SelectTrigger className="w-[280px]">
        <SelectValue placeholder="Select a template" />
      </SelectTrigger>
      <SelectContent>
        {templates.map((t) => (
          <SelectItem key={t.id} value={t.id}>
            {t.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
