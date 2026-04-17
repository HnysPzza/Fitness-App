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
        new("plaza-independencia", "Plaza Independencia, Cebu City", 123.903, 10.292),
        new("cebu-business-park", "Cebu Business Park", 123.911, 10.325),
        new("banilad-town", "Banilad Town Center", 123.912, 10.345),
        new("waterfront-drive", "Waterfront Drive, Cebu City", 123.900, 10.288),
        new("mango-square", "Mango Square, Cebu City", 123.908, 10.305),

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
        new("buwakan-ni-alejandra", "Buwakan ni Alejandra, Balamban", 123.737, 10.484, "driving"),
        new("west-35", "West 35 Eco Mountain Resort, Balamban", 123.724, 10.474, "driving"),
        new("jvr-island", "JVR Island in the Sky, Balamban", 123.730, 10.483, "driving"),
        new("balamban-town", "Balamban Municipal Plaza", 123.713, 10.503, "cycling"),

        // Coastal, Mactan, bike-friendly segments (cycling profile)
        new("mactan-shrine", "Mactan Shrine / Lapu-Lapu", 123.958, 10.263, "cycling"),
        new("cebu-cordova-bridge", "CCLEX / Cordova side approach", 123.947, 10.252, "cycling"),
        new("mactan-newtown", "Mactan Newtown", 123.965, 10.275, "cycling"),
        new("maribago-beach-road", "Maribago coastal road", 123.973, 10.285, "cycling"),
        new("southern-coastal-cebu", "South Road Properties coast", 123.878, 10.265, "cycling"),
        new("talisay-boardwalk", "Talisay coastal area", 123.833, 10.245, "cycling"),
        new("liloan-light", "Liloan lighthouse / coast", 123.998, 10.400, "cycling"),
        new("naga-boardwalk", "Naga Boardwalk, Cebu", 123.760, 10.210, "cycling"),
        new("cordova-boardwalk", "Cordova Boardwalk", 123.946, 10.252, "cycling"),
        new("ten-thousand-roses", "10,000 Roses Cafe, Cordova", 123.951, 10.256, "cycling"),
        new("cclex-view", "CCLEX Cordova Viewpoint", 123.942, 10.248, "cycling"),
        new("papa-kits", "Papa Kit's Marina and Fishing Lagoon, Liloan", 124.006, 10.401, "cycling"),
        new("sogod-baywalk", "Sogod Baywalk, Cebu", 124.036, 10.751, "cycling"),
        new("as-fortuna", "A.S. Fortuna bike corridor", 123.918, 10.338, "cycling"),

        // Parks, trails, open space (walking)
        new("cebu-safari", "Cebu Safari and Adventure Park, Carmen", 124.017, 10.594, "driving"),
        new("durano-eco-farm", "Durano Eco Farm, Carmen", 123.975, 10.592, "driving"),
        new("danasan-park", "Danasan Eco Adventure Park, Danao", 124.038, 10.633, "driving"),
        new("catmon-beach", "Catmon New Beach and Caves", 124.014, 10.717, "driving"),
        new("esoy-hot-spring", "Esoy Hot Spring, Catmon", 123.968, 10.744, "driving"),
        new("simala-shrine", "Simala Shrine, Sibonga", 123.610, 10.040, "driving"),
        new("carcar-rotunda", "Carcar Rotunda, Carcar", 123.640, 10.110, "cycling"),
        new("bojo-river", "Bojo River, Aloguinsan", 123.470, 10.220, "driving"),
        new("hermits-cove", "Hermit's Cove, Aloguinsan", 123.440, 10.200, "driving"),
        new("mantayupan-falls", "Mantayupan Falls, Barili", 123.540, 10.110, "driving"),
        new("panagsama", "Panagsama Beach, Moalboal", 123.399, 9.945, "driving"),
        new("basdaku", "Basdaku White Beach, Moalboal", 123.356, 9.942, "driving"),
        new("kawasan-falls", "Kawasan Falls, Badian", 123.370, 9.800, "driving"),
        new("badian-canyoneering", "Badian Canyoneering Base", 123.369, 9.801, "driving"),
        new("lambug-beach", "Lambug Beach, Badian", 123.320, 9.870, "driving"),
        new("osmena-peak", "Osmeña Peak, Dalaguete", 123.596, 9.806, "driving"),
        new("kandungaw-peak", "Kandungaw Peak, Dalaguete", 123.610, 9.832, "driving"),
        new("casino-peak", "Casino Peak, Dalaguete", 123.587, 9.809, "driving"),
        new("tingko-beach", "Tingko Beach, Alcoy", 123.478, 9.707, "driving"),
        new("tumalog-falls", "Tumalog Falls, Oslob", 123.428, 9.531, "driving"),
        new("oslob-whale-shark", "Oslob Whale Shark Watching", 123.427, 9.520, "driving"),
        new("sumilon-view", "Sumilon Sandbar Viewpoint, Oslob", 123.394, 9.483, "driving"),
        new("cuartel-oslob", "Cuartel Ruins, Oslob", 123.404, 9.520, "driving"),
    ];
}
