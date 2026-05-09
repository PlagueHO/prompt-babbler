import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: 'Prompt Babbler',
  description: 'Record voice notes as babbles and generate AI prompts using Azure OpenAI and configurable templates.',
  // Change to '/' if using a custom domain.
  base: '/prompt-babbler/',
  outDir: 'dist',
  appearance: 'auto',
  head: [
    ['link', { rel: 'icon', type: 'image/svg+xml', href: '/prompt-babbler/favicon.svg' }],
  ],
  themeConfig: {
    nav: [
      { text: 'User Guide', link: '/USER-GUIDE' },
      { text: 'Quickstart (Local)', link: '/QUICKSTART-LOCAL' },
      { text: 'Quickstart (Azure)', link: '/QUICKSTART-AZURE' },
      { text: 'Architecture', link: '/ARCHITECTURE' },
      { text: 'API', link: '/API' },
    ],
    sidebar: [
      {
        text: 'Getting Started',
        items: [
          { text: 'Quick Start (Local)', link: '/QUICKSTART-LOCAL' },
          { text: 'Quick Start (Azure)', link: '/QUICKSTART-AZURE' },
          { text: 'Getting Started', link: '/GETTING-STARTED' },
        ],
      },
      {
        text: 'User Guide',
        items: [
          { text: 'Overview', link: '/USER-GUIDE' },
          { text: 'Home Page', link: '/HOME-PAGE' },
          { text: 'Create a Babble', link: '/CREATE-A-BABBLE' },
          { text: 'Edit a Babble', link: '/EDIT-A-BABBLE' },
          { text: 'Templates Page', link: '/TEMPLATES-PAGE' },
        ],
      },
      {
        text: 'Reference',
        items: [
          { text: 'Architecture', link: '/ARCHITECTURE' },
          { text: 'API Reference', link: '/API' },
          { text: 'Authentication', link: '/AUTHENTICATION' },
          { text: 'CI/CD', link: '/CICD' },
          { text: 'MCP Server', link: '/MCP-SERVER' },
        ],
      },
      {
        text: 'Research',
        items: [
          { text: 'Entra ID Auth Research', link: '/research/ENTRA-ID-AUTH-RESEARCH' },
        ],
      },
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/PlagueHO/prompt-babbler' },
    ],
  },
})
