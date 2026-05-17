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
      { text: 'User Guide', link: '/user-guide' },
      { text: 'Quickstart (Local)', link: '/quickstart-local' },
      { text: 'Quickstart (Azure)', link: '/quickstart-azure' },
      { text: 'Architecture', link: '/architecture' },
      { text: 'API', link: '/api' },
    ],

    sidebar: [
      {
        text: 'Getting Started',
        items: [
          { text: 'Quick Start (Local)', link: '/quickstart-local' },
          { text: 'Quick Start (Azure)', link: '/quickstart-azure' },
          { text: 'Getting Started', link: '/getting-started' },
        ],
      },
      {
        text: 'User Guide',
        items: [
          { text: 'Overview', link: '/user-guide' },
          { text: 'Home Page', link: '/home-page' },
          { text: 'Create a Babble', link: '/create-a-babble' },
          { text: 'Edit a Babble', link: '/edit-a-babble' },
          { text: 'Templates Page', link: '/templates-page' },
        ],
      },
      {
        text: 'Reference',
        items: [
          { text: 'Architecture', link: '/architecture' },
          { text: 'API Reference', link: '/api' },
          { text: 'Authentication', link: '/authentication' },
          { text: 'CI/CD', link: '/cicd' },
          { text: 'MCP Server', link: '/mcp-server' },
          { text: 'Testing', link: '/testing' },
        ],
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/PlagueHO/prompt-babbler' },
    ],

    footer: {
      message: 'Released under the MIT License.',
    },
  },
})
