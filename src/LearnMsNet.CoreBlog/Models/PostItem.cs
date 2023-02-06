using LearnMsNet.CoreBlog.Config;

using System.Net;
using System.Text.RegularExpressions;

namespace LearnMsNet.CoreBlog.Models;

public class PostItem
{
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;


    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public string Excerpt { get; set; } = string.Empty;

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string Slug { get; set; } = string.Empty;


    public IList<string> Categories { get; } = new List<string>();

    public IList<string> Tags { get; } = new List<string>();

    public IList<CommentItem> Comments { get; } = new List<CommentItem>();
    public bool IsPublished { get; set; } = true;
    public DateTime PublishDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public bool AreCommentsOpen(int commentsCloseAfterDays) =>
            this.PublishDate.AddDays(commentsCloseAfterDays) >= DateTime.UtcNow;

    public string GetEncodedLink() => $"/blog/{WebUtility.UrlEncode(this.Slug)}/";

    public string GetLink() => $"/blog/{this.Slug}/";

    public bool IsVisible() => this.PublishDate <= DateTime.UtcNow && this.IsPublished;

    public static string CreateSlug(string title)
    {
        title = title?.ToLowerInvariant().Replace(
            BlogConstants.SPACE, BlogConstants.DASH, StringComparison.OrdinalIgnoreCase) ?? string.Empty;
        title = RemoveDiacritics(title);
        title = RemoveReservedUrlCharacters(title);

        return title.ToLowerInvariant();
    }

    public string RenderContent()
    {
        var result = this.Content;

        // Set up lazy loading of images/iframes
        if (!string.IsNullOrEmpty(result))
        {
            // Set up lazy loading of images/iframes
            var replacement = " src=\"data:image/gif;base64,R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==\" data-src=\"";
            var pattern = "(<img.*?)(src=[\\\"|'])(?<src>.*?)([\\\"|'].*?[/]?>)";
            result = Regex.Replace(result, pattern, m => m.Groups[1].Value + replacement + m.Groups[4].Value + m.Groups[3].Value);

            // Youtube content embedded using this syntax: [youtube:xyzAbc123]
            var video = "<div class=\"video\"><iframe width=\"560\" height=\"315\" title=\"YouTube embed\" src=\"about:blank\" data-src=\"https://www.youtube-nocookie.com/embed/{0}?modestbranding=1&amp;hd=1&amp;rel=0&amp;theme=light\" allowfullscreen></iframe></div>";
            result = Regex.Replace(
                result,
                @"\[youtube:(.*?)\]",
                m => string.Format(CultureInfo.InvariantCulture, video, m.Groups[1].Value));
        }

        return result;
    }


    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string RemoveReservedUrlCharacters(string text)
    {
        var reservedCharacters = new List<string> { "!", "#", "$", "&", "'", "(", ")", "*", ",", "/", ":", ";", "=", "?", "@", "[", "]", "\"", "%", ".", "<", ">", "\\", "^", "_", "'", "{", "}", "|", "~", "`", "+" };

        foreach (var chr in reservedCharacters)
        {
            text = text.Replace(chr, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }
}
