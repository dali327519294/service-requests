import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { errorInterceptor } from '../../../core/http/error.interceptor';
import { RequestsListPage } from './requests-list-page';

describe('RequestsListPage', () => {
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RequestsListPage],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
  });

  async function render(answer: (search: TestRequest) => void): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(RequestsListPage);
    fixture.detectChanges();
    answer(backend.expectOne((r) => r.url === '/api/requests'));
    backend
      .expectOne((r) => r.url === '/api/requests/summary')
      .flush({ total: 0, byStatus: {}, byPriority: {} });
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('says no requests were found and offers to clear the filters', async () => {
    const page = await render((r) =>
      r.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }),
    );

    expect(page.querySelector('[role=status] h2')?.textContent?.trim()).toBe('לא נמצאו פניות');
    expect(page.textContent).toContain('ניקוי כל הסינונים');
  });

  it('adds a status to the URL when its card is clicked, and removes it on a second click', async () => {
    const summaryAll = {
      total: 5,
      byStatus: { New: 5, InProgress: 0, Waiting: 0, Completed: 0 },
      byPriority: { Low: 0, Medium: 5, High: 0 },
    };
    const answer = () => {
      backend
        .match((r) => r.url === '/api/requests')
        .forEach(
          (r) =>
            r.cancelled ||
            r.flush({ items: [], page: 1, pageSize: 20, totalCount: 5, totalPages: 1 }),
        );
      backend
        .match((r) => r.url === '/api/requests/summary')
        .forEach((r) => r.cancelled || r.flush(summaryAll));
    };
    const fixture = TestBed.createComponent(RequestsListPage);
    const router = TestBed.inject(Router);
    fixture.detectChanges();
    answer();
    await fixture.whenStable();
    const newCard = () =>
      (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
        'button[data-status=new]',
      )!;

    newCard().click();
    await fixture.whenStable();
    answer();
    expect(router.url).toBe('/?status=New');

    newCard().click();
    await fixture.whenStable();
    answer();
    expect(router.url).toBe('/');
  });

  it('shows the error with the support code and a retry button', async () => {
    const page = await render((r) =>
      r.flush({ traceId: '00-abc-01' }, { status: 500, statusText: 'x' }),
    );

    expect(page.querySelector('[role=alert]')?.textContent).toContain('00-abc-01');
    expect(page.querySelector('[role=alert] button')?.textContent).toContain('נסו שוב');
  });
});
