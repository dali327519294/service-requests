import { HttpInterceptorFn } from '@angular/common/http';

const storageKey = 'sr.userName';
const fallbackUser = 'dana.levi';

/** Temporary, until sign-in: tells the API who is acting. Replaced by the token interceptor if auth is built. */
export const userHeaderInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/')) {
    return next(req);
  }

  let user = fallbackUser;
  try {
    user = localStorage.getItem(storageKey) || fallbackUser;
  } catch {
    // Storage can be blocked (private mode, policy); the fallback keeps the app usable
  }

  return next(req.clone({ setHeaders: { 'X-User-Name': user } }));
};
