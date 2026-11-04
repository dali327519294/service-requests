import { RequestPriority, RequestStatus } from '../api/request-enums';

/** Every Hebrew string of the UI lives here. */
export const labels = {
  appTitle: 'ניהול פניות',
  appSubtitle: 'מערכת לטיפול בפניות ארגונים',
  skipToContent: 'דלג לתוכן הראשי',
  requestsTitle: 'פניות',
  updating: 'מעדכן את הרשימה',
  loadFailed: 'לא הצלחנו לטעון את הפניות',
  noResultsTitle: 'לא נמצאו פניות',
  noResultsBody: 'אין פניות שמתאימות לכל הסינונים שבחרת. אפשר להסיר סינון או לנקות הכל.',
  clearFilters: 'ניקוי כל הסינונים',
  statusFilterGroup: 'סינון לפי סטטוס',
  offlineMessage: 'אין חיבור לשרת. הנתונים שמוצגים עלולים להיות לא עדכניים.',
  retry: 'נסו שוב',
} as const;

export const statusLabel: Record<RequestStatus, string> = {
  New: 'חדשה',
  InProgress: 'בטיפול',
  Waiting: 'ממתינה',
  Completed: 'הושלמה',
};

/** Card titles: how many requests are in each status. */
export const statusPluralLabel: Record<RequestStatus, string> = {
  New: 'חדשות',
  InProgress: 'בטיפול',
  Waiting: 'ממתינות',
  Completed: 'הושלמו',
};

export const priorityLabel: Record<RequestPriority, string> = {
  Low: 'נמוכה',
  Medium: 'בינונית',
  High: 'גבוהה',
};

/** The browser tab title for a page. */
export const pageTitle = (page: string): string => `${page} | ${labels.appTitle}`;
