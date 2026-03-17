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

/** Connection timeout in milliseconds. */
const OPEN_TIMEOUT_MS = 10_000;

/**
 * Maximum number of PCM frames to buffer while waiting for the WebSocket
 * to open. At 16 kHz / 16-bit mono the worklet fires ~62.5 frames/sec
 * (128 samples each), so 320 frames ≈ 5 seconds of audio ≈ 160 KB.
 */
const MAX_BUFFERED_FRAMES = 320;

export class TranscriptionStream {
  private ws: WebSocket | null = null;
  private _isOpen = false;
  private _pendingFrames: ArrayBuffer[] = [];
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

  /**
   * Open the WebSocket connection. Returns a Promise that resolves once the
   * connection is established, or rejects on error / timeout.
   */
  open(language?: string, accessToken?: string): Promise<void> {
    if (this.ws) return Promise.resolve();

    const base = getWsBaseUrl();
    const params = new URLSearchParams();
    if (language) params.append('language', language);
    if (accessToken) params.append('access_token', accessToken);

    const queryString = params.toString();
    const url = `${base}/api/transcribe/stream${queryString ? `?${queryString}` : ''}`;

    console.debug('[TranscriptionStream] Opening WebSocket to', url);

    return new Promise<void>((resolve, reject) => {
      const ws = new WebSocket(url);
      ws.binaryType = 'arraybuffer';
      this.ws = ws;

      const timer = setTimeout(() => {
        if (!this._isOpen) {
          const msg = 'WebSocket connection timed out';
          console.error('[TranscriptionStream]', msg);
          ws.close();
          this.ws = null;
          reject(new Error(msg));
        }
      }, OPEN_TIMEOUT_MS);

      ws.onopen = () => {
        clearTimeout(timer);
        this._isOpen = true;
        console.debug('[TranscriptionStream] WebSocket opened');
        this._flushBufferedFrames();
        resolve();
      };

      ws.onmessage = (event: MessageEvent) => {
        if (typeof event.data === 'string') {
          try {
            const msg = JSON.parse(event.data) as TranscriptionMessage;
            console.debug('[TranscriptionStream] Received:', msg.isFinal ? 'FINAL' : 'partial', JSON.stringify(msg.text).slice(0, 80));
            this.#onMessage(msg);
          } catch {
            console.warn('[TranscriptionStream] Malformed JSON:', event.data);
          }
        }
      };

      ws.onerror = (event) => {
        clearTimeout(timer);
        console.error('[TranscriptionStream] WebSocket error:', event);
        this.#onError?.(event);
        if (!this._isOpen) {
          this.ws = null;
          reject(new Error('WebSocket connection failed'));
        }
      };

      ws.onclose = (event) => {
        clearTimeout(timer);
        console.debug('[TranscriptionStream] WebSocket closed — code:', event.code, 'reason:', event.reason);
        this._isOpen = false;
        this.ws = null;
        if (!this._isOpen) {
          // If we never opened, reject the promise
          reject(new Error(`WebSocket closed before open (code: ${event.code})`));
        }
      };
    });
  }

  private _framesSent = 0;

  /** Flush any PCM frames that were buffered while the WebSocket was connecting. */
  private _flushBufferedFrames(): void {
    if (this._pendingFrames.length === 0) return;
    console.debug(`[TranscriptionStream] Flushing ${this._pendingFrames.length} buffered audio frames`);
    for (const frame of this._pendingFrames) {
      this.ws!.send(frame);
      this._framesSent++;
    }
    this._pendingFrames = [];
  }

  /** Send raw PCM audio data (Int16 LE) as a binary WebSocket frame. */
  sendAudio(pcmBuffer: ArrayBuffer): void {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(pcmBuffer);
      this._framesSent++;
      if (this._framesSent === 1 || this._framesSent % 100 === 0) {
        console.debug(`[TranscriptionStream] Audio frames sent: ${this._framesSent}, last size: ${pcmBuffer.byteLength}B`);
      }
    } else if (this.ws && this.ws.readyState === WebSocket.CONNECTING) {
      // Buffer frames while the WebSocket handshake is in progress
      if (this._pendingFrames.length < MAX_BUFFERED_FRAMES) {
        this._pendingFrames.push(pcmBuffer);
      }
    }
  }

  /** Gracefully close the WebSocket connection. */
  close(): void {
    this._pendingFrames = [];
    if (this.ws) {
      if (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING) {
        this.ws.close(1000, 'done');
      }
      this.ws = null;
      this._isOpen = false;
    }
  }
}
