import { LiveAnnouncer } from '@angular/cdk/a11y';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { errorInterceptor } from '../../../core/http/error.interceptor';
import { RequestsSearchState } from './requests-search.state';

const pageOf = (totalCount: number) => ({
  items: [],
  page: 1,
  pageSize: 20,
  totalCount,
  totalPages: 1,
});
const summaryOf = (total: number) => ({ total, byStatus: {}, byPriority: {} });

describe('RequestsSearchState', () => {
  let backend: HttpTestingController;
  let router: Router;
  let announce: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    announce = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        RequestsSearchState,
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: LiveAnnouncer, useValue: { announce } },
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    await router.navigateByUrl('/');
  });

  afterEach(() => backend.verify());

  const searches = () => backend.match((r) => r.url === '/api/requests');
  const summaries = () => backend.match((r) => r.url === '/api/requests/summary');
  const answerAll = (total: number) => {
    searches().forEach((r) => r.cancelled || r.flush(pageOf(total)));
    summaries().forEach((r) => r.cancelled || r.flush(summaryOf(total)));
  };

  it('cancels the first search when the URL changes again before it answers', async () => {
    TestBed.inject(RequestsSearchState);
    answerAll(5);

    await router.navigateByUrl('/?status=New');
    await router.navigateByUrl('/?status=Waiting');
    const [first, second] = searches();

    expect(first.request.params.get('status')).toBe('New');
    expect(first.cancelled).toBe(true);
    expect(second.request.params.get('status')).toBe('Waiting');
    answerAll(1);
  });

  it('marks an empty result and announces the count', () => {
    const state = TestBed.inject(RequestsSearchState);

    answerAll(0);

    expect(state.empty()).toBe(true);
    expect(announce).toHaveBeenCalledWith('נמצאו 0 פניות', 'polite');
  });

  it('keeps the error and loads again on retry', () => {
    const state = TestBed.inject(RequestsSearchState);
    searches()[0].flush({ traceId: '00-x-01' }, { status: 500, statusText: 'x' });
    summaries()[0].flush(summaryOf(0));

    expect(state.error()?.kind).toBe('server');

    state.retry();
    answerAll(3);

    expect(state.error()).toBeNull();
    expect(state.page()?.totalCount).toBe(3);
  });

  it('goes back to page 1 when a filter changes', async () => {
    const state = TestBed.inject(RequestsSearchState);
    await router.navigateByUrl('/?page=3');
    answerAll(100);

    await state.update({ priority: ['High'] });

    expect(router.url).toBe('/?priority=High');
    answerAll(10);
  });

  it('does not reload the summary when only the page changes', async () => {
    TestBed.inject(RequestsSearchState);
    answerAll(50);

    await router.navigateByUrl('/?page=2');

    expect(searches()).toHaveLength(1);
    expect(summaries()).toHaveLength(0);
    answerAll(50);
  });
});
