# Changelog

All notable changes to the Prompt Babbler project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial project scaffold with .NET 10 backend and React 19 frontend
- Speech-to-text transcription via Azure OpenAI Whisper
- Prompt generation via Azure OpenAI LLM with streaming (SSE)
- LLM settings management (endpoint, API key, deployments)
- Babble management (create, read, update, delete) with localStorage
- Template management with built-in and custom templates
- Aspire AppHost orchestration
- CI/CD pipeline with GitHub Actions
