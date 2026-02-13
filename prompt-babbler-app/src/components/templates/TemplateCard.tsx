import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import type { PromptTemplate } from '@/types';

interface TemplateCardProps {
  template: PromptTemplate;
  onClick: () => void;
}

export function TemplateCard({ template, onClick }: TemplateCardProps) {
  return (
    <Card
      className="cursor-pointer transition-colors hover:bg-accent/50"
      onClick={onClick}
    >
      <CardHeader>
        <div className="flex items-center gap-2">
          <CardTitle className="text-base">{template.name}</CardTitle>
          {template.isBuiltIn && (
            <Badge variant="secondary">Built-in</Badge>
          )}
        </div>
        <CardDescription>{template.description}</CardDescription>
      </CardHeader>
      <CardContent>
        <p className="line-clamp-2 text-xs text-muted-foreground font-mono">
          {template.systemPrompt}
        </p>
      </CardContent>
    </Card>
  );
}
