import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, retry, tap, throwError, timer } from 'rxjs';
import { ConnectionStatusService } from './connection-status.service';

// 0: the browser reached no server at all. 502/503/504: a gateway (the dev proxy, a load balancer)
// answered, but the API behind it did not - the most common way an outage shows up
const isConnectionError = (error: unknown) =>
  error instanceof HttpErrorResponse && [0, 502, 503, 504].includes(error.status);

/**
 * Reads (GET) are retried three times, waiting 1, 2 and 4 seconds, only when the connection failed.
 * Writes (PATCH, POST) are never retried: the first attempt may have been saved even though its answer was lost.
 */
export const connectionInterceptor: HttpInterceptorFn = (req, next) => {
  const connection = inject(ConnectionStatusService);
  const request$ = next(req).pipe(
    tap((event) => {
      if (event instanceof HttpResponse) {
        connection.markOnline();
      }
    }),
  );

  const withRetry =
    req.method === 'GET'
      ? request$.pipe(
          retry({
            count: 3,
            delay: (error, attempt) =>
              isConnectionError(error) ? timer(1000 * 2 ** (attempt - 1)) : throwError(() => error),
          }),
        )
      : request$;

  return withRetry.pipe(
    catchError((error: unknown) => {
      if (isConnectionError(error)) {
        connection.markOffline();
      }

      return throwError(() => error);
    }),
  );
};
