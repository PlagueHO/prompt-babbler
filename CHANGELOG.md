# Changelog

All notable changes to the Prompt Babbler project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
