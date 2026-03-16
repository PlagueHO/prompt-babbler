/**
 * WebSocket-based real-time transcription client.
 *
 * Opens a WebSocket to the backend `/api/transcribe/stream` endpoint,
 * sends binary PCM audio frames, and receives JSON transcription events.
 */

// Injected by Vite from Aspire service discovery at build/dev time
declare const __API_BASE_URL__: string;

function getWsBaseUrl(): string {
  const base =
    typeof __API_BASE_URL__ !== 'undefined' ? __API_BASE_URL__ : '';
  if (!base) {
    // Fallback: derive from current page location
    const proto = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    return `${proto}//${window.location.host}`;
  }
  return base.replace(/^https:/, 'wss:').replace(/^http:/, 'ws:');
}

export interface TranscriptionMessage {
  text: string;
  isFinal: boolean;
}

export type TranscriptionCallback = (msg: TranscriptionMessage) => void;
export type ErrorCallback = (error: Event | string) => void;

export class TranscriptionStream {
  private ws: WebSocket | null = null;
  private _isOpen = false;
  #onMessage: TranscriptionCallback;
  #onError?: ErrorCallback;

  constructor(
    onMessage: TranscriptionCallback,
    onError?: ErrorCallback,
  ) {
    this.#onMessage = onMessage;
    this.#onError = onError;
  }

  get isOpen(): boolean {
    return this._isOpen;
  }

  open(language?: string, accessToken?: string): void {
    if (this.ws) return;

    const base = getWsBaseUrl();
    const params = new URLSearchParams();
    if (language) params.append('language', language);
    if (accessToken) params.append('access_token', accessToken);

    const queryString = params.toString();
    const url = `${base}/api/transcribe/stream${queryString ? `?${queryString}` : ''}`;

    this.ws = new WebSocket(url);
    this.ws.binaryType = 'arraybuffer';

    this.ws.onopen = () => {
      this._isOpen = true;
    };

    this.ws.onmessage = (event: MessageEvent) => {
      if (typeof event.data === 'string') {
        try {
          const msg = JSON.parse(event.data) as TranscriptionMessage;
          this.#onMessage(msg);
        } catch {
          // Ignore malformed JSON
        }
      }
    };

    this.ws.onerror = (event) => {
      this.#onError?.(event);
    };

    this.ws.onclose = () => {
      this._isOpen = false;
      this.ws = null;
    };
  }

  /** Send raw PCM audio data (Int16 LE) as a binary WebSocket frame. */
  sendAudio(pcmBuffer: ArrayBuffer): void {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(pcmBuffer);
    }
  }

  /** Gracefully close the WebSocket connection. */
  close(): void {
    if (this.ws) {
      if (this.ws.readyState === WebSocket.OPEN) {
        this.ws.close(1000, 'done');
      }
      this.ws = null;
      this._isOpen = false;
    }
  }
}
