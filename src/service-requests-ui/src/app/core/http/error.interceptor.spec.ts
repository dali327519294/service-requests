import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiError } from './api-error';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  function failWith(status: number, body: object): ApiError {
    let error: ApiError | undefined;
    http.get('/api/requests/7').subscribe({ error: (e: ApiError) => (error = e) });
    backend.expectOne('/api/requests/7').flush(body, { status, statusText: 'x' });
    return error!;
  }

  it('turns 409 into a conflict with the current state', () => {
    const current = { id: 7, status: 'Waiting', rowVersion: 'AAAAAAAAB9E=' };

    const error = failWith(409, { title: 'The request was changed by someone else.', current });

    expect(error.kind).toBe('conflict');
    expect(error.current).toEqual(current);
    expect(error.message).toBe('הפנייה שונתה בינתיים על ידי משתמש אחר.');
  });

  it('turns a disallowed transition into invalidTransition with the allowed statuses', () => {
    const error = failWith(400, {
      title: 'The status change is not allowed.',
      allowedTransitions: ['InProgress'],
    });

    expect(error.kind).toBe('invalidTransition');
    expect(error.allowedTransitions).toEqual(['InProgress']);
  });

  it('keeps the field names of a validation error', () => {
    const error = failWith(400, {
      errors: { PageSize: ['The field PageSize must be between 1 and 100.'] },
    });

    expect(error.kind).toBe('validation');
    expect(error.fieldErrors).toEqual({
      PageSize: ['The field PageSize must be between 1 and 100.'],
    });
  });

  it('shows the trace id of a server error as a support code', () => {
    const error = failWith(500, { title: 'An unexpected error occurred.', traceId: '00-abc-01' });

    expect(error.kind).toBe('server');
    expect(error.traceId).toBe('00-abc-01');
    expect(error.message).toContain('00-abc-01');
  });

  it('reports a gateway that cannot reach the API as a lost connection', () => {
    expect(failWith(503, {}).kind).toBe('network');
  });

  it('reports a network failure', () => {
    let error: ApiError | undefined;
    http.get('/api/requests').subscribe({ error: (e: ApiError) => (error = e) });

    backend.expectOne('/api/requests').error(new ProgressEvent('error'));

    expect(error?.kind).toBe('network');
  });
});
