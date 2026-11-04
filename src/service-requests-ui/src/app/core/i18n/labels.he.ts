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
  searchPlaceholder: 'חיפוש לפי כותרת או שם ארגון',
  moreFilters: 'סינון נוסף',
  priority: 'עדיפות',
  priorityLevel: 'רמת עדיפות',
  organizationStartsWith: 'שם הארגון',
  assignedTo: 'מטפל/ת',
  createdRange: 'תאריך יצירה',
  activeFilters: 'סינונים פעילים',
  removeFilter: 'הסרת הסינון',
  clearAll: 'ניקוי הכל',
  searchChip: 'חיפוש',
  statusChip: 'סטטוס',
  organizationChip: 'ארגון',
  createdChip: 'נוצרה',
  requestsTable: 'רשימת פניות',
  colId: 'מס׳',
  colTitle: 'כותרת',
  colOrganization: 'ארגון',
  colUpdated: 'עודכנה',
  unassigned: 'לא שויך',
  requestTitle: 'פנייה',
  backToList: 'חזרה לרשימה',
  requestNotFound: 'הפנייה לא נמצאה',
  requestNotFoundBody: 'ייתכן שהקישור שגוי, או שמספר הפנייה לא קיים.',
  requestLoadFailed: 'לא הצלחנו לטעון את הפנייה',
  changeStatus: 'שינוי סטטוס',
  noTransitions: 'אין מעבר זמין מהסטטוס הנוכחי',
  historyTitle: 'היסטוריית סטטוסים',
  noHistory: 'עדיין לא היו שינויי סטטוס',
  changedTo: 'אל',
  statusUpdated: 'הסטטוס עודכן',
  close: 'סגירה',
  conflictTitle: 'הפנייה שונתה בינתיים',
  conflictBody: 'משתמש אחר עדכן את הפנייה אחרי שפתחת אותה, ולכן השינוי שלך לא נשמר.',
  conflictNow: 'הסטטוס עכשיו',
  conflictYouChose: 'ניסית לשנות ל',
  conflictChangedBy: 'עודכנה על ידי',
  conflictShowCurrent: 'הצגת המצב העדכני',
  conflictRetry: 'לשנות בכל זאת ל',
  conflictNoLongerAllowed: (status: string) => `המעבר ל"${status}" כבר לא אפשרי מהסטטוס הנוכחי.`,
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
