import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiError } from './api-error';

interface ProblemBody {
  traceId?: string;
  errors?: Record<string, string[]>;
  current?: ApiError['current'];
  allowedTransitions?: ApiError['allowedTransitions'];
}

/** Turns every HttpErrorResponse into an ApiError, so screens never read status codes. */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: unknown) =>
      throwError(() => (error instanceof HttpErrorResponse ? toApiError(error) : error)),
    ),
  );

export function toApiError(error: HttpErrorResponse): ApiError {
  const body = (
    typeof error.error === 'object' && error.error !== null ? error.error : {}
  ) as ProblemBody;

  switch (error.status) {
    case 0:
    case 502:
    case 503:
    case 504:
      return { kind: 'network', message: 'אין חיבור לשרת.' };
    case 400:
      return body.allowedTransitions
        ? {
            kind: 'invalidTransition',
            message: 'המעבר לסטטוס הזה אינו מותר.',
            allowedTransitions: body.allowedTransitions,
          }
        : { kind: 'validation', message: 'חלק מהשדות אינם תקינים.', fieldErrors: body.errors };
    case 401:
      return { kind: 'unauthorized', message: 'יש להתחבר מחדש.' };
    case 403:
      return { kind: 'forbidden', message: 'אין לך הרשאה לפעולה הזו.' };
    case 404:
      return { kind: 'notFound', message: 'הפנייה לא נמצאה.' };
    case 409:
      return {
        kind: 'conflict',
        message: 'הפנייה שונתה בינתיים על ידי משתמש אחר.',
        current: body.current,
      };
    default:
      return {
        kind: 'server',
        message: body.traceId
          ? `אירעה שגיאה בשרת. קוד לתמיכה: ${body.traceId}`
          : 'אירעה שגיאה בשרת.',
        traceId: body.traceId,
      };
  }
}
