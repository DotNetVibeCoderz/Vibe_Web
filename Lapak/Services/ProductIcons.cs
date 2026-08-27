namespace Lapak.Services;

/// <summary>
/// Maps a category name to the icon and weave hue used wherever a product,
/// category, or store is drawn.
/// </summary>
/// <remarks>
/// Kept in one place so a product looks identical on the card, the detail page,
/// the cart line, and the wishlist. Previously each page carried its own copy of
/// the mapping and they drifted apart.
/// </remarks>
public static class ProductIcons
{
    /// <summary>Icon key understood by <c>GlyphIcon</c>.</summary>
    public static string For(string? categoryName) => (categoryName ?? "").ToLowerInvariant() switch
    {
        var c when c.Contains("smartphone") || c.Contains("ponsel") || c.Contains("tablet") => "ponsel",
        var c when c.Contains("laptop") || c.Contains("komputer") => "laptop",
        var c when c.Contains("audio") || c.Contains("headphone") => "audio",
        var c when c.Contains("kamera") => "kamera",
        var c when c.Contains("pakaian") || c.Contains("fashion") || c.Contains("kaos") || c.Contains("baju") => "kaos",
        var c when c.Contains("sepatu") => "sepatu",
        var c when c.Contains("tas") => "tas",
        var c when c.Contains("jam") || c.Contains("aksesoris") => "jam",
        var c when c.Contains("furniture") || c.Contains("meja") || c.Contains("kursi") => "kursi",
        var c when c.Contains("dapur") || c.Contains("masak") => "wajan",
        var c when c.Contains("skincare") || c.Contains("makeup") || c.Contains("kecantikan") || c.Contains("parfum") => "kecantikan",
        var c when c.Contains("makanan") || c.Contains("snack") || c.Contains("kue") => "makanan",
        var c when c.Contains("minuman") || c.Contains("kopi") || c.Contains("teh") => "minuman",
        var c when c.Contains("buku") || c.Contains("alat tulis") => "buku",
        var c when c.Contains("olahraga") || c.Contains("fitness") || c.Contains("sepeda") => "olahraga",
        var c when c.Contains("otomotif") || c.Contains("mobil") || c.Contains("motor") => "mobil",
        var c when c.Contains("hobi") || c.Contains("koleksi") || c.Contains("mainan") || c.Contains("gaming") => "gamepad",
        var c when c.Contains("rumah") || c.Contains("kehidupan") || c.Contains("taman") => "rumah",
        var c when c.Contains("elektronik") => "elektronik",
        _ => "kotak"
    };

    /// <summary>
    /// Hue for the woven tile behind the icon. Falls back to a stable hash of the
    /// slug so uncategorised items still differ from one another instead of
    /// collapsing into one colour.
    /// </summary>
    public static int HueFor(string? categoryName, string? slug) => (categoryName ?? "").ToLowerInvariant() switch
    {
        var c when c.Contains("smartphone") || c.Contains("ponsel") || c.Contains("tablet") => 212,
        var c when c.Contains("laptop") || c.Contains("komputer") => 198,
        var c when c.Contains("audio") || c.Contains("headphone") => 258,
        var c when c.Contains("kamera") => 232,
        var c when c.Contains("pakaian") || c.Contains("fashion") => 338,
        var c when c.Contains("sepatu") => 16,
        var c when c.Contains("tas") => 32,
        var c when c.Contains("jam") || c.Contains("aksesoris") => 246,
        var c when c.Contains("furniture") || c.Contains("meja") || c.Contains("kursi") => 28,
        var c when c.Contains("dapur") || c.Contains("masak") => 8,
        var c when c.Contains("skincare") || c.Contains("makeup") || c.Contains("kecantikan") => 322,
        var c when c.Contains("makanan") || c.Contains("snack") => 42,
        var c when c.Contains("minuman") || c.Contains("kopi") => 24,
        var c when c.Contains("buku") => 168,
        var c when c.Contains("olahraga") || c.Contains("fitness") => 136,
        var c when c.Contains("otomotif") => 204,
        var c when c.Contains("hobi") || c.Contains("koleksi") => 152,
        var c when c.Contains("rumah") || c.Contains("kehidupan") => 30,
        var c when c.Contains("elektronik") => 212,
        _ => StableHue(slug)
    };

    /// <summary>Deterministic hue from any string, so the same item is always the same colour.</summary>
    public static int StableHue(string? seed)
    {
        if (string.IsNullOrEmpty(seed)) return 220;

        var hash = 0;
        foreach (var ch in seed) hash = unchecked(hash * 31 + ch);
        return Math.Abs(hash) % 360;
    }
}
