import { LiveAnnouncer } from '@angular/cdk/a11y';
import { computed, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, distinctUntilChanged, map, merge, of, Subject, switchMap, tap } from 'rxjs';
import {
  PagedResult,
  RequestListItem,
  RequestQuery,
  RequestSummary,
} from '../../../core/api/models';
import { RequestsApiService } from '../../../core/api/requests-api.service';
import { ApiError } from '../../../core/http/api-error';
import { filtersOf, sameQuery, toParams, toQuery } from './query-params';

const counts = new Intl.NumberFormat('he-IL');

/**
 * The list page's state. The URL is the source of truth: every change goes through the router,
 * so refresh, Back and a shared link all show the same view.
 */
@Injectable()
export class RequestsSearchState {
  private readonly api = inject(RequestsApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly announcer = inject(LiveAnnouncer);
  private readonly reload$ = new Subject<void>();

  readonly query = toSignal(this.route.queryParamMap.pipe(map(toQuery)), { requireSync: true });

  private readonly list = signal<PagedResult<RequestListItem> | null>(null);
  private readonly listError = signal<ApiError | null>(null);
  readonly summary = signal<RequestSummary | null>(null);
  readonly loading = signal(false);

  readonly page = this.list.asReadonly();
  readonly error = this.listError.asReadonly();
  readonly empty = computed(() => this.list()?.totalCount === 0);

  constructor() {
    const query$ = this.route.queryParamMap.pipe(map(toQuery), distinctUntilChanged(sameQuery));
    const now = () => toQuery(this.route.snapshot.queryParamMap);

    // A newer query cancels the request still in flight (switchMap), so a slow old answer never wins
    merge(query$, this.reload$.pipe(map(now)))
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap((query) =>
          this.api.search(query).pipe(
            map((result) => ({ result, error: null })),
            catchError((error: ApiError) => of({ result: null, error })),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(({ result, error }) => {
        // A page past the end (an old link, or fewer results than before): show the last page instead
        if (result && result.page > result.totalPages && result.totalPages > 0) {
          void this.router.navigate([], {
            relativeTo: this.route,
            queryParams: toParams({ ...this.query(), page: result.totalPages }),
            replaceUrl: true,
          });
          return;
        }

        this.loading.set(false);
        this.listError.set(error);
        if (result) {
          this.list.set(result);
          this.announcer.announce(`נמצאו ${counts.format(result.totalCount)} פניות`, 'polite');
        }
      });

    // Paging and sorting do not change the counts, so the summary only reloads when the filters do
    merge(
      query$.pipe(map(filtersOf), distinctUntilChanged(sameQuery)),
      this.reload$.pipe(map(() => filtersOf(now()))),
    )
      .pipe(
        switchMap((filters) => this.api.getSummary(filters).pipe(catchError(() => of(null)))),
        takeUntilDestroyed(),
      )
      .subscribe((summary) => this.summary.set(summary));
  }

  /** Any change other than the page itself starts over at page 1. */
  update(patch: Partial<RequestQuery>): Promise<boolean> {
    const next: RequestQuery = { ...this.query(), ...patch };
    if (!('page' in patch)) {
      next.page = 1;
    }

    return this.router.navigate([], { relativeTo: this.route, queryParams: toParams(next) });
  }

  clearFilters(): Promise<boolean> {
    const { sortBy, sortDirection, pageSize } = this.query();
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams: toParams({ sortBy, sortDirection, pageSize }),
    });
  }

  retry(): void {
    this.reload$.next();
  }
}
