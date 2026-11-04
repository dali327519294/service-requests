import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { userHeaderInterceptor } from './user-header.interceptor';

describe('userHeaderInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([userHeaderInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    backend.verify();
    localStorage.clear();
    vi.restoreAllMocks();
  });

  const userSentTo = (url: string) => {
    http.get(url).subscribe();
    return backend.expectOne(url).request.headers.get('X-User-Name');
  };

  it('sends the stored user name to the API', () => {
    localStorage.setItem('sr.userName', 'avi.peretz');

    expect(userSentTo('/api/requests')).toBe('avi.peretz');
  });

  it('falls back to a default user when none is stored', () => {
    expect(userSentTo('/api/requests')).toBe('dana.levi');
  });

  it('falls back when the browser blocks storage', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked');
    });

    expect(userSentTo('/api/requests')).toBe('dana.levi');
  });

  it('does not send the user name outside the API', () => {
    expect(userSentTo('/health')).toBeNull();
  });
});
