import { HttpClient } from '@angular/common/http';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';

/** Whether the API is reachable; drives the single disconnection banner. */
@Injectable({ providedIn: 'root' })
export class ConnectionStatusService {
  private readonly http = inject(HttpClient);
  private readonly state = signal(true);
  readonly online = this.state.asReadonly();

  constructor() {
    // The browser knows first when the network goes away or comes back. Coming back is only a hint:
    // the API may still be down, so it triggers a health check rather than marking us online
    const goOffline = () => this.markOffline();
    const checkAgain = () => this.retry();
    window.addEventListener('offline', goOffline);
    window.addEventListener('online', checkAgain);
    inject(DestroyRef).onDestroy(() => {
      window.removeEventListener('offline', goOffline);
      window.removeEventListener('online', checkAgain);
    });
  }

  markOnline(): void {
    this.state.set(true);
  }

  markOffline(): void {
    this.state.set(false);
  }

  /** Asks the health endpoint; a successful answer goes through the interceptor and marks us online. */
  retry(): void {
    this.http.get('/health', { responseType: 'text' }).subscribe({ error: () => undefined });
  }
}
