import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { RequestDetails, StatusHistoryEntry } from '../../../core/api/models';
import { errorInterceptor } from '../../../core/http/error.interceptor';
import { RequestDetailsPage } from './request-details-page';

const details = (patch: Partial<RequestDetails> = {}): RequestDetails => ({
  id: 42,
  title: 'רעש מגנרטור בלילה',
  organizationName: 'עיריית חיפה',
  status: 'New',
  priority: 'High',
  assignedTo: null,
  createdAt: '2026-10-01T08:30:00Z',
  updatedAt: '2026-10-01T08:30:00Z',
  rowVersion: 'v1',
  allowedTransitions: ['InProgress', 'Waiting'],
  ...patch,
});

const history: StatusHistoryEntry[] = [
  {
    previousStatus: 'InProgress',
    newStatus: 'Waiting',
    changedAt: '2026-10-03T10:00:00Z',
    changedBy: 'dana.levi',
  },
  {
    previousStatus: 'New',
    newStatus: 'InProgress',
    changedAt: '2026-10-02T10:00:00Z',
    changedBy: 'yossi.cohen',
  },
];

describe('RequestDetailsPage', () => {
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'requests/:id', component: RequestDetailsPage },
          { path: 'requests', children: [] },
        ]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  /** Opens the page the way a click in the table does, with the list's query in the navigation state. */
  async function open(listQuery?: Record<string, string>) {
    const harness = await RouterTestingHarness.create();
    await TestBed.inject(Router).navigateByUrl('/requests/42', {
      state: listQuery ? { listQuery } : undefined,
    });
    harness.detectChanges();
    const el = () => harness.routeNativeElement as HTMLElement;
    const buttons = () =>
      [...el().querySelectorAll<HTMLButtonElement>('.actions button')].map((b) => ({
        text: b.textContent!.replace(/^[a-z_]+/, '').trim(),
        disabled: b.disabled,
      }));
    return { harness, el, buttons };
  }

  function answer(request = details(), entries = history) {
    backend.expectOne('/api/requests/42').flush(request);
    backend.expectOne('/api/requests/42/history').flush(entries);
  }

  it('offers a button only for each status the request may move to', async () => {
    const { harness, el, buttons } = await open();
    answer();
    harness.detectChanges();

    expect(el().querySelector('h1')?.textContent?.trim()).toBe('רעש מגנרטור בלילה');
    expect(buttons().map((b) => b.text)).toEqual(['בטיפול', 'ממתינה']);
  });

  it('shows the history in the order it arrived, newest first', async () => {
    const { harness, el } = await open();
    answer();
    harness.detectChanges();

    const who = [...el().querySelectorAll('.history__who')].map((w) => w.textContent?.trim());
    expect(who).toEqual(['dana.levi', 'yossi.cohen']);
  });

  it('sends the version it showed, then shows the new status and history', async () => {
    const { harness, el, buttons } = await open();
    answer();
    harness.detectChanges();

    el().querySelector<HTMLButtonElement>('.actions button')!.click();
    harness.detectChanges();
    expect(buttons().every((b) => b.disabled)).toBe(true);

    const patch = backend.expectOne('/api/requests/42/status');
    expect(patch.request.method).toBe('PATCH');
    expect(patch.request.body).toEqual({ status: 'InProgress', rowVersion: 'v1' });
    patch.flush(
      details({ status: 'InProgress', rowVersion: 'v2', allowedTransitions: ['Waiting'] }),
    );
    backend.expectOne('/api/requests/42/history').flush([
      {
        previousStatus: 'New',
        newStatus: 'InProgress',
        changedAt: '2026-10-04T09:00:00Z',
        changedBy: 'me',
      },
      ...history,
    ]);
    harness.detectChanges();

    expect(el().querySelector('.header app-status-chip')?.textContent).toContain('בטיפול');
    expect(buttons()).toEqual([{ text: 'ממתינה', disabled: false }]);
    expect(el().querySelectorAll('.history__item').length).toBe(3);
  });

  it('says the request was not found, without offering to try again', async () => {
    const { harness, el } = await open();
    backend.expectOne('/api/requests/42').flush(null, { status: 404, statusText: 'Not Found' });
    harness.detectChanges();

    // forkJoin gives up on the history once the request itself failed
    expect(backend.expectOne('/api/requests/42/history').cancelled).toBe(true);

    expect(el().querySelector('[role=alert] h1')?.textContent?.trim()).toBe('הפנייה לא נמצאה');
    expect(el().querySelector('[role=alert] p')?.textContent?.trim()).toBe(
      'ייתכן שהקישור שגוי, או שמספר הפנייה לא קיים.',
    );
    expect(el().querySelector('[role=alert] button')).toBeNull();
  });

  it('goes back to the list with the filters it came from', async () => {
    const { harness, el } = await open({ status: 'New', search: 'רעש' });
    answer();
    harness.detectChanges();

    const back = el().querySelector<HTMLAnchorElement>('a.back')!;
    expect(decodeURIComponent(back.getAttribute('href')!)).toBe('/requests?status=New&search=רעש');
  });
});
