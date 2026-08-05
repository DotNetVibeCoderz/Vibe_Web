using Ganss.Xss;

namespace SMSNet.Services;

/// <summary>
/// Cleans HTML produced by the rich-text editor before it is stored.
/// <para>
/// The editor runs in the browser, so its output is user input like any other:
/// a crafted request can post whatever HTML it likes straight past the toolbar.
/// Sanitising happens on save rather than on display, so a stored payload never
/// exists in the first place.
/// </para>
/// </summary>
public sealed class HtmlContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "hr", "span", "div",
                     "strong", "b", "em", "i", "u", "s", "del", "ins", "mark", "sub", "sup", "small",
                     "h1", "h2", "h3", "h4", "h5", "h6",
                     "ul", "ol", "li",
                     "blockquote", "pre", "code",
                     "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption",
                     "a", "img"
                 })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        _sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "href", "title", "alt", "src", "colspan", "rowspan" })
        {
            _sanitizer.AllowedAttributes.Add(attribute);
        }

        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("mailto");

        // No inline styles and no class: either is enough to cover the page with an
        // invisible overlay, which is a clickjacking primitive.
        _sanitizer.AllowedCssProperties.Clear();

        _sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Html.Dom.IHtmlAnchorElement anchor)
            {
                anchor.SetAttribute("target", "_blank");
                anchor.SetAttribute("rel", "noopener noreferrer");
            }
        };
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var clean = _sanitizer.Sanitize(html);

        // Drop paragraphs with nothing at all in them. Toggling a list leaves these
        // behind, and each one renders as a stray gap. A paragraph the user actually
        // added for spacing contains a <br>, so this cannot eat deliberate blank lines.
        return System.Text.RegularExpressions.Regex.Replace(
            clean, @"<p>\s*</p>", string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// True when the value carries no visible content — an editor left untouched
    /// still emits markup such as "&lt;p&gt;&lt;br&gt;&lt;/p&gt;", which would
    /// otherwise pass a plain empty-string check.
    /// </summary>
    public static bool IsBlank(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return true;
        }

        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty);

        return string.IsNullOrWhiteSpace(
            text.Replace("&nbsp;", " ").Replace(" ", " "));
    }

    /// <summary>
    /// A plain-text rendering, for search, CSV export, and list previews — places
    /// where markup would be noise rather than formatting.
    /// </summary>
    public static string ToPlainText(string? html, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        // Block boundaries become spaces so words either side don't run together.
        var withBreaks = System.Text.RegularExpressions.Regex.Replace(
            html, @"</(p|div|li|h[1-6]|blockquote|pre|tr)\s*>|<br\s*/?>", " ",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var text = System.Text.RegularExpressions.Regex.Replace(withBreaks, "<[^>]+>", string.Empty);

        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

        return maxLength > 0 && text.Length > maxLength ? text[..maxLength] + "…" : text;
    }

    /// <summary>
    /// Presents a legacy plain-text value as HTML.
    /// <para>
    /// Topics written before the editor existed are stored as plain text with real
    /// newlines. Handing those to a renderer verbatim would collapse every line
    /// break, so they are escaped and their newlines promoted to paragraphs.
    /// </para>
    /// </summary>
    public static string FromPlainText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var paragraphs = text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return string.Concat(paragraphs.Select(p => $"<p>{System.Net.WebUtility.HtmlEncode(p)}</p>"));
    }

    /// <summary>Whether a stored value already carries markup, as opposed to being
    /// legacy plain text.</summary>
    public static bool LooksLikeHtml(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && System.Text.RegularExpressions.Regex.IsMatch(value, "<(p|div|ul|ol|h[1-6]|br|strong|em|b|i|u|s|a|blockquote|pre|table)\\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
