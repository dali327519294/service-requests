import { ParamMap, Params } from '@angular/router';
import { RequestQuery, SortDirection, SortField } from '../../../core/api/models';
import { REQUEST_PRIORITIES, REQUEST_STATUSES } from '../../../core/api/request-enums';

export const PAGE_SIZES = [20, 50, 100] as const;
const SORT_FIELDS: readonly SortField[] = [
  'CreatedAt',
  'UpdatedAt',
  'Priority',
  'Status',
  'OrganizationName',
];
const SORT_DIRECTIONS: readonly SortDirection[] = ['Asc', 'Desc'];

/** What the list shows when the URL says nothing. Defaults are left out of the URL, so links stay short. */
export const DEFAULTS = {
  sortBy: 'CreatedAt',
  sortDirection: 'Desc',
  page: 1,
  pageSize: 20,
} as const;

/**
 * Reads the list state from the URL. Anything invalid falls back to the default,
 * so an old or hand-edited link never breaks the page.
 */
export function toQuery(params: ParamMap): RequestQuery {
  const text = (key: string) => params.get(key)?.trim() || undefined;
  const pageSize = Number(params.get('pageSize'));
  const page = Number(params.get('page'));
  const from = parseDay(params.get('createdFrom'));
  const to = parseDay(params.get('createdTo'));

  return {
    search: text('search'),
    status: listOf(params.getAll('status'), REQUEST_STATUSES),
    priority: listOf(params.getAll('priority'), REQUEST_PRIORITIES),
    organizationName: text('organizationName'),
    assignedTo: text('assignedTo'),
    createdFrom: from,
    // The URL keeps the last day the user chose; the API wants the first moment after it (half-open range)
    createdTo: to && addDays(to, 1),
    sortBy: oneOf(params.get('sortBy'), SORT_FIELDS) ?? DEFAULTS.sortBy,
    sortDirection: oneOf(params.get('sortDirection'), SORT_DIRECTIONS) ?? DEFAULTS.sortDirection,
    page: Number.isInteger(page) && page >= 1 ? page : DEFAULTS.page,
    pageSize: (PAGE_SIZES as readonly number[]).includes(pageSize) ? pageSize : DEFAULTS.pageSize,
  };
}

/** Writes the list state to the URL, leaving out empty values and defaults. */
export function toParams(query: RequestQuery): Params {
  const params: Params = {
    search: query.search || undefined,
    status: query.status?.length ? [...query.status] : undefined,
    priority: query.priority?.length ? [...query.priority] : undefined,
    organizationName: query.organizationName || undefined,
    assignedTo: query.assignedTo || undefined,
    createdFrom: query.createdFrom && formatDay(query.createdFrom),
    createdTo: query.createdTo && formatDay(addDays(query.createdTo, -1)),
    sortBy: query.sortBy === DEFAULTS.sortBy ? undefined : query.sortBy,
    sortDirection: query.sortDirection === DEFAULTS.sortDirection ? undefined : query.sortDirection,
    page: query.page === DEFAULTS.page ? undefined : query.page,
    pageSize: query.pageSize === DEFAULTS.pageSize ? undefined : query.pageSize,
  };
  return Object.fromEntries(Object.entries(params).filter(([, value]) => value !== undefined));
}

/** The filters alone: what the summary depends on (paging and sorting do not change the counts). */
export function filtersOf(query: RequestQuery): RequestQuery {
  return {
    search: query.search,
    status: query.status,
    priority: query.priority,
    organizationName: query.organizationName,
    assignedTo: query.assignedTo,
    createdFrom: query.createdFrom,
    createdTo: query.createdTo,
  };
}

export const sameQuery = (a: RequestQuery, b: RequestQuery) =>
  JSON.stringify(toParams(a)) === JSON.stringify(toParams(b));

function listOf<T extends string>(values: string[], allowed: readonly T[]): T[] | undefined {
  const valid = [...new Set(values)].filter((v): v is T =>
    (allowed as readonly string[]).includes(v),
  );
  return valid.length ? valid : undefined;
}

function oneOf<T extends string>(value: string | null, allowed: readonly T[]): T | undefined {
  return (allowed as readonly (string | null)[]).includes(value) ? (value as T) : undefined;
}

// A calendar day in the user's own time zone: "2026-10-01" is midnight of that day in Israel
function parseDay(value: string | null): Date | undefined {
  const match = value?.match(/^(\d{4})-(\d{2})-(\d{2})$/);
  if (!match) {
    return undefined;
  }

  const [year, month, day] = match.slice(1).map(Number);
  const date = new Date(year, month - 1, day);
  return date.getDate() === day ? date : undefined; // rejects 2026-02-31
}

function formatDay(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}
