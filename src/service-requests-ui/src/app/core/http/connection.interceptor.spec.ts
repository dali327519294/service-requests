import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { connectionInterceptor } from './connection.interceptor';
import { ConnectionStatusService } from './connection-status.service';

describe('connectionInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([connectionInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    backend.verify();
    vi.useRealTimers();
  });

  const networkDown = () => backend.expectOne('/api/requests').error(new ProgressEvent('error'));

  it('retries a GET three times, waiting 1, 2 and 4 seconds, then reports offline', () => {
    let failed = false;
    http.get('/api/requests').subscribe({ error: () => (failed = true) });

    networkDown();
    for (const wait of [1000, 2000, 4000]) {
      vi.advanceTimersByTime(wait);
      networkDown();
    }

    expect(failed).toBe(true);
    expect(TestBed.inject(ConnectionStatusService).online()).toBe(false);
  });

  it('treats a gateway that cannot reach the API (502) like a lost connection', () => {
    let failed = false;
    http.get('/api/requests').subscribe({ error: () => (failed = true) });

    const gatewayDown = () =>
      backend.expectOne('/api/requests').flush('', { status: 502, statusText: 'Bad Gateway' });
    gatewayDown();
    for (const wait of [1000, 2000, 4000]) {
      vi.advanceTimersByTime(wait);
      gatewayDown();
    }

    expect(failed).toBe(true);
    expect(TestBed.inject(ConnectionStatusService).online()).toBe(false);
  });

  it('does not retry an answer from the API itself, such as 404', () => {
    http.get('/api/requests').subscribe({ error: () => undefined });

    backend.expectOne('/api/requests').flush('', { status: 404, statusText: 'Not Found' });
    vi.advanceTimersByTime(10_000);

    backend.expectNone('/api/requests');
  });

  it('never resends a PATCH: it may have been saved even if the answer was lost', () => {
    http.patch('/api/requests', {}).subscribe({ error: () => undefined });

    networkDown();
    vi.advanceTimersByTime(10_000);

    backend.expectNone('/api/requests');
  });

  it('marks the connection online again after a successful answer', () => {
    const status = TestBed.inject(ConnectionStatusService);
    status.markOffline();

    http.get('/api/requests').subscribe();
    backend.expectOne('/api/requests').flush([]);

    expect(status.online()).toBe(true);
  });
});
