using Ganss.Xss;

namespace MedicalStoreERP.Services
{
    public interface IHtmlSanitizerService
    {
        string Sanitize(string? html);
    }

    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizerService()
        {
            _sanitizer = new HtmlSanitizer();

            // Allow common formatting tags
            _sanitizer.AllowedTags.Add("h1");
            _sanitizer.AllowedTags.Add("h2");
            _sanitizer.AllowedTags.Add("h3");
            _sanitizer.AllowedTags.Add("h4");
            _sanitizer.AllowedTags.Add("h5");
            _sanitizer.AllowedTags.Add("h6");
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("hr");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("b");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("i");
            _sanitizer.AllowedTags.Add("u");
            _sanitizer.AllowedTags.Add("s");
            _sanitizer.AllowedTags.Add("strike");
            _sanitizer.AllowedTags.Add("blockquote");
            _sanitizer.AllowedTags.Add("pre");
            _sanitizer.AllowedTags.Add("code");
            _sanitizer.AllowedTags.Add("ul");
            _sanitizer.AllowedTags.Add("ol");
            _sanitizer.AllowedTags.Add("li");
            _sanitizer.AllowedTags.Add("a");
            _sanitizer.AllowedTags.Add("img");
            _sanitizer.AllowedTags.Add("table");
            _sanitizer.AllowedTags.Add("thead");
            _sanitizer.AllowedTags.Add("tbody");
            _sanitizer.AllowedTags.Add("tr");
            _sanitizer.AllowedTags.Add("th");
            _sanitizer.AllowedTags.Add("td");
            _sanitizer.AllowedTags.Add("div");
            _sanitizer.AllowedTags.Add("span");
            _sanitizer.AllowedTags.Add("figure");
            _sanitizer.AllowedTags.Add("figcaption");
            _sanitizer.AllowedTags.Add("sub");
            _sanitizer.AllowedTags.Add("sup");

            // Allow safe attributes
            _sanitizer.AllowedAttributes.Add("class");
            _sanitizer.AllowedAttributes.Add("style");
            _sanitizer.AllowedAttributes.Add("href");
            _sanitizer.AllowedAttributes.Add("src");
            _sanitizer.AllowedAttributes.Add("alt");
            _sanitizer.AllowedAttributes.Add("title");
            _sanitizer.AllowedAttributes.Add("width");
            _sanitizer.AllowedAttributes.Add("height");
            _sanitizer.AllowedAttributes.Add("target");
            _sanitizer.AllowedAttributes.Add("rel");
            _sanitizer.AllowedAttributes.Add("colspan");
            _sanitizer.AllowedAttributes.Add("rowspan");
            _sanitizer.AllowedAttributes.Add("id");

            // Allow safe CSS properties for inline styles
            _sanitizer.AllowedCssProperties.Add("color");
            _sanitizer.AllowedCssProperties.Add("background-color");
            _sanitizer.AllowedCssProperties.Add("font-size");
            _sanitizer.AllowedCssProperties.Add("font-family");
            _sanitizer.AllowedCssProperties.Add("font-weight");
            _sanitizer.AllowedCssProperties.Add("font-style");
            _sanitizer.AllowedCssProperties.Add("text-align");
            _sanitizer.AllowedCssProperties.Add("text-decoration");
            _sanitizer.AllowedCssProperties.Add("margin");
            _sanitizer.AllowedCssProperties.Add("margin-top");
            _sanitizer.AllowedCssProperties.Add("margin-bottom");
            _sanitizer.AllowedCssProperties.Add("margin-left");
            _sanitizer.AllowedCssProperties.Add("margin-right");
            _sanitizer.AllowedCssProperties.Add("padding");
            _sanitizer.AllowedCssProperties.Add("padding-top");
            _sanitizer.AllowedCssProperties.Add("padding-bottom");
            _sanitizer.AllowedCssProperties.Add("padding-left");
            _sanitizer.AllowedCssProperties.Add("padding-right");
            _sanitizer.AllowedCssProperties.Add("border");
            _sanitizer.AllowedCssProperties.Add("width");
            _sanitizer.AllowedCssProperties.Add("height");
            _sanitizer.AllowedCssProperties.Add("max-width");
            _sanitizer.AllowedCssProperties.Add("line-height");
            _sanitizer.AllowedCssProperties.Add("list-style-type");

            // Allow only safe URL schemes
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("mailto");
        }

        public string Sanitize(string? html)
        {
            if (string.IsNullOrEmpty(html))
                return html ?? string.Empty;

            return _sanitizer.Sanitize(html);
        }
    }
}
