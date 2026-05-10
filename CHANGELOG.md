# Changelog

All notable changes to the Prompt Babbler project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.4.2] - 2026-05-10

### Fixed

- Improve WebSocket error handling in smoke tests

## [1.4.1] - 2026-05-10

### Fixed

- Improve health check error handling

## [1.4.0] - 2026-05-10

### Added

- Add optional custom frontend domain validation to smoke tests when a custom domain is configured

### Changed

- Enhance deployment and smoke-test workflows to pass custom domain configuration through CI/CD
- Refactor smoke test header handling with a shared helper for clearer authenticated request setup

## [1.3.1] - 2026-05-10

### Added

- Add Speech region environment variable for the API container app to enable production transcription session startup

## [1.3.0] - 2026-05-10

### Added

- Add a reusable `usePageTitle` hook and apply it across app pages for consistent document titles

### Changed

- Update MCP server configuration structure and documentation for local development setup

## [1.2.0] - 2026-05-10

### Added

- Prompt Babbler agent orchestration and Foundry runner support
- MCP agent tools for orchestration and execution
- Additional unit tests for agent orchestration and tools
- New application favicon assets

### Changed

- Refresh release-planning and validation artifacts
- Remove legacy Speckit prompts, agents, and templates
- Update deployment and documentation assets

## [1.1.2] - 2026-05-09

### Fixed

- Allow MCP health and liveness endpoints when access code protection is enabled
- Add MCP middleware unit tests for allowlisted and protected endpoint behavior

## [1.1.1] - 2026-05-09

### Fixed

- Normalize container image references in infrastructure and workflows

## [1.1.0] - 2026-05-09

### Added

- Add MCP Server with API client and health checks
- Add audio file upload and transcription
- Add semantic search with embeddings and vector search
- Add built-in prompt template creation scripts
- Add babble tag editing and colored tag display enhancements
- Add AI Coding Tool application description template
- Add Cosmos vector container initialization service
- Add issue templates for bug reports, chores, and feature requests

### Changed

- Redesign template listing with pagination, filter and sort controls, and babbles-style UX
- Redesign prompt template selection with a searchable, filterable template browser
- Update prompt generation to use POST requests
- Update prompt template terminology and structure
- Update template hashes and vector data types
- Add retry logic for Cosmos DB upserts
- Normalize container image references in workflows
- Update environment name length and add extensions
- Clean up code structure and change tracking artifacts
- Add new images and update Bicep configuration

### Documentation

- Update documentation to reflect Foundry Models
- Update README badge links and descriptions
- Expand authentication, MCP server, API, quickstart, and user guide documentation
- Update agent and Copilot guidance

### Infrastructure

- Add Cosmos DB vector container infrastructure and model deployment configuration
- Update CI/CD, smoke test, and production deployment workflows for API and MCP server releases

### Dependencies

- Bump `TypeScript` from 5.9.3 to 6.0.3 in frontend
- Bump frontend ESLint packages
- Bump frontend testing packages and `jsdom`
- Bump `lucide-react` in frontend
- Bump frontend OpenTelemetry packages
- Bump `Microsoft.Azure.Cosmos` from 3.58.0 to 3.59.0
- Bump `Microsoft.Extensions.AI.OpenAI` from 10.5.0 to 10.5.1
- Bump `Microsoft.Extensions.Http.Resilience` from 10.4.0 to 10.5.0
- Bump `Microsoft.Extensions.ServiceDiscovery` from 10.4.0 to 10.5.0
- Bump `Microsoft.Identity.Web` from 4.7.0 to 4.9.0
- Bump `System.Security.Cryptography.Xml` from 10.0.6 to 10.0.7
- Bump `MSTest.Sdk` and `MSTest.TestFramework`
- Bump GitHub Actions workflow dependencies

## [1.0.1] - 2026-04-25

### Changed

- Refactor AI-related components and dependencies
- Update Aspire and Playwright CLI documentation

### Dependencies

- Bump `@opentelemetry/context-zone` in frontend
- Bump `markdownlint-cli2` from 0.22.0 to 0.22.1
- Bump `react-hook-form` in frontend
- Bump `vite` in frontend
- Bump Tailwind group packages in frontend
- Bump testing group packages in frontend
- Bump `OpenTelemetry.Instrumentation.AspNetCore` and related packages
- Bump `Microsoft.AspNetCore.Mvc.Testing` from 10.0.6 to 10.0.7
- Bump `Microsoft.Extensions.Caching.Memory` from 10.0.6 to 10.0.7
- Bump `Aspire.Hosting.Testing` from 13.2.2 to 13.2.4

## [1.0.0] - 2026-04-25

### Added

- Initial project scaffold with .NET 10 backend and React 19 frontend
- Speech-to-text transcription via Azure AI Speech Service with WebSocket streaming
- Prompt generation via Azure OpenAI LLM with streaming (SSE)
- LLM settings management (endpoint, API key, deployments)
- Babble management (create, read, update, delete) with localStorage
- Template management with built-in and custom templates
- Aspire AppHost orchestration
- CI/CD pipeline with GitHub Actions
