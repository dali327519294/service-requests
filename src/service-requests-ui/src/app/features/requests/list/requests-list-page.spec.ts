import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
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

  it('shows the error with the support code and a retry button', async () => {
    const page = await render((r) =>
      r.flush({ traceId: '00-abc-01' }, { status: 500, statusText: 'x' }),
    );

    expect(page.querySelector('[role=alert]')?.textContent).toContain('00-abc-01');
    expect(page.querySelector('[role=alert] button')?.textContent).toContain('נסו שוב');
  });
});
