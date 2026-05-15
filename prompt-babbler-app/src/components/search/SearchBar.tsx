import { useRef, useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router';
import { Search, Loader2 } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { useSearch } from '@/hooks/useSearch';

const isMac = /Mac/i.test(navigator.platform);

export function SearchBar() {
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const { results, loading } = useSearch(query);
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  const handleClear = useCallback(() => {
    setQuery('');
    setOpen(false);
    inputRef.current?.blur();
  }, []);

  const handleSelect = useCallback(
    (babbleId: string) => {
      handleClear();
      navigate(`/babble/${babbleId}`);
    },
    [navigate, handleClear]
  );

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        inputRef.current?.focus();
        setOpen(true);
      }
      if (e.key === 'Escape') {
        handleClear();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [handleClear]);

  const handleContainerBlur = (e: React.FocusEvent) => {
    if (!containerRef.current?.contains(e.relatedTarget as Node)) {
      setOpen(false);
    }
  };

  const showDropdown = open && query.trim().length >= 2;

  return (
    <div
      ref={containerRef}
      className="relative w-9 xl:w-60"
      onBlur={handleContainerBlur}
    >
      <div className="relative">
        <Search className="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
        <Input
          ref={inputRef}
          placeholder="Search babbles..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onFocus={() => setOpen(true)}
          className="h-9 w-full pl-8"
          aria-label="Search babbles"
          aria-expanded={showDropdown}
          aria-haspopup="listbox"
        />
        {!query && (
          <kbd className="pointer-events-none absolute right-1.5 top-2 hidden h-5 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-xs font-medium xl:flex">
            {isMac ? (
              <>
                <span className="text-xs">⌘</span>K
              </>
            ) : (
              <>Ctrl K</>
            )}
          </kbd>
        )}
      </div>
      {showDropdown && (
        <div
          role="listbox"
          className="absolute right-0 top-full z-50 mt-1 w-72 rounded-md border bg-popover text-popover-foreground shadow-md"
        >
          {loading && (
            <div className="flex items-center justify-center py-6">
              <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
            </div>
          )}
          {!loading && results.length === 0 && (
            <p className="py-6 text-center text-sm text-muted-foreground">No results found.</p>
          )}
          {!loading && results.length > 0 && (
            <ul>
              {results.map((result) => (
                <li key={result.id} role="option">
                  <button
                    type="button"
                    className="w-full cursor-pointer rounded-md px-3 py-2 text-left hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground focus:outline-none"
                    onMouseDown={(e) => e.preventDefault()}
                    onClick={() => handleSelect(result.id)}
                  >
                    <div className="flex flex-col gap-1">
                      <span className="text-sm font-medium">{result.title}</span>
                      {result.snippet && (
                        <span className="line-clamp-2 text-xs text-muted-foreground">
                          {result.snippet}
                        </span>
                      )}
                      {result.tags && result.tags.length > 0 && (
                        <div className="mt-1 flex gap-1">
                          {result.tags.slice(0, 3).map((tag) => (
                            <Badge key={tag} variant="secondary" className="text-xs">
                              {tag}
                            </Badge>
                          ))}
                        </div>
                      )}
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
