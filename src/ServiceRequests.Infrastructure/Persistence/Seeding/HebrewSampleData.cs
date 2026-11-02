using System.Globalization;

namespace ServiceRequests.Infrastructure.Persistence.Seeding;

/// <summary>Fictional Hebrew demo data, combined into many distinct values from short lists.</summary>
internal static class HebrewSampleData
{
    private static readonly string[] Places =
    [
        "ירושלים", "תל אביב", "חיפה", "באר שבע", "אשדוד", "נתניה", "ראשון לציון", "פתח תקווה", "חולון", "רמת גן",
        "אשקלון", "רחובות", "בת ים", "כפר סבא", "הרצליה", "חדרה", "מודיעין", "נצרת", "לוד", "רמלה",
        "רעננה", "עכו", "אילת", "טבריה", "צפת", "קריית שמונה", "דימונה", "ערד", "בית שמש", "קריית גת",
        "נהריה", "כרמיאל", "עפולה", "יבנה", "אור יהודה", "נס ציונה", "גבעתיים", "הוד השרון", "ראש העין", "שדרות",
    ];

    private static readonly string[] OrganizationTypes =
        ["מפעל המתכת {0}", "מחלבת {0}", "מוסך {0}", "מכון הטיהור {0}", "אתר הבנייה {0}"];

    private static readonly string[] Subjects =
    [
        "תלונה על זיהום אוויר", "בקשה להיתר פליטה", "תלונה על רעש ממפעל", "פסולת בנייה בשטח פתוח",
        "בדיקת איכות מים", "חידוש רישיון עסק", "ריח חריף מאזור תעשייה", "בקשה לפינוי אסבסט",
        "דיווח על שפכים בנחל", "בקשה לסקר קרינה", "שריפת פסולת בשטח פתוח", "דליפת דלק ממיכל",
        "השלכת פסולת ביתית", "עשן שחור מארובה", "בקשה לאישור תוכנית מחזור", "תלונה על אבק מאתר בנייה",
        "דיווח על חומרים מסוכנים", "בקשה לבדיקת קרקע", "רעש מגנרטור בלילה", "זיהום חוף ים",
    ];

    /// <summary>200 organizations: 5 types × 40 places.</summary>
    public static readonly string[] Organizations =
        [.. OrganizationTypes.SelectMany(type => Places.Select(place => string.Format(CultureInfo.InvariantCulture, type, place)))];

    /// <summary>800 titles: 20 subjects × 40 places.</summary>
    public static readonly string[] Titles = [.. Subjects.SelectMany(subject => Places.Select(place => $"{subject} – {place}"))];

    /// <summary>30 handler user names; Task 15 creates users with the same names.</summary>
    public static readonly string[] Handlers =
    [
        "dana.levi", "yossi.cohen", "michal.mizrahi", "avi.peretz", "noa.biton", "eitan.friedman", "shira.azulay",
        "omer.katz", "tamar.dahan", "roni.avraham", "yael.shapira", "moshe.ohana", "liat.ben-david", "gil.malka",
        "hila.gabay", "daniel.segal", "maya.amar", "itay.golan", "efrat.vaknin", "nir.hadad", "orly.sasson",
        "amit.rosen", "keren.elkayam", "tal.zohar", "ronit.levy", "boaz.yosef", "galit.harel", "ido.barak",
        "sarit.cohen", "eyal.mor",
    ];
}
