import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RequestsApiService } from './requests-api.service';

describe('RequestsApiService', () => {
  let api: RequestsApiService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(RequestsApiService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  const searchUrl = () => backend.expectOne((r) => r.url === '/api/requests').request.urlWithParams;

  it('sends every chosen status as its own parameter and skips empty filters', () => {
    api.search({ status: ['New', 'Waiting'], search: '', page: 2, pageSize: 20 }).subscribe();

    expect(searchUrl()).toBe('/api/requests?status=New&status=Waiting&page=2&pageSize=20');
  });

  it('encodes + so the server does not read it as a space', () => {
    api.search({ search: 'C++' }).subscribe();

    expect(searchUrl()).toBe('/api/requests?search=C%2B%2B');
  });

  it('sends dates as UTC instants', () => {
    api.search({ createdFrom: new Date(Date.UTC(2026, 2, 31, 21)) }).subscribe();

    expect(searchUrl()).toBe('/api/requests?createdFrom=2026-03-31T21%3A00%3A00.000Z');
  });

  it('updates a status with the version the user saw', () => {
    api.updateStatus(7, { status: 'Completed', rowVersion: 'AAAAAAAAB9E=' }).subscribe();

    const request = backend.expectOne('/api/requests/7/status');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ status: 'Completed', rowVersion: 'AAAAAAAAB9E=' });
  });
});
