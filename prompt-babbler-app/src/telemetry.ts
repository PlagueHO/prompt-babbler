/**
 * OpenTelemetry browser SDK initialization.
 *
 * Sends traces and metrics to the Aspire dashboard via HTTP OTLP.
 * Gracefully no-ops when the OTLP endpoint is not configured (e.g. standalone dev).
 *
 * @see https://aspire.dev/dashboard/enable-browser-telemetry/
 */

import { trace, metrics, type Span } from '@opentelemetry/api';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { SimpleSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-http';
import {
  MeterProvider,
  PeriodicExportingMetricReader,
} from '@opentelemetry/sdk-metrics';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction';

// Compile-time constants injected by Vite from Aspire env vars
declare const __OTEL_EXPORTER_OTLP_ENDPOINT__: string;
declare const __OTEL_EXPORTER_OTLP_HEADERS__: string;
declare const __OTEL_RESOURCE_ATTRIBUTES__: string;
declare const __OTEL_SERVICE_NAME__: string;

const SERVICE_NAME = 'prompt-babbler-app';

/** Parse comma-delimited `key=value` pairs into a record. */
function parseDelimitedValues(s: string): Record<string, string> {
  if (!s) return {};
  const result: Record<string, string> = {};
  for (const pair of s.split(',')) {
    const idx = pair.indexOf('=');
    if (idx > 0) {
      result[pair.slice(0, idx).trim()] = pair.slice(idx + 1).trim();
    }
  }
  return result;
}

function getEndpoint(): string {
  return typeof __OTEL_EXPORTER_OTLP_ENDPOINT__ !== 'undefined'
    ? __OTEL_EXPORTER_OTLP_ENDPOINT__
    : '';
}

function getHeaders(): Record<string, string> {
  const raw =
    typeof __OTEL_EXPORTER_OTLP_HEADERS__ !== 'undefined'
      ? __OTEL_EXPORTER_OTLP_HEADERS__
      : '';
  return parseDelimitedValues(raw);
}

function getResourceAttributes(): Record<string, string> {
  const raw =
    typeof __OTEL_RESOURCE_ATTRIBUTES__ !== 'undefined'
      ? __OTEL_RESOURCE_ATTRIBUTES__
      : '';
  return parseDelimitedValues(raw);
}

function getServiceName(): string {
  const name =
    typeof __OTEL_SERVICE_NAME__ !== 'undefined' ? __OTEL_SERVICE_NAME__ : '';
  return name || SERVICE_NAME;
}

let initialized = false;

/**
 * Initialize the OpenTelemetry SDK. Call once before React renders.
 * No-ops if the OTLP endpoint is not configured.
 */
export function initTelemetry(): void {
  if (initialized) return;
  initialized = true;

  const endpoint = getEndpoint();
  if (!endpoint) {
    console.debug('[Telemetry] OTEL_EXPORTER_OTLP_ENDPOINT not set — telemetry disabled');
    return;
  }

  const headers = getHeaders();
  const attributes = getResourceAttributes();
  attributes[ATTR_SERVICE_NAME] = getServiceName();

  const resource = resourceFromAttributes(attributes);

  // --- Traces ---
  const traceExporter = new OTLPTraceExporter({
    url: `${endpoint}/v1/traces`,
    headers,
  });

  const tracerProvider = new WebTracerProvider({
    resource,
    spanProcessors: [new SimpleSpanProcessor(traceExporter)],
  });
  tracerProvider.register({
    contextManager: new ZoneContextManager(),
  });

  // --- Metrics ---
  const metricExporter = new OTLPMetricExporter({
    url: `${endpoint}/v1/metrics`,
    headers,
  });

  const meterProvider = new MeterProvider({
    resource,
    readers: [
      new PeriodicExportingMetricReader({
        exporter: metricExporter,
        exportIntervalMillis: 10_000,
      }),
    ],
  });
  metrics.setGlobalMeterProvider(meterProvider);

  // --- Auto-instrumentations ---
  registerInstrumentations({
    instrumentations: [
      new DocumentLoadInstrumentation(),
      new FetchInstrumentation({
        // Propagate trace context to same-origin API calls
        propagateTraceHeaderCorsUrls: [/.*/],
      }),
      new UserInteractionInstrumentation(),
    ],
  });

  console.debug('[Telemetry] OpenTelemetry initialized — exporting to', endpoint);
}

// --- Shared tracer & meter for custom instrumentation ---

export const tracer = trace.getTracer(SERVICE_NAME);
export const meter = metrics.getMeter(SERVICE_NAME);

// --- Convenience helpers ---

/** Record a duration (ms) on a span and end it. */
export function endSpanWithDuration(span: Span, startMs: number): void {
  span.setAttribute('duration_ms', performance.now() - startMs);
  span.end();
}
