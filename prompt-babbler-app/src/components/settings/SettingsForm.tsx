import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Save } from 'lucide-react';
import type { LlmSettingsView, LlmSettingsSaveRequest } from '@/types';

const settingsSchema = z.object({
  endpoint: z.string().min(1, 'Endpoint is required').url('Must be a valid URL'),
  apiKey: z.string().min(1, 'API key is required'),
  deploymentName: z.string().min(1, 'Deployment name is required'),
  whisperDeploymentName: z.string().min(1, 'Whisper deployment name is required'),
});

type SettingsFormData = z.infer<typeof settingsSchema>;

interface SettingsFormProps {
  settings: LlmSettingsView | null;
  onSave: (data: LlmSettingsSaveRequest) => void;
  isSaving?: boolean;
}

export function SettingsForm({
  settings,
  onSave,
  isSaving = false,
}: SettingsFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<SettingsFormData>({
    resolver: zodResolver(settingsSchema),
    defaultValues: {
      endpoint: settings?.endpoint ?? '',
      apiKey: '',
      deploymentName: settings?.deploymentName ?? '',
      whisperDeploymentName: settings?.whisperDeploymentName ?? '',
    },
  });

  const onSubmit = (data: SettingsFormData) => {
    onSave(data);
  };

  return (
    <form
      onSubmit={(e) => void handleSubmit(onSubmit)(e)}
      className="space-y-4"
    >
      <div className="space-y-2">
        <label htmlFor="endpoint" className="text-sm font-medium">
          Azure OpenAI Endpoint
        </label>
        <Input
          id="endpoint"
          {...register('endpoint')}
          placeholder="https://your-resource.openai.azure.com/"
        />
        {errors.endpoint && (
          <p className="text-sm text-destructive">{errors.endpoint.message}</p>
        )}
      </div>
      <div className="space-y-2">
        <label htmlFor="apiKey" className="text-sm font-medium">
          API Key
        </label>
        <Input
          id="apiKey"
          type="password"
          {...register('apiKey')}
          placeholder={
            settings?.apiKeyHint
              ? `Current: ${settings.apiKeyHint}`
              : 'Enter your API key'
          }
        />
        {errors.apiKey && (
          <p className="text-sm text-destructive">{errors.apiKey.message}</p>
        )}
      </div>
      <div className="space-y-2">
        <label htmlFor="deploymentName" className="text-sm font-medium">
          Chat Deployment Name
        </label>
        <Input
          id="deploymentName"
          {...register('deploymentName')}
          placeholder="gpt-4o"
        />
        {errors.deploymentName && (
          <p className="text-sm text-destructive">
            {errors.deploymentName.message}
          </p>
        )}
      </div>
      <div className="space-y-2">
        <label htmlFor="whisperDeploymentName" className="text-sm font-medium">
          Whisper Deployment Name
        </label>
        <Input
          id="whisperDeploymentName"
          {...register('whisperDeploymentName')}
          placeholder="whisper"
        />
        {errors.whisperDeploymentName && (
          <p className="text-sm text-destructive">
            {errors.whisperDeploymentName.message}
          </p>
        )}
      </div>
      <Button type="submit" disabled={isSaving}>
        <Save className="size-4" />
        {isSaving ? 'Saving…' : 'Save Settings'}
      </Button>
    </form>
  );
}
