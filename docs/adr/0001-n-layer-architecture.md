# 0001 – ארכיטקטורת שכבות (N-Layer)

**סטטוס:** התקבלה · 01/11/2026

## הקשר
מערכת לניהול פניות: API, לוגיקה עסקית (מעברי סטטוס, Concurrency, Bulk), ובסיס נתונים.
הכללים העסקיים צריכים להיבדק מהר ובנפרד, והמערכת צריכה להיות קלה להבנה לבודק.

## החלטה
ארבעה פרויקטים: `Api → Application → Infrastructure → Domain`.
- **Domain:** ישויות וכללים עסקיים, בלי תלות בשום ספרייה.
- **Infrastructure:** EF Core, ה-DbContext וה-Migrations.
- **Application:** תרחישי השימוש (חיפוש, עדכון, Bulk), עובדת ישירות מול ה-DbContext.
- **Api:** Controllers, טיפול בשגיאות והגדרות.

## חלופות שנשקלו
- **Clean Architecture מלאה** (Interfaces לכל גישה לנתונים, Application לא מכירה את EF Core): יותר הפשטה ממה שמערכת בגודל הזה צריכה.
- **Repository Pattern:** ה-DbContext של EF Core כבר מספק Repository ו-Unit of Work. שכבה נוספת הייתה מסתירה יכולות כמו `AsNoTracking` והטלה ל-DTO.
- **פרויקט אחד:** פשוט יותר, אבל הכללים העסקיים מתערבבים עם הגישה לנתונים.

## השלכות
- ה-Domain נבדק ב-Unit Tests מהירים, בלי בסיס נתונים.
- Application תלויה ב-EF Core. החלפת ORM תדרוש שינוי בה. זה מחיר מקובל, כי אין כוונה להחליף.
- הבדיקות של Application הן Integration Tests מול SQL Server אמיתי, ולא Mocks.
