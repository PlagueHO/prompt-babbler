import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

const LANGUAGES = [
  { value: '', label: 'Auto-detect' },
  { value: 'en', label: 'English' },
  { value: 'es', label: 'Spanish' },
  { value: 'fr', label: 'French' },
  { value: 'de', label: 'German' },
  { value: 'it', label: 'Italian' },
  { value: 'pt', label: 'Portuguese' },
  { value: 'nl', label: 'Dutch' },
  { value: 'ja', label: 'Japanese' },
  { value: 'ko', label: 'Korean' },
  { value: 'zh', label: 'Chinese' },
  { value: 'ru', label: 'Russian' },
  { value: 'ar', label: 'Arabic' },
  { value: 'hi', label: 'Hindi' },
] as const;

interface LanguageSelectorProps {
  value: string;
  onChange: (value: string) => void;
}

export function LanguageSelector({ value, onChange }: LanguageSelectorProps) {
  return (
    <div className="space-y-2">
      <label className="text-sm font-medium">Speech Language</label>
      <Select value={value} onValueChange={onChange}>
        <SelectTrigger className="w-[200px]">
          <SelectValue placeholder="Auto-detect" />
        </SelectTrigger>
        <SelectContent>
          {LANGUAGES.map((lang) => (
            <SelectItem key={lang.value || 'auto'} value={lang.value || 'auto'}>
              {lang.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <p className="text-xs text-muted-foreground">
        Selecting a language can improve transcription accuracy.
      </p>
    </div>
  );
}
