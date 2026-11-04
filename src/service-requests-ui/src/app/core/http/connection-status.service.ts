import { HttpClient } from '@angular/common/http';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';

/** Whether the API is reachable; drives the single disconnection banner. */
@Injectable({ providedIn: 'root' })
export class ConnectionStatusService {
  private readonly http = inject(HttpClient);
  private readonly state = signal(true);
  readonly online = this.state.asReadonly();

  constructor() {
    // The browser knows first when the network itself goes away
    const goOffline = () => this.markOffline();
    window.addEventListener('offline', goOffline);
    inject(DestroyRef).onDestroy(() => window.removeEventListener('offline', goOffline));
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
