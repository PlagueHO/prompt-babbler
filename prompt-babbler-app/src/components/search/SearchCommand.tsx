import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router';
import {
  CommandDialog,
  CommandInput,
  CommandList,
  CommandItem,
  CommandEmpty,
  CommandGroup,
} from '@/components/ui/command';
import { useSemanticSearch } from '@/hooks/useSemanticSearch';
import { Badge } from '@/components/ui/badge';
import { Loader2 } from 'lucide-react';

export function SearchCommand() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const { results, loading } = useSemanticSearch(query);
  const navigate = useNavigate();

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        setOpen((prev) => !prev);
      }
    };
    const openHandler = () => setOpen(true);
    document.addEventListener('keydown', down);
    document.addEventListener('babble:open-search', openHandler);
    return () => {
      document.removeEventListener('keydown', down);
      document.removeEventListener('babble:open-search', openHandler);
    };
  }, []);

  const handleSelect = (babbleId: string) => {
    setOpen(false);
    setQuery('');
    navigate(`/babble/${babbleId}`);
  };

  return (
    <CommandDialog open={open} onOpenChange={setOpen} shouldFilter={false}>
      <CommandInput
        placeholder="Search babbles..."
        value={query}
        onValueChange={setQuery}
      />
      <CommandList>
        {loading && (
          <div className="flex items-center justify-center py-6">
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
          </div>
        )}
        <CommandEmpty>
          {query.length < 2 ? 'Type to search...' : 'No results found.'}
        </CommandEmpty>
        {results.length > 0 && (
          <CommandGroup heading="Babbles">
            {results.map((result) => (
              <CommandItem
                key={result.id}
                value={result.id}
                onSelect={() => handleSelect(result.id)}
              >
                <div className="flex flex-col gap-1">
                  <span className="font-medium">{result.title}</span>
                  <span className="text-muted-foreground text-sm line-clamp-2">
                    {result.snippet}
                  </span>
                  {result.tags && result.tags.length > 0 && (
                    <div className="flex gap-1 mt-1">
                      {result.tags.slice(0, 3).map((tag) => (
                        <Badge key={tag} variant="secondary" className="text-xs">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  )}
                </div>
              </CommandItem>
            ))}
          </CommandGroup>
        )}
      </CommandList>
    </CommandDialog>
  );
}
