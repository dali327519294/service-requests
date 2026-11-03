import { RequestPriority, RequestStatus } from '../api/request-enums';

/** Every Hebrew string of the UI lives here. */
export const labels = {
  appTitle: 'ניהול פניות',
  appSubtitle: 'מערכת לטיפול בפניות ארגונים',
  skipToContent: 'דלג לתוכן הראשי',
  requestsTitle: 'פניות',
  requestsEmptyTitle: 'רשימת הפניות תוצג כאן',
  requestsEmptyBody: 'החיפוש, הסינון והטבלה נבנים בשלב הבא.',
} as const;

export const statusLabel: Record<RequestStatus, string> = {
  New: 'חדשה',
  InProgress: 'בטיפול',
  Waiting: 'ממתינה',
  Completed: 'הושלמה',
};

export const priorityLabel: Record<RequestPriority, string> = {
  Low: 'נמוכה',
  Medium: 'בינונית',
  High: 'גבוהה',
};

/** The browser tab title for a page. */
export const pageTitle = (page: string): string => `${page} | ${labels.appTitle}`;
