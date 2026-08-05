using System.Text.RegularExpressions;
using Ganss.Xss;
using Markdig;

namespace SMSNet.Services.Assistant;

/// <summary>
/// Turns assistant Markdown into display HTML.
/// <para>
/// Model output is untrusted input: it can quote a web page the model just
/// fetched, and that page can contain a payload. So the pipeline is always
/// render → sanitise → enrich, and the sanitiser allowlist is deliberately
/// narrow (no iframe, no script, no event handlers, no inline styles).
/// </para>
/// </summary>
public sealed partial class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;
    private readonly HtmlSanitizer _sanitizer;

    public MarkdownRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()   // tables, task lists, autolinks, footnotes, strikethrough
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak()
            .DisableHtml()             // raw HTML in model output is never trusted
            .Build();

        _sanitizer = BuildSanitizer();
    }

    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var html = Markdown.ToHtml(markdown, _pipeline);
        var safe = _sanitizer.Sanitize(html);

        return Enrich(safe);
    }

    private static HtmlSanitizer BuildSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "hr", "span", "div",
                     "strong", "em", "del", "ins", "mark", "sub", "sup", "small",
                     "h1", "h2", "h3", "h4", "h5", "h6",
                     "ul", "ol", "li",
                     "blockquote", "pre", "code",
                     "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption",
                     "a", "img", "video", "audio", "source",
                     "details", "summary"
                 })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[]
                 {
                     "href", "title", "alt", "src", "class",
                     "colspan", "rowspan", "align",
                     "controls", "poster", "type", "open"
                 })
        {
            sanitizer.AllowedAttributes.Add(attribute);
        }

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("mailto");

        // No inline styles: a style attribute is enough to cover the page with an
        // invisible overlay, which is a clickjacking primitive.
        sanitizer.AllowedCssProperties.Clear();

        sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is not AngleSharp.Html.Dom.IHtmlAnchorElement anchor)
            {
                return;
            }

            // Model-supplied links go to third-party pages: open them away from the
            // app and cut the opener reference so the target can't navigate us.
            anchor.SetAttribute("target", "_blank");
            anchor.SetAttribute("rel", "noopener noreferrer nofollow");
        };

        return sanitizer;
    }

    /// <summary>
    /// Upgrades media links that Markdown can only express as images, and tags
    /// tables and code blocks so the stylesheet can treat them properly.
    /// </summary>
    private static string Enrich(string html)
    {
        html = VideoImages().Replace(html,
            m => $"<video class=\"sms-md-media\" controls preload=\"metadata\" src=\"{m.Groups["src"].Value}\"></video>");

        html = AudioImages().Replace(html,
            m => $"<audio class=\"sms-md-media\" controls preload=\"metadata\" src=\"{m.Groups["src"].Value}\"></audio>");

        html = html.Replace("<table>", "<div class=\"sms-md-table\"><table>")
                   .Replace("</table>", "</table></div>");

        return html;
    }

    [GeneratedRegex(@"<img[^>]*src=""(?<src>[^""]+\.(?:mp4|webm|ogv))(?:\?[^""]*)?""[^>]*>",
        RegexOptions.IgnoreCase)]
    private static partial Regex VideoImages();

    [GeneratedRegex(@"<img[^>]*src=""(?<src>[^""]+\.(?:mp3|wav|ogg|m4a))(?:\?[^""]*)?""[^>]*>",
        RegexOptions.IgnoreCase)]
    private static partial Regex AudioImages();
}
