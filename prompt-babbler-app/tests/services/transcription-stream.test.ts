import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  TranscriptionStream,
  type TranscriptionMessage,
} from '@/services/transcription-stream';

// ---------------------------------------------------------------------------
// Minimal WebSocket mock
// ---------------------------------------------------------------------------
type WsHandler = ((ev: unknown) => void) | null;

class MockWebSocket {
  static CONNECTING = 0 as const;
  static OPEN = 1 as const;
  static CLOSING = 2 as const;
  static CLOSED = 3 as const;
  CONNECTING = 0 as const;
  OPEN = 1 as const;
  CLOSING = 2 as const;
  CLOSED = 3 as const;

  url: string;
  binaryType = '';
  readyState = MockWebSocket.CONNECTING;
  onopen: WsHandler = null;
  onmessage: WsHandler = null;
  onerror: WsHandler = null;
  onclose: WsHandler = null;
  sentFrames: unknown[] = [];

  constructor(url: string) {
    this.url = url;
    // Register so tests can get a reference
    MockWebSocket.instances.push(this);
  }

  send(data: unknown) {
    this.sentFrames.push(data);
  }

  close(_code?: number, _reason?: string) {
    this.readyState = MockWebSocket.CLOSED;
    this.onclose?.({ code: _code ?? 1000, reason: _reason ?? '' });
  }

  // --- test helpers ---
  static instances: MockWebSocket[] = [];
  static reset() {
    MockWebSocket.instances = [];
  }

  /** Simulate the server accepting the connection */
  simulateOpen() {
    this.readyState = MockWebSocket.OPEN;
    this.onopen?.({});
  }

  /** Simulate a server-sent text message */
  simulateMessage(data: string) {
    this.onmessage?.({ data });
  }

  /** Simulate a connection error */
  simulateError() {
    this.onerror?.({});
  }
}

// Replace global WebSocket
const OriginalWebSocket = globalThis.WebSocket;

beforeEach(() => {
  MockWebSocket.reset();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (globalThis as any).WebSocket = MockWebSocket;
});

afterEach(() => {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (globalThis as any).WebSocket = OriginalWebSocket;
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------
describe('TranscriptionStream', () => {
  describe('open()', () => {
    it('resolves when WebSocket opens', async () => {
      const stream = new TranscriptionStream(vi.fn());
      const openPromise = stream.open();

      // The constructor should have been called
      expect(MockWebSocket.instances).toHaveLength(1);
      const ws = MockWebSocket.instances[0];

      // Simulate server accepting the connection
      ws.simulateOpen();

      await expect(openPromise).resolves.toBeUndefined();
      expect(stream.isOpen).toBe(true);
    });

    it('rejects when WebSocket errors before opening', async () => {
      const onError = vi.fn();
      const stream = new TranscriptionStream(vi.fn(), onError);
      const openPromise = stream.open();

      const ws = MockWebSocket.instances[0];
      ws.simulateError();

      await expect(openPromise).rejects.toThrow('WebSocket connection failed');
      expect(onError).toHaveBeenCalled();
      expect(stream.isOpen).toBe(false);
    });

    it('is a no-op when already connected', async () => {
      const stream = new TranscriptionStream(vi.fn());
      const p1 = stream.open();
      MockWebSocket.instances[0].simulateOpen();
      await p1;

      // Calling open again should return immediately
      const p2 = stream.open();
      await expect(p2).resolves.toBeUndefined();
      // No new WebSocket created
      expect(MockWebSocket.instances).toHaveLength(1);
    });
  });

  describe('sendAudio() buffering', () => {
    it('buffers frames while WebSocket is CONNECTING and flushes on open', async () => {
      const stream = new TranscriptionStream(vi.fn());
      const openPromise = stream.open();
      const ws = MockWebSocket.instances[0];

      // Send frames while still CONNECTING
      const frame1 = new ArrayBuffer(128);
      const frame2 = new ArrayBuffer(128);
      stream.sendAudio(frame1);
      stream.sendAudio(frame2);

      // Frames should NOT have been sent to the WebSocket yet
      expect(ws.sentFrames).toHaveLength(0);

      // Open the connection — should flush buffered frames
      ws.simulateOpen();
      await openPromise;

      // The 2 buffered frames should have been flushed
      expect(ws.sentFrames).toHaveLength(2);
      expect(ws.sentFrames[0]).toBe(frame1);
      expect(ws.sentFrames[1]).toBe(frame2);
    });

    it('sends frames directly when WebSocket is already open', async () => {
      const stream = new TranscriptionStream(vi.fn());
      const openPromise = stream.open();
      MockWebSocket.instances[0].simulateOpen();
      await openPromise;

      const ws = MockWebSocket.instances[0];
      const frame = new ArrayBuffer(64);
      stream.sendAudio(frame);

      expect(ws.sentFrames).toHaveLength(1);
      expect(ws.sentFrames[0]).toBe(frame);
    });

    it('drops frames when no WebSocket exists', () => {
      const stream = new TranscriptionStream(vi.fn());
      // No open() called — sendAudio should not throw
      stream.sendAudio(new ArrayBuffer(64));
    });
  });

  describe('message handling', () => {
    it('invokes onMessage callback for valid JSON', async () => {
      const onMessage = vi.fn();
      const stream = new TranscriptionStream(onMessage);
      const openPromise = stream.open();
      const ws = MockWebSocket.instances[0];
      ws.simulateOpen();
      await openPromise;

      const msg: TranscriptionMessage = { text: 'hello', isFinal: true };
      ws.simulateMessage(JSON.stringify(msg));

      expect(onMessage).toHaveBeenCalledWith(msg);
    });
  });

  describe('close()', () => {
    it('clears pending frames and closes the WebSocket', async () => {
      const stream = new TranscriptionStream(vi.fn());
      const openPromise = stream.open();

      // Buffer a frame
      stream.sendAudio(new ArrayBuffer(64));

      // Close before open resolves
      stream.close();

      // Should not throw even though open never resolves normally
      expect(stream.isOpen).toBe(false);

      // open promise will reject because the WebSocket was closed
      await expect(openPromise).rejects.toThrow();
    });
  });
});
