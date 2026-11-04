import { RequestDetails, RequestStatus } from '../api/models';

export type ApiErrorKind =
  | 'validation'
  | 'invalidTransition'
  | 'notFound'
  | 'conflict'
  | 'unauthorized'
  | 'forbidden'
  | 'network'
  | 'server';

/** What every failed request turns into: one shape, a Hebrew message, and what the screen needs to act on. */
export interface ApiError {
  kind: ApiErrorKind;
  message: string;
  traceId?: string;
  fieldErrors?: Record<string, string[]>;
  current?: RequestDetails;
  allowedTransitions?: RequestStatus[];
}
