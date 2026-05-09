import { useEffect } from 'react';

const appName = 'Prompt Babbler';

function buildTitle(pageTitle?: string): string {
  if (!pageTitle?.trim()) {
    return appName;
  }

  return `${pageTitle.trim()} | ${appName}`;
}

export function usePageTitle(pageTitle?: string) {
  useEffect(() => {
    document.title = buildTitle(pageTitle);
  }, [pageTitle]);
}
