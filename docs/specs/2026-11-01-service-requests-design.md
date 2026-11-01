# Service Requests – מסמך אפיון טכני (Design Spec)

**תאריך:** 2026-11-01 · **סטטוס:** טיוטה לאישור · **מקור הדרישות:** מבדק Full Stack – מערכת לניהול פניות

מסמך זה מתאר **מה** בונים ו**למה**. סדר הביצוע, המשימות והערכות הזמן נמצאים בתוכנית המימוש (מסמך נפרד).
מספרי הסעיפים בסוגריים מרובעים, למשל [§2], מפנים לסעיפי המבדק.

---

## 1. מטרה והיקף

מערכת לניהול פניות שמתקבלות מארגונים ומעסיקים, לעבודה של כמה משתמשים במקביל על לפחות 100,000 פניות. המשתמשים מאתרים פניות, מסננים, ממיינים, צופים בפרטים, מעדכנים סטטוס (בודד או מרוכז), צופים בהיסטוריית שינויים ובנתונים מסכמים.

**עקרונות מנחים:**
1. **כל פעולת נתונים מתבצעת בבסיס הנתונים.** הלקוח אף פעם לא מסנן, ממיין או מדפדף בעצמו.
2. **כללים עסקיים נאכפים בשרת ובבסיס הנתונים**, וה-UI רק משקף אותם.
3. **כל טענה בתיעוד מגובה בבדיקה או במדידה.**
4. **פשוט, נכון ומנומק.** אין שכבה, ספרייה או תשתית בלי דרישה שמצדיקה אותן.

**מחוץ להיקף (מתועד כשיפור עתידי):** הרשמה עצמית, שחזור סיסמה, Refresh Tokens, פריסה בפועל לענן, Full-Text Search, Keyset Pagination, Redis בפועל, API Versioning.

---

## 2. טכנולוגיות וגרסאות

| שכבה | טכנולוגיה | נימוק |
|---|---|---|
| שרת | .NET 10 (LTS), ASP.NET Core Web API עם Controllers | LTS עד 2028. .NET 8 יוצאת מתמיכה בנובמבר 2026 |
| גישה לנתונים | EF Core 10 | `rowversion` מובנה, LINQ שמתורגם ל-SQL |
| בסיס נתונים | SQL Server – LocalDB בפיתוח, SQL Server 2022 בקונטיינר ב-CI | Concurrency, טרנזקציות ו-Execution Plan מובנים |
| Cache | HybridCache (Microsoft.Extensions.Caching.Hybrid) | הגנת Stampede מובנית, ומסלול ל-Redis בשינוי הגדרה |
| לקוח | Angular (הגרסה היציבה העדכנית), Angular Material, RxJS, Signals | ספריית הרכיבים הרשמית; תמיכה ב-RTL ובנגישות |
| בדיקות שרת | xUnit, `WebApplicationFactory` מול SQL Server אמיתי | ספק InMemory לא אוכף `rowversion` |
| בדיקות לקוח | כלי הבדיקות שמגיע כברירת מחדל עם גרסת Angular | |
| נתוני בדיקה | `SqlBulkCopy` עם Seed קבוע | שניות במקום דקות, ונתונים זהים בכל הרצה |
| CI | GitHub Actions | חינמי ל-Repository ציבורי |
| איכות קוד | Nullable, Analyzers עם `TreatWarningsAsErrors`, Central Package Management, `.editorconfig`, ESLint, Prettier | |

הגרסה של ה-SDK נקבעת ב-`global.json`.

---

## 3. מבנה הפתרון (N-Layer)

```
src/
  ServiceRequests.Domain/          ישויות, Enums, StatusTransitions – ללא תלויות
  ServiceRequests.Infrastructure/  DbContext, קונפיגורציה ואינדקסים, Migrations, Seeder
  ServiceRequests.Application/     Services, DTOs, Validation, Cache, ICurrentUser
  ServiceRequests.Api/             Controllers, טיפול גלובלי בשגיאות, Auth, הרכבת DI
  service-requests-ui/             Angular
tests/
  ServiceRequests.UnitTests/
  ServiceRequests.IntegrationTests/
tools/
  ServiceRequests.LoadTests/       (Stretch) NBomber
docs/
```

**כיוון התלויות:** `Api → Application → Infrastructure → Domain`, ו-`Application → Domain`. ההפרדה נאכפת על ידי הקומפיילר: פרויקט Api לא מכיר את Infrastructure, ולכן Controller לא יכול לגשת ל-DbContext.

**בלי Repository Pattern:** ה-DbContext כבר מממש Repository ו-Unit of Work. ה-Services ב-Application משתמשים בו ישירות.

**חלופה שנשקלה – Clean Architecture:** היתרונות שלה (החלפת DB ובדיקות עם Mock) לא מתממשים כאן. אין צורך להחליף DB, ו-Mock ל-DbContext מריץ LINQ בזיכרון ולא ב-SQL, כך שבדיקות מולו לא מוכיחות את מה שחשוב במבדק הזה. היא מתאימה למערכת גדולה וארוכת חיים.

**Lifetimes:** Scoped – `DbContext`, ה-Services, `ICurrentUser`. Singleton – `HybridCache`, `TimeProvider`, ה-Generation Counter של ה-Cache.

---

## 4. מודל הנתונים [§1, §2, §3]

### `Requests`
| עמודה | טיפוס | הערות |
|---|---|---|
| `Id` | `int IDENTITY`, PK Clustered | מספר עולה: בלי Fragmentation, וקטן פי 4 מ-`Guid` |
| `Title` | `nvarchar(200) NOT NULL` | |
| `OrganizationName` | `nvarchar(200) NOT NULL` | |
| `Status` | `tinyint NOT NULL` + CHECK | `New=1, InProgress=2, Waiting=3, Completed=4` – ערכים מפורשים |
| `Priority` | `tinyint NOT NULL` + CHECK | `Low=1, Medium=2, High=3` |
| `AssignedTo` | `nvarchar(100) NULL` | `NULL` = עוד לא שובצה |
| `CreatedAt` | `datetime2(3) NOT NULL` | UTC |
| `UpdatedAt` | `datetime2(3) NOT NULL` | UTC; מתעדכן בכל שינוי סטטוס |
| `RowVersion` | `rowversion` | Concurrency Token; מתעדכן על ידי SQL Server |

### `RequestStatusHistory` (Audit, Append-only)
`Id bigint IDENTITY PK`, `RequestId int FK → Requests`, `PreviousStatus tinyint`, `NewStatus tinyint`, `ChangedAt datetime2(3)` (UTC), `ChangedBy nvarchar(100)`.
שורות נוספות בלבד – אין עדכון או מחיקה.

### `Users`
`Id int IDENTITY PK`, `UserName nvarchar(50) UNIQUE`, `DisplayName nvarchar(100)`, `PasswordHash nvarchar(200)`, `Role tinyint` + CHECK (`Handler=1, Manager=2`).
`Requests.AssignedTo` מכיל `UserName` של מטפלים מה-Seed (ללא FK, כי המבדק מגדיר את השדה כ"מזהה/שם מטפל" ולא מחייב ישות משתמשים).

אורכי השדות מוגדרים במקום אחד (מחלקת קבועים ב-Domain) ומשמשים גם את הסכמה וגם את ה-Validation.

### אינדקסים (השערה; הסט הסופי ייקבע במדידה [§6])
| אינדקס | תרחיש |
|---|---|
| `IX_Requests_CreatedAt` `(CreatedAt DESC)` | ברירת המחדל: החדשות קודם, וטווח תאריכים |
| `IX_Requests_Status_CreatedAt` `(Status, CreatedAt DESC) INCLUDE (Priority)` | סינון לפי סטטוס ממוין לפי תאריך, **וגם** Covering לסיכום (ספירה לפי סטטוס ועדיפות) |
| `IX_Requests_AssignedTo_Status` `(AssignedTo, Status)` | "הפניות שלי" |
| `IX_Requests_OrganizationName` `(OrganizationName)` | סינון לפי תחילת שם ארגון – Seek |
| `IX_RequestStatusHistory_RequestId_ChangedAt` `(RequestId, ChangedAt DESC)` | היסטוריה של פנייה |

**השערה לבדיקה:** Filtered Index על פניות פתוחות (`Status <> 4`). יאומץ רק אם המדידה תראה תועלת.

---

## 5. נתוני בדיקה (Seed) [נתונים]

- **100,000 פניות** כברירת מחדל; מספר אחר בפרמטר.
- **`SqlBulkCopy`** לטעינה, ו-**Seed אקראי קבוע** – כל הרצה יוצרת בדיוק אותם נתונים, כך שהמדידות ניתנות לשחזור.
- **התפלגות מציאותית:** סטטוסים לא אחידים (למשל 10% New, 40% InProgress, 15% Waiting, 35% Completed), כ-200 ארגונים, כ-30 מטפלים, תאריכים על פני שנתיים; היסטוריה עקבית עם הסטטוס הנוכחי.
- **נתונים בדיוניים בעברית** – שמות ארגונים, כותרות ומטפלים – שמתאימים לממשק.
- **משתמשים לדוגמה** מכל תפקיד, עם סיסמאות שמתועדות ב-README (לפיתוח בלבד).
- **הרצה:** בהרצה הראשונה, אם הטבלה ריקה – Migrations ו-Seed אוטומטית. יצירה חוזרת: `dotnet run --project src/ServiceRequests.Api -- seed --reset [count]`.
- **אחרי הטעינה:** בדיקת מצב האינדקסים (Fragmentation) – ממצא יתועד ויטופל אם צריך.

---

## 6. חוזה ה-API [§1–§4]

כל ה-Endpoints תחת `/api`, דורשים JWT, חוץ מ-`/api/auth/login` ו-`/health`.

| Method | כתובת | תיאור | קודים |
|---|---|---|---|
| `POST` | `/api/auth/login` | התחברות; מחזיר JWT, תפקיד ושם תצוגה | 200, 400, 401 |
| `GET` | `/api/requests` | רשימה: חיפוש, סינון, מיון, דפדוף | 200, 400, 401 |
| `GET` | `/api/requests/summary` | Aggregations (Faceted) לאותם סינונים | 200, 400, 401 |
| `GET` | `/api/requests/{id}` | פנייה + `rowVersion` + `allowedTransitions` | 200, 401, 404 |
| `PATCH` | `/api/requests/{id}/status` | עדכון סטטוס `{ status, rowVersion }` | 200, 400, 401, 404, 409 |
| `GET` | `/api/requests/{id}/history` | היסטוריה, מהחדש לישן | 200, 401, 404 |
| `POST` | `/api/requests/bulk-status` | עד 100 פניות; **Manager בלבד** | 200, 400, 401, 403 |
| `GET` | `/health` | השרת ובסיס הנתונים זמינים | 200, 503 |

### פרמטרי חיפוש (רשימה וסיכום; משולבים ב-AND)
| פרמטר | משמעות | Validation |
|---|---|---|
| `search` | "מכיל" ב-`Title` או ב-`OrganizationName` | עד 100 תווים; תווים מיוחדים של `LIKE` (`%`, `_`, `[`) עוברים Escape |
| `status` | אחד או יותר | ערכים קיימים בלבד |
| `priority` | אחד או יותר | ערכים קיימים בלבד |
| `organizationName` | מתחיל ב- | עד 200 תווים |
| `assignedTo` | התאמה מדויקת | עד 100 תווים |
| `createdFrom`, `createdTo` | טווח **חצי-פתוח** `[from, to)` | `from < to`; ערכים עם Offset, מומרים ל-UTC |
| `sortBy` | `createdAt`, `updatedAt`, `priority`, `status`, `organizationName` | Whitelist |
| `sortDirection` | `asc`, `desc` | ברירת מחדל `desc` |
| `page`, `pageSize` | (רשימה בלבד) | `page ≥ 1`; `pageSize` 1–100, ברירת מחדל 20 |

**טווח חצי-פתוח:** טווח סגור מפספס את מה שנוצר ביום האחרון אחרי חצות. הלקוח שולח את תחילת היום לפי שעון ישראל עם Offset, והשרת ממיר ל-UTC.

### צורות תשובה
- **רשימה:** `{ items, page, pageSize, totalCount, totalPages }`. כל פריט הוא DTO (לא ישות), שמוטל (Projection) בתוך השאילתה.
- **פנייה:** כל השדות + `rowVersion` (Base64) + `allowedTransitions`.
- **סיכום:** `{ total, byStatus: {...}, byPriority: {...} }`, מחושב כ-**Faceted Counts**: הספירה לפי סטטוס מתעלמת מסינון הסטטוס בלבד, והספירה לפי עדיפות מתעלמת מסינון העדיפות בלבד. כך כרטיסי הסיכום הלחיצים תמיד מציגים את כל הקטגוריות.
- **Enums** מוחזרים כשמות (`"InProgress"`) בכל תשובה, כולל גוף של שגיאה.
- **תאריכים** ב-ISO 8601 UTC.

### שגיאות
- כל שגיאה מוחזרת כ-**ProblemDetails** (RFC 9457) דרך `IExceptionHandler`, עם `traceId`.
- 400 של Validation כולל את השדות הבעייתיים.
- 500 – פרטים מלאים בלוג, ללקוח רק הודעה כללית ו-`traceId`.

### CancellationToken
עובר מה-Controller עד כל שאילתת EF. בקשה שהלקוח ביטל מבטלת את השאילתה ב-SQL Server ונרשמת בלוג כ-Information.

---

## 7. מעברי סטטוס [§2]

```
New → InProgress
InProgress → Waiting | Completed
Waiting → InProgress
Completed → (סופי)
```

- הכללים במחלקה אחת, `StatusTransitions`, ב-Domain. ממנה נגזרים גם האכיפה וגם `allowedTransitions` בתשובות – **Single Source of Truth**.
- **Completed סופי** – החלטה עסקית מכוונת.
- סטטוס לא קיים, מעבר אסור, או מעבר לאותו סטטוס → **400** עם הסבר.

---

## 8. עדכון סטטוס ו-Optimistic Concurrency [§2, §3]

**הגרסה עוברת בגוף הבקשה** (`rowVersion`), גם בעדכון בודד וגם ב-Bulk – מנגנון אחד לכל המערכת. ETag/If-Match נשקל ונדחה: ב-Bulk הגרסאות ממילא חייבות להיות בגוף, ושני מנגנונים מקבילים פוגעים בעקביות.

### זרימה
1. Validation של הבקשה (סטטוס קיים, `rowVersion` קיים ותקין) → 400.
2. טעינת הפנייה → 404 אם לא קיימת.
3. **השוואת גרסה מוקדמת** → 409 עם המצב העדכני. נבדקת לפני המעבר, כי מעבר שנבדק מול מצב ישן נותן שגיאה מטעה.
4. בדיקת מעבר → 400.
5. עדכון `Status`, `UpdatedAt` (מ-`TimeProvider`), והוספת שורת היסטוריה עם `ChangedBy` מ-`ICurrentUser`.
6. **`SaveChanges` אחד** – טרנזקציה אחת ל-`UPDATE ... WHERE Id = @id AND RowVersion = @v` ול-`INSERT` של ההיסטוריה. `DbUpdateConcurrencyException` → 409. זו רשת הביטחון לחלון שבין שלב 3 לשלב 6 (TOCTOU).
7. **אחרי ה-Commit:** Invalidation ל-Cache ורישום בלוג. תשובה 200 עם הפנייה המעודכנת והגרסה החדשה.

### תשובת 409
ProblemDetails עם `current`: הפנייה במצבה העדכני, כולל `rowVersion` ו-`allowedTransitions`. הלקוח יכול להציג למשתמש את המצב ולאפשר ניסיון חוזר בלי `GET` נוסף. מי שינה ומתי – מה-Endpoint של ההיסטוריה.

---

## 9. Bulk Update [§4]

**Partial Success:** כל פנייה מקבלת תוצאה משלה. All-or-Nothing נדחה: במערכת עם הרבה משתמשים, פנייה אחת עם התנגשות הייתה חוסמת עשרות פניות תקינות.

### זרימה
1. **Validation של הבקשה כולה:** 1–100 פריטים, בלי כפילויות, סטטוס יעד קיים, `rowVersion` לכל פריט → 400, ושום דבר לא מתעדכן.
2. **טעינת כל הפניות בשאילתה אחת.**
3. **סיווג בזיכרון:** `NotFound` · `Conflict` (גרסה ישנה) · `InvalidTransition` · תקינה.
4. **שמירה מרוכזת:** כל הפניות התקינות ושורות ההיסטוריה שלהן ב-`SaveChanges` אחד (Batching). אם פנייה השתנתה בין הקריאה לכתיבה, הטרנזקציה מתבטלת, ה-Exception מזהה את הפנייה, היא מסומנת `Conflict`, מוצאת מה-Change Tracker, ונשמרות שוב השאר. כל סבב מוציא לפחות פנייה אחת, ולכן הלולאה מסתיימת תמיד.
5. Invalidation אחד ל-Cache אם עודכנה לפחות פנייה אחת; שורת לוג עם סיכום.

### תשובה – 200 (גם כשאף פריט לא הצליח)
```json
{ "requested": 4, "succeeded": 1, "failed": 3,
  "results": [
    { "id": 12, "outcome": "Updated", "rowVersion": "AAAAAAAAB9E=" },
    { "id": 45, "outcome": "NotFound", "error": "..." },
    { "id": 78, "outcome": "Conflict", "error": "..." },
    { "id": 90, "outcome": "InvalidTransition", "error": "..." } ] }
```
התוצאות באותו סדר כמו בבקשה. `207 Multi-Status` נדחה – שייך ל-WebDAV ולקוחות רבים לא מטפלים בו.

**הרשאה:** Manager בלבד → 403 ל-Handler.

---

## 10. Cache [§7]

- **מה נשמר:** הסיכום **ללא סינון** בלבד – המסך שכל משתמש פותח, והחישוב היקר ביותר (כל הטבלה). סיכומים מסוננים מחושבים בכל בקשה: אינסוף צירופים, Hit Rate נמוך.
- **לא נשמרת הרשימה:** משתמש מעדכן לפי מה שהוא רואה; רשימה ישנה הייתה מייצרת עוד התנגשויות.
- **מימוש:** `HybridCache`, עם **Cache Stampede Protection** מובנית (כשהערך חסר, רק בקשה אחת מחשבת).
- **Expiration:** 60 שניות (ניתן להגדרה) – רשת ביטחון.
- **Invalidation:** אחרי Commit של כל שינוי סטטוס (בודד או Bulk).
- **מרוץ בין חישוב לעדכון:** **Generation Key** – המפתח כולל מונה (`summary:v{n}`). כל Invalidation מעלה את המונה; חישוב שהתחיל לפני העדכון כותב לדור ישן שאף אחד כבר לא קורא ממנו.
- **Scale-out:** Redis כשכבה שנייה של HybridCache (ב-GCP – Memorystore) בשינוי הגדרה, והמונה עובר ל-Redis (`INCR`) כדי שכל המופעים יראו אותו דור. מתועד, לא ממומש.

---

## 11. אימות והרשאות

- **JWT Bearer:** `[Authorize]` כברירת מחדל על כל ה-Controllers. 401 בלי טוקן תקין.
- **Login:** טבלת `Users`; סיסמאות נשמרות עם `PasswordHasher` המובנה של ASP.NET Core. טוקן בתוקף שעה, עם Claims של שם משתמש ותפקיד.
- **Roles:** `Handler` – צפייה ועדכון בודד. `Manager` – בנוסף Bulk. 403 למשתמש בלי הרשאה.
- **`ICurrentUser`:** מחזיר את המשתמש מה-Claims, ומשמש את ה-Audit. עד שלב ההתחברות (ראו סדר העבודה), מימוש זמני קורא Header; ההחלפה היא מחלקה אחת.
- **מפתח החתימה:** לא נמצא ב-Git. בפיתוח – User Secrets; ב-CI – GitHub Secret.
- **בייצור:** אותו מנגנון מתחבר ל-IdP ארגוני בשינוי הגדרה.

---

## 12. Logging ואבחון

- Structured Logging עם `[LoggerMessage]` (Source Generator).
- **Information:** עדכון סטטוס (מזהה, סטטוס קודם וחדש), סיכום Bulk, **409** (התנגשות היא אירוע עסקי צפוי, לא תקלה), בקשה שבוטלה.
- **Warning:** 400 של Validation – שמות השדות בלבד, בלי ערכים.
- **Error:** חריגה לא צפויה, פעם אחת, עם `traceId`.
- **לא נרשמים:** סיסמאות, טוקנים, גופי בקשות, Connection Strings.
- `traceId` מובנה של ASP.NET Core מקשר בין התשובה ללקוח לבין הלוג.

---

## 13. Angular [§5]

### מבנה
```
src/app/
  core/       api (HTTP בלבד) · auth (AuthService, authGuard, Login) · http (Interceptors) · i18n
  features/requests/
    list/     RequestsListPage (Smart) · SummaryCards · RequestFilters · RequestsTable · BulkActionBar
    details/  RequestDetailsPage – Route /requests/:id
    dialogs/  ConflictDialog · BulkResultDialog
```

### ניהול מצב הרשימה
- **ה-URL הוא מקור האמת:** כל שינוי (חיפוש, סינון, מיון, דפדוף) מעדכן Query Params; רענון, קישור ו-Back מציגים אותה תצוגה.
- **`RequestsSearchState`** – Service ברמת הרכיב: מאזין ל-URL → `switchMap` לבקשה (בקשה קודמת מבוטלת, גם בשרת) → Signals: `items`, `loading`, `error`, `empty`.
- **Debounce** של 300ms ו-`distinctUntilChanged` בשדה החיפוש בלבד.
- **Smart/Dumb:** רק עמוד הרשימה מדבר עם ה-State; שאר הרכיבים עם `input()`/`output()` ו-`OnPush`.

### מסכים והתנהגות
- **רשימה:** כרטיסי סיכום לחיצים (Faceted), טופס סינון (Typed Reactive Form), סינונים פעילים כ-Chips להסרה, `MatTable` + `MatSort` + `MatPaginator` שרק שולחים אירועים לשרת. מצבי Loading / Empty / Error.
- **פנייה (Route נפרד):** פרטים, כפתורים רק למעברים ב-`allowedTransitions`, טבלת היסטוריה. Back חוזר לרשימה עם אותם סינונים.
- **409:** דיאלוג – מה המצב עכשיו, מי שינה ומתי, מה לא נשמר. "הצג מצב עדכני", ו"נסי שוב" רק אם המעבר עדיין מותר.
- **Bulk:** בחירת שורות; סרגל פעולה רק ל-Manager; דיאלוג תוצאות עם סיבה וקישור לכל פנייה שנכשלה.

### Interceptors
- **Auth:** מוסיף טוקן; 401 → מסך Login.
- **Connection:** שגיאת רשת → באנר אחד. `GET` מנוסה שוב אוטומטית עם Exponential Backoff; `PATCH`/`POST` לא (לא Idempotent).
- **Errors:** ProblemDetails → הודעה בעברית (שדה לא תקין, מעברים מותרים); ב-500 מוצג `traceId` כקוד לתמיכה.

### עברית ונגישות (ת"י 5568 / WCAG 2.0 AA)
RTL (`dir="rtl"`), תרגום רכיבי Material (Paginator, Datepicker), ניווט מלא במקלדת, `aria-label` לכפתורים ואייקונים, `LiveAnnouncer` להודעות ("נמצאו N פניות", "הסטטוס עודכן"), סטטוס שלא מוצג רק בצבע. בדיקת Lighthouse נשמרת כראיה.

---

## 14. ביצועים [§6]

**שתי הפעולות שנמדדות:** `GET /api/requests` ו-`GET /api/requests/summary`; בנוסף העדכון המותנה.

**שיטה:**
1. לכידת ה-SQL ש-EF Core מייצר מהלוג.
2. `SET STATISTICS IO, TIME` ו-Actual Execution Plan, **לפני ואחרי** כל אינדקס.
3. ראיות ב-Repository: קבצי `.sqlplan`, סקריפט מדידה שניתן להריץ שוב, טבלת סיכום ב-`docs/PERFORMANCE.md`.
4. זמני תגובה של ה-API.
5. בדיקת Fragmentation אחרי ה-Seed, ובדיקת השערת ה-Filtered Index.

**צווארי בקבוק צפויים (לאימות במדידה):** חיפוש `LIKE '%x%'` (לא משתמש באינדקס – הצעה: Full-Text Index); `COUNT(*)` בכל בקשה; `OFFSET` בעמודים עמוקים (הצעה: Keyset Pagination).

**Stretch:** בדיקת עומס עם NBomber – כ-20 משתמשים במקביל, תרחיש מעורב, p50/p95, מספר 409 תחת עומס.

---

## 15. אסטרטגיית בדיקות [§8]

כל טענה בתיעוד מגובה בבדיקה. נכתבות לפי TDD, יחד עם הקוד.

**Unit:** `StatusTransitions` (כל מעבר מותר ואסור), Validation של פרמטרי החיפוש ושל בקשת Bulk.

**Integration (מול SQL Server, DB נפרד לכל מחלקת בדיקות):**
- חיפוש, סינון, מיון, דפדוף; חיפוש בעברית; Escape של תווים מיוחדים; טווח תאריכים חצי-פתוח עם אזור זמן.
- Faceted Counts.
- עדכון: 200 + שורת היסטוריה עם `ChangedBy` נכון; 404; 400 למעבר אסור.
- **Concurrency:**
  1. מרוץ **דטרמיניסטי** בעדכון בודד – שינוי מוכרח בין הקריאה לכתיבה → 409, שום דבר לא נדרס, שורת היסטוריה אחת.
  2. אותו מרוץ ב-Bulk – רק הפנייה שהשתנתה נכשלת.
  3. אין התנגשויות שווא בעדכון מקביל של פניות שונות.
  4. Bulk ועדכון בודד על אותן פניות – מנצח אחד ושורת היסטוריה אחת לכל פנייה.
- Bulk – Partial Success עם כל סוגי התוצאות; 400 לבקשה לא תקינה.
- Cache – Hit, Invalidation, ומרוץ בין חישוב לעדכון.
- Auth – 401 בלי טוקן, 403 ל-Handler ב-Bulk.

**Frontend:** Debounce (בקשה אחת), ביטול בקשה קודמת, דיאלוג 409, מצבי UI, סנכרון URL, תרגום שגיאות.

**אימות הבדיקות:** בבדיקות הקריטיות (Concurrency, מרוץ ב-Cache, Debounce) המנגנון מוסר זמנית, ומוודאים שהבדיקה נכשלת.

**CI:** שתי קבוצות הבדיקות ובדיקות האיכות רצות בכל PR; Branch Protection על `main`.

---

## 16. תיעוד והגשה [§9, §11]

| מסמך | תוכן |
|---|---|
| `README.md` | מה המערכת, הרצה מהירה, איך בודקים, החלטות מרכזיות, מגבלות, קישורים |
| `docs/REVIEWER_GUIDE.md` | Traceability Matrix (סעיף → איך רואים → קוד → בדיקה → תיעוד), 3–4 צילומי מסך, פתרון תקלות |
| `requests.http` | בקשות לפי סעיפי המבדק, עם תשובות אמיתיות כהערות |
| `docs/adr/` | ADR לכל החלטה מרכזית |
| `docs/PERFORMANCE.md` | מדידות, אינדקסים, צווארי בקבוק |
| `docs/DEPLOYMENT_GCP.md` | Cloud Run, Cloud SQL, Memorystore, Secret Manager, ו-Pipeline של CD |
| `docs/WORK_PLAN.md` | משימות, תלויות, Critical Path, סיכום מאמץ, הערכה מול בפועל |
| `docs/AI_USAGE.md` | מה הכלי עשה / מה הוחלט / מה נבדק, ורשימת בעיות שנמצאו ותוקנו |

**מגבלות** מוצגות בטבלה: השפעה שנמדדה, תיקון מוצע, הערכת זמן.
**שתי ההחלטות המרכזיות ב-README:** Optimistic Concurrency עם `rowversion` בגוף הבקשה, ו-Cache עם Generation Key.

---

## 17. תהליך עבודה

- משימה = GitHub Issue; יום עבודה = Milestone.
- ענף לכל משימה (`feature/...`, `docs/...`), commit-ים קטנים בציווי, PR עם `Closes #N`, Self-review, Code Review לפני Merge של Feature גדול, Merge.
- CI חייב לעבור לפני Merge.
