import { Sun, Moon, Monitor } from 'lucide-react';
import type { ThemeMode } from '@/types';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

const THEME_OPTIONS: { value: ThemeMode; label: string; icon: typeof Sun }[] = [
  { value: 'light', label: 'Light', icon: Sun },
  { value: 'dark', label: 'Dark', icon: Moon },
  { value: 'system', label: 'System', icon: Monitor },
];

interface ThemeSelectorProps {
  value: ThemeMode;
  onChange: (value: ThemeMode) => void;
}

export function ThemeSelector({ value, onChange }: ThemeSelectorProps) {
  return (
    <div className="space-y-2">
      <label className="text-sm font-medium">Theme</label>
      <Select value={value} onValueChange={(v) => onChange(v as ThemeMode)}>
        <SelectTrigger className="w-[200px]">
          <SelectValue placeholder="System" />
        </SelectTrigger>
        <SelectContent>
          {THEME_OPTIONS.map(({ value: v, label, icon: Icon }) => (
            <SelectItem key={v} value={v}>
              <div className="flex items-center gap-2">
                <Icon className="size-4" />
                {label}
              </div>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <p className="text-xs text-muted-foreground">
        Select &quot;System&quot; to automatically match your browser or OS
        preference.
      </p>
    </div>
  );
}
