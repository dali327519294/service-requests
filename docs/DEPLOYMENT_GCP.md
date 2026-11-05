# פריסה ל-Google Cloud

המסמך מתאר איך המערכת הייתה רצה ב-GCP. **הפריסה לא בוצעה בפועל**: הקוד מוכן לה, והשינויים הנדרשים מפורטים כאן.

## הרכיבים

```
משתמש ─► Cloud Load Balancing + Cloud CDN
            ├─ /        ─► Cloud Storage  (הממשק: קבצים סטטיים מ-ng build)
            └─ /api/*   ─► Cloud Run      (ה-API, קונטיינר)
                              ├─ Cloud SQL for SQL Server  (חיבור פרטי, VPC)
                              ├─ Memorystore for Redis     (כשיש יותר ממופע אחד)
                              └─ Secret Manager            (מחרוזת החיבור)
```

| רכיב | שירות | למה |
|---|---|---|
| API | **Cloud Run** | קונטיינר בלי ניהול שרתים, גדל ומצטמצם לפי עומס. `/health` משמש כבדיקת מוכנות (Startup Probe) |
| בסיס נתונים | **Cloud SQL for SQL Server** | אותו מנוע כמו בפיתוח, כולל `rowversion` והאינדקסים. גיבויים אוטומטיים וזמינות גבוהה |
| ממשק | **Cloud Storage + Cloud CDN** | הממשק הוא קבצים סטטיים. אותו דומיין כמו ה-API דרך ה-Load Balancer, כך שאין צורך ב-CORS, כמו ב-Proxy של הפיתוח |
| Cache משותף | **Memorystore for Redis** | רק כשיש יותר ממופע אחד של ה-API (ראו "Cache") |
| סודות | **Secret Manager** | מחרוזת החיבור לא נמצאת בקוד ולא בקונטיינר. Cloud Run מעביר אותה כמשתנה סביבה `ConnectionStrings__ServiceRequestsDb` |

## מה משתנה בקוד לפני פריסה

- **Dockerfile** ל-API: בנייה ב-`mcr.microsoft.com/dotnet/sdk:10.0`, הרצה ב-`mcr.microsoft.com/dotnet/aspnet:10.0`.
- **`Database:InitializeOnStartup=false` בייצור.** בפיתוח ה-API מריץ Migrations ו-Seed כשהוא עולה. בייצור כמה מופעים עולים יחד, ונתוני דמו לא רצויים. ה-Migrations רצות כשלב נפרד ב-Pipeline (למטה).
- **ההתחברות.** לפני חשיפה למשתמשים אמיתיים צריך התחברות במקום הכותרת `X-User-Name` ([ADR 0007](adr/0007-no-authentication-in-this-version.md)). ב-GCP: Identity Platform, או ה-IdP הארגוני, עם JWT.

## Cache

במופע אחד, ה-HybridCache שבזיכרון מספיק. כשיש כמה מופעים של Cloud Run, לכל מופע יש Cache ומספר דור משלו, ומופע אחד לא יודע שמופע אחר עדכן פנייה. המעבר:
1. Memorystore for Redis כשכבה שנייה של HybridCache. זה שינוי הגדרה ב-`Program.cs` (`AddStackExchangeRedisCache`).
2. מונה הדור עובר ל-Redis (`INCR`), כדי שכל המופעים יראו את אותו דור ([ADR 0006](adr/0006-hybridcache-generation-key.md)).

## Pipeline של CD (GitHub Actions)

ה-CI הקיים (בנייה ובדיקות של השרת והממשק) נשאר שער הכניסה. אחרי Merge ל-`main`:

1. **אימות מול GCP** בלי מפתחות שמורים: Workload Identity Federation בין GitHub ל-GCP.
2. **בניית קונטיינר** ל-API ודחיפה ל-Artifact Registry, מתויג לפי ה-commit.
3. **Migrations:** EF Core Migration Bundle (`dotnet ef migrations bundle`) רץ כ-Cloud Run Job מול Cloud SQL. אם הוא נכשל, הפריסה נעצרת.
4. **פריסת ה-API** לגרסה חדשה של Cloud Run, בלי תנועה, ובדיקת `/health`.
5. **העברת התנועה** לגרסה החדשה. החזרה לגרסה הקודמת היא פקודה אחת.
6. **הממשק:** `ng build` והעלאה ל-Cloud Storage, ואז ניקוי ה-CDN.

Migrations נכתבות כך שהגרסה הקודמת של ה-API ממשיכה לעבוד מול הסכמה החדשה (קודם מוסיפים, ורק בפריסה הבאה מוחקים), כדי שהחזרה לגרסה הקודמת תהיה בטוחה.

## ניטור

- **לוגים:** ה-API כותב לוגים מובנים, ו-Cloud Run מעביר אותם ל-Cloud Logging. כל שגיאת שרת מחזירה `traceId`, שהממשק מציג כקוד לתמיכה, וכך אפשר למצוא את הבקשה בלוג.
- **מדדים והתראות:** Cloud Monitoring על זמני תגובה, שיעור שגיאות 5xx ועומס על Cloud SQL.
