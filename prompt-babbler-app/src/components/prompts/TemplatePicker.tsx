import { useMemo, useState } from 'react';
import type { PromptTemplate } from '@/types';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Check, ChevronsUpDown } from 'lucide-react';

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
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [typeFilter, setTypeFilter] = useState<'all' | 'builtIn' | 'custom'>('all');

  const selectedTemplate = useMemo(
    () => templates.find((template) => template.id === selectedId),
    [templates, selectedId]
  );

  const filteredTemplates = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return templates.filter((template) => {
      const matchesType =
        typeFilter === 'all' ||
        (typeFilter === 'builtIn' && template.isBuiltIn) ||
        (typeFilter === 'custom' && !template.isBuiltIn);

      if (!matchesType) {
        return false;
      }

      if (!normalizedQuery) {
        return true;
      }

      const tags = template.tags?.join(' ').toLowerCase() ?? '';
      return `${template.name} ${template.description} ${tags}`
        .toLowerCase()
        .includes(normalizedQuery);
    });
  }, [query, templates, typeFilter]);

  const handleSelect = (id: string) => {
    onSelect(id);
    setOpen(false);
  };

  const templateCountLabel = `${filteredTemplates.length} template${filteredTemplates.length === 1 ? '' : 's'}`;

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button
          type="button"
          variant="outline"
          className="w-[280px] justify-between overflow-hidden"
          disabled={templates.length === 0}
        >
          <span className="truncate">
            {selectedTemplate?.name ?? 'Select a template'}
          </span>
          <ChevronsUpDown className="size-4 shrink-0 opacity-50" />
        </Button>
      </DialogTrigger>
      <DialogContent className="overflow-hidden p-0 sm:max-w-2xl">
        <div className="sr-only">
          <DialogTitle>Choose a prompt template</DialogTitle>
          <DialogDescription>
            Search and filter templates before generating a prompt.
          </DialogDescription>
        </div>
        <Command shouldFilter={false}>
          <CommandInput
            placeholder="Search templates by name, description, or tag..."
            value={query}
            onValueChange={setQuery}
          />
          <div className="flex items-center gap-2 border-b px-3 py-2">
            <Button
              type="button"
              size="sm"
              variant={typeFilter === 'all' ? 'secondary' : 'ghost'}
              onClick={() => setTypeFilter('all')}
            >
              All
            </Button>
            <Button
              type="button"
              size="sm"
              variant={typeFilter === 'builtIn' ? 'secondary' : 'ghost'}
              onClick={() => setTypeFilter('builtIn')}
            >
              Built-in
            </Button>
            <Button
              type="button"
              size="sm"
              variant={typeFilter === 'custom' ? 'secondary' : 'ghost'}
              onClick={() => setTypeFilter('custom')}
            >
              Custom
            </Button>
          </div>
          <CommandList className="max-h-[360px]">
            <CommandEmpty>No templates match your filters.</CommandEmpty>
            <CommandGroup heading={templateCountLabel}>
              {filteredTemplates.map((template) => (
                <CommandItem
                  key={template.id}
                  value={template.id}
                  onSelect={() => handleSelect(template.id)}
                  className="items-start py-3"
                >
                  <div className="flex w-full items-start justify-between gap-3">
                    <div className="min-w-0 space-y-1">
                      <div className="flex items-center gap-2">
                        <span className="truncate font-medium">{template.name}</span>
                        {template.isBuiltIn && (
                          <Badge variant="secondary" className="text-xs">
                            Built-in
                          </Badge>
                        )}
                      </div>
                      <p className="line-clamp-2 text-xs text-muted-foreground">
                        {template.description}
                      </p>
                      {template.tags && template.tags.length > 0 && (
                        <div className="flex flex-wrap gap-1 pt-1">
                          {template.tags.slice(0, 4).map((tag) => (
                            <Badge key={tag} variant="outline" className="text-xs">
                              {tag}
                            </Badge>
                          ))}
                        </div>
                      )}
                    </div>
                    {selectedId === template.id && (
                      <Check className="mt-1 size-4 text-primary" />
                    )}
                  </div>
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </DialogContent>
    </Dialog>
  );
}
