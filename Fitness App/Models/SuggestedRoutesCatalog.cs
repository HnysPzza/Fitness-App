namespace Fitness_App.Models;

/// <summary>Metro Cebu area seeds (lng/lat). Filtered at runtime by user position and <see cref="Services.CebuMapRegion"/>.</summary>
public static class SuggestedRoutesCatalog
{
    /// <summary>Malls, viewpoints, trail/hill access, coastal and bike-friendly points — all within region bbox.</summary>
    public static readonly SuggestedRouteSeed[] Seeds =
    [
        // Well-known city / malls
        new("cebu-waterfront", "Waterfront Cebu City", 123.902, 10.292),
        new("ayala-cebu", "Ayala Center Cebu", 123.905, 10.318),
        new("sm-seaside", "SM Seaside City", 123.898, 10.282),
        new("sm-city-cebu", "SM City Cebu", 123.898, 10.311),
        new("it-park-cebu", "Cebu IT Park", 123.906, 10.329),
        new("fuente-osmena", "Fuente Osmeña Circle", 123.896, 10.298),
        new("cebu-cathedral", "Cebu Metropolitan Cathedral", 123.902, 10.295),
        new("magellans-cross", "Magellan's Cross & Basilica", 123.906, 10.294),
        new("colon-obras", "Colon Street / Downtown", 123.899, 10.293),

        // Hills, trails, hiking / viewpoint style (walking)
        new("temple-of-leah", "Temple of Leah (Busay)", 123.884, 10.342),
        new("taoist-temple", "Cebu Taoist Temple", 123.885, 10.355),
        new("tops-lookout", "Tops Lookout (Busay)", 123.888, 10.365),
        new("sirao-garden", "Sirao Flower Garden", 123.861, 10.385),
        new("mountain-view-park", "Mountain View Nature Park", 123.857, 10.379),
        new("la-vie-sky", "La Vie in the Sky (Busay)", 123.862, 10.378),
        new("chocolate-hills-cebu", "Chocolate Hills View (Sirao)", 123.858, 10.388),
        new("budlaan-trailhead", "Budlaan / river trail access", 123.872, 10.395),
        new("transcentral-scenic", "Transcentral Hwy scenic pull-off", 123.845, 10.402),

        // Coastal, Mactan, bike-friendly segments (cycling profile)
        new("mactan-shrine", "Mactan Shrine / Lapu-Lapu", 123.958, 10.263, "cycling"),
        new("cebu-cordova-bridge", "CCLEX / Cordova side approach", 123.947, 10.252, "cycling"),
        new("mactan-newtown", "Mactan Newtown", 123.965, 10.275, "cycling"),
        new("maribago-beach-road", "Maribago coastal road", 123.973, 10.285, "cycling"),
        new("southern-coastal-cebu", "South Road Properties coast", 123.878, 10.265, "cycling"),
        new("talisay-boardwalk", "Talisay coastal area", 123.833, 10.245, "cycling"),
        new("liloan-light", "Liloan lighthouse / coast", 123.998, 10.400, "cycling"),

        // Parks, trails, open space (walking)
        new("plaza-independencia", "Plaza Independencia", 123.903, 10.292),
        new("mango-square", "Mango Square / Gen. Maxilom", 123.908, 10.305),
        new("cebu-business-park", "Cebu Business Park", 123.911, 10.325),
        new("waterfront-drive", "Waterfront Drive loop", 123.900, 10.288),
        new("banilad-town", "Banilad Town Center area", 123.912, 10.345),
        new("as-fortuna", "A.S. Fortuna bike corridor", 123.918, 10.338, "cycling"),
    ];
}
