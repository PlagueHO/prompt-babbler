# Skill: OpenTelemetry Browser SDK

## Confidence: medium

## Overview

prompt-babbler uses the OpenTelemetry browser SDK v2.x to send traces and metrics from the React frontend to the Aspire dashboard via HTTP OTLP. This skill covers the SDK setup, the breaking changes in v2.6.0, custom metrics, custom spans, and Aspire integration.

## SDK Setup (src/telemetry.ts)

### Provider Configuration

```typescript
// WebTracerProvider — use spanProcessors array in constructor (NOT addSpanProcessor)
const tracerProvider = new WebTracerProvider({
  resource,
  spanProcessors: [
    new SimpleSpanProcessor(traceExporter),
  ],
});

// MeterProvider — periodic metric export
const meterProvider = new MeterProvider({
  resource,
  readers: [
    new PeriodicExportingMetricReader({
      exporter: metricExporter,
      exportIntervalMillis: 10000,
    }),
  ],
});
```

**Critical:** `addSpanProcessor()` was removed in SDK v2.6.0. Always use the constructor `spanProcessors` array.

### Context Management

```typescript
tracerProvider.register({
  contextManager: new ZoneContextManager(),
});
```

`ZoneContextManager` enables async context propagation in the browser via Zone.js.

### Auto-Instrumentations

Three auto-instrumentations are registered:

1. **DocumentLoadInstrumentation** — traces page load events
1. **FetchInstrumentation** — traces fetch/XHR requests, propagates trace context to same-origin
1. **UserInteractionInstrumentation** — traces user clicks and interactions

### Initialization Flow

```typescript
// main.tsx — called BEFORE React renders
initTelemetry();
```

`initTelemetry()` no-ops when `__OTEL_EXPORTER_OTLP_ENDPOINT__` is empty (i.e., not running under Aspire).

## Aspire Integration

Aspire injects environment variables into the frontend process:

| Env Var | Vite Constant | Purpose |
|---------|---------------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `__OTEL_EXPORTER_OTLP_ENDPOINT__` | OTLP HTTP endpoint URL |
| `OTEL_EXPORTER_OTLP_HEADERS` | `__OTEL_EXPORTER_OTLP_HEADERS__` | Auth headers (API key) |
| `OTEL_RESOURCE_ATTRIBUTES` | `__OTEL_RESOURCE_ATTRIBUTES__` | Resource attributes |
| `OTEL_SERVICE_NAME` | `__OTEL_SERVICE_NAME__` | Service name |

The Aspire AppHost must have `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL` set in `launchSettings.json` to enable the HTTP OTLP endpoint (separate from the default gRPC endpoint).

Headers and resource attributes are parsed from comma-delimited format:

```typescript
const headers: Record<string, string> = {};
rawHeaders.split(',').forEach(pair => {
  const [key, value] = pair.split('=');
  if (key && value) headers[key.trim()] = value.trim();
});
```

## Custom Metrics

All metrics are histograms created from a shared meter:

```typescript
export const meter = metrics.getMeter('prompt-babbler-app');
```

| Metric Name | Unit | Source Hook | What It Measures |
|-------------|------|-------------|-----------------|
| `recording.audio_init_ms` | ms | `useAudioRecording` | Time to initialize AudioContext |
| `transcription.ttfw_ms` | ms | `useTranscription` | Time-to-first-word after connection |
| `transcription.ws_connect_ms` | ms | `useTranscription` | WebSocket connection establishment |
| `prompt.ttft_ms` | ms | `usePromptGeneration` | Time-to-first-token for SSE stream |
| `prompt.duration_ms` | ms | `usePromptGeneration` | Total prompt generation duration |

## Custom Spans

```typescript
export const tracer = trace.getTracer('prompt-babbler-app');
```

| Span Name | Source | What It Traces |
|-----------|--------|---------------|
| `transcription.session` | `useTranscription` | Full transcription session lifecycle |
| `transcription.time-to-first-word` | `useTranscription` | Period from connect to first word |
| `transcription.ws_connect` | `transcription-stream.ts` | WebSocket handshake |
| `recording.audio_init` | `useAudioRecording` | AudioContext + worklet initialization |
| `prompt.generate` | `usePromptGeneration` | Full prompt generation request |

## Helper Function

```typescript
export function endSpanWithDuration(span: Span, startMs: number) {
  span.setAttribute('duration_ms', Date.now() - startMs);
  span.end();
}
```

## Package Versions

| Package | Version |
|---------|---------|
| `@opentelemetry/api` | 1.9.0 |
| `@opentelemetry/sdk-trace-web` | 2.6.0 |
| `@opentelemetry/sdk-trace-base` | 2.6.0 |
| `@opentelemetry/sdk-metrics` | 2.6.0 |
| `@opentelemetry/resources` | 2.6.0 |
| `@opentelemetry/context-zone` | 2.6.0 |
| `@opentelemetry/exporter-trace-otlp-proto` | 0.213.0 |
| `@opentelemetry/exporter-metrics-otlp-http` | 0.213.0 |
| `@opentelemetry/instrumentation` | 0.213.0 |
| `@opentelemetry/instrumentation-document-load` | 0.58.0 |
| `@opentelemetry/instrumentation-fetch` | 0.213.0 |
| `@opentelemetry/instrumentation-user-interaction` | 0.57.0 |
| `@opentelemetry/semantic-conventions` | 1.40.0 |
