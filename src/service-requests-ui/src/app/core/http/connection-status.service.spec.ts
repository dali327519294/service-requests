import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { connectionInterceptor } from './connection.interceptor';
import { ConnectionStatusService } from './connection-status.service';

describe('ConnectionStatusService', () => {
  let status: ConnectionStatusService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([connectionInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    status = TestBed.inject(ConnectionStatusService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('goes offline when the browser loses the network', () => {
    window.dispatchEvent(new Event('offline'));

    expect(status.online()).toBe(false);
  });

  it('checks the API again when the network comes back, and clears the banner if it answers', () => {
    status.markOffline();

    window.dispatchEvent(new Event('online'));
    backend.expectOne('/health').flush('Healthy');

    expect(status.online()).toBe(true);
  });
});
