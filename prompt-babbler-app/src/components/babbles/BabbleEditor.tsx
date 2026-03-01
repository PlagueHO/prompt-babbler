import { useState } from 'react';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { Check, X } from 'lucide-react';
import type { Babble } from '@/types';

interface BabbleEditorProps {
  babble: Babble;
  onSave: (updated: Babble) => void;
  onCancel: () => void;
}

export function BabbleEditor({ babble, onSave, onCancel }: BabbleEditorProps) {
  const [title, setTitle] = useState(babble.title);
  const [text, setText] = useState(babble.text);

  const handleSave = () => {
    onSave({
      ...babble,
      title: title.trim() || babble.title,
      text,
      updatedAt: new Date().toISOString(),
    });
  };

  return (
    <div className="space-y-4">
      <Input
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        placeholder="Babble title"
      />
      <Textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder="Babble text"
        className="min-h-[200px]"
      />
      <div className="flex gap-2">
        <Button size="sm" onClick={handleSave}>
          <Check className="size-4" />
          Save
        </Button>
        <Button size="sm" variant="outline" onClick={onCancel}>
          <X className="size-4" />
          Cancel
        </Button>
      </div>
    </div>
  );
}
