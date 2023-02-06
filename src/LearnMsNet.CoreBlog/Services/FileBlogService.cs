using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LearnMsNet.CoreBlog.Services;

public class FileBlogService : IBlogService
{
    private const string POSTS = "Data\\Posts";
    private const string FILES = "files";
    private readonly string _folder;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly List<PostItem> _posts = new();

    public FileBlogService(
        IWebHostEnvironment env,
        IHttpContextAccessor contextAccessor)
    {
        if (env is null)
        {
            throw new ArgumentNullException(nameof(env));
        }
        _folder = Path.Combine(env.WebRootPath, POSTS);
        _contextAccessor = contextAccessor;
        Initialize();
    }

    private void Initialize()
    {
        LoadPosts();
        SortPosts();
    }

    private void SortPosts()
    {
        throw new NotImplementedException();
    }

    private void LoadPosts()
    {
        if (!Directory.Exists(_folder))
        {
            Directory.CreateDirectory(_folder);
        }
        foreach (var file in Directory.EnumerateFiles(_folder, "*.xml", SearchOption.TopDirectoryOnly))
        {
            //using StreamReader reader = new(file);
            //string json = reader.ReadToEnd();
            //var post = JsonSerializer.Deserialize<PostItem>(json);
            //if (post != null)
            //{
            //    _posts.Add(post);
            //}
            var doc = XElement.Load(file);
            var post = new PostItem
            {
                Id = new Guid(Path.GetFileNameWithoutExtension(file)),
                Title = ReadValue(doc, "title"),
                Excerpt = ReadValue(doc, "excerpt"),
                Content = ReadValue(doc, "content"),
                Slug = ReadValue(doc, "slug").ToLowerInvariant(),
                PublishDate = DateTime.Parse(ReadValue(doc, "pubDate"), CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal),
                LastModified = DateTime.Parse(
                        ReadValue(
                            doc,
                            "lastModified",
                            DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
                        CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                IsPublished = bool.Parse(ReadValue(doc, "ispublished", "true")),
            };

            LoadCategories(post, doc);
            LoadTags(post, doc);
            LoadComments(post, doc);
            _posts.Add(post);
        }
    }

    public Task DeletePost(PostItem post)
    {
        if (post is null)
        {
            throw new ArgumentNullException(nameof(post));
        }

        var filePath = GetFilePath(post);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        if (_posts.Contains(post))
        {
            _posts.Remove(post);
        }

        return Task.CompletedTask;
    }

    public virtual IAsyncEnumerable<string> GetCategories()
    {
        var isAdmin = IsAdmin();

        return _posts
            .Where(p => p.IsPublished || isAdmin)
            .SelectMany(post => post.Categories)
            .Select(cat => cat.ToLowerInvariant())
            .Distinct()
            .ToAsyncEnumerable();
    }

    public virtual Task<PostItem?> GetPostById(string id)
    {
        var isAdmin = IsAdmin();
        var post = _posts.FirstOrDefault(p => p.Id.ToString().Equals(id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(
            post is null || post.PublishDate > DateTime.UtcNow || (!post.IsPublished && !isAdmin)
            ? null
            : post);
    }

    public virtual Task<PostItem?> GetPostBySlug(string slug)
    {
        var isAdmin = IsAdmin();
        var post = _posts.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(
            post is null || post.PublishDate > DateTime.UtcNow || (!post.IsPublished && !isAdmin)
            ? null
            : post);
    }

    public virtual IAsyncEnumerable<PostItem> GetPosts()
    {
        var isAdmin = IsAdmin();

        var posts = _posts
            .Where(p => p.PublishDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
            .ToAsyncEnumerable();

        return posts;
    }

    public virtual IAsyncEnumerable<PostItem> GetPosts(
        int count,
        int skip = 0)
    {
        var isAdmin = IsAdmin();

        var posts = _posts
            .Where(p => p.PublishDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
            .Skip(skip)
            .Take(count)
            .ToAsyncEnumerable();

        return posts;
    }

    public virtual IAsyncEnumerable<PostItem> GetPostsByCategory(string category)
    {
        var isAdmin = IsAdmin();

        var posts = from p in _posts
                    where p.PublishDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                    where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                    select p;

        return posts.ToAsyncEnumerable();
    }

    public virtual IAsyncEnumerable<PostItem> GetPostsByTag(string tag)
    {
        var isAdmin = IsAdmin();

        var posts = from p in _posts
                    where p.PublishDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                    where p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)
                    select p;

        return posts.ToAsyncEnumerable();
    }

    public virtual IAsyncEnumerable<string> GetTags()
    {
        var isAdmin = IsAdmin();

        return _posts
            .Where(p => p.IsPublished || isAdmin)
            .SelectMany(post => post.Tags)
            .Select(tag => tag.ToLowerInvariant())
            .Distinct()
            .ToAsyncEnumerable();
    }

    public async Task<string> SaveFile(
        byte[] bytes,
        string fileName,
        string? suffix = null)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        suffix = CleanFromInvalidChars(suffix ?? DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));

        var ext = Path.GetExtension(fileName);
        var name = CleanFromInvalidChars(Path.GetFileNameWithoutExtension(fileName));

        var fileNameWithSuffix = $"{name}_{suffix}{ext}";

        var absolute = Path.Combine(_folder, FILES, fileNameWithSuffix);
        var dir = Path.GetDirectoryName(absolute);

        Directory.CreateDirectory(dir);
        using (var writer = new FileStream(absolute, FileMode.CreateNew))
        {
            await writer.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        return $"/{POSTS}/{FILES}/{fileNameWithSuffix}";
    }

    public async Task SavePost(PostItem post)
    {
        if (post is null)
        {
            throw new ArgumentNullException(nameof(post));
        }

        var filePath = GetFilePath(post);
        post.LastModified = DateTime.UtcNow;

        var doc = new XDocument(
            new XElement("post",
                new XElement("title", post.Title),
                new XElement("slug", post.Slug),
                new XElement("pubDate", FormatDateTime(post.PublishDate)),
                new XElement("lastModified", FormatDateTime(post.LastModified)),
                new XElement("excerpt", post.Excerpt),
                new XElement("content", post.Content),
                new XElement("ispublished", post.IsPublished),
                new XElement("categories", string.Empty),
                new XElement("tags", string.Empty),
                new XElement("comments", string.Empty)
            ));

        var categories = doc.XPathSelectElement("post/categories");
        foreach (var category in post.Categories)
        {
            categories.Add(new XElement("category", category));
        }

        var tags = doc.XPathSelectElement("post/tags");
        foreach (var tag in post.Tags)
        {
            tags.Add(new XElement("tag", tag));
        }

        var comments = doc.XPathSelectElement("post/comments");
        foreach (var comment in post.Comments)
        {
            comments.Add(
                new XElement("comment",
                    new XElement("author", comment.Author),
                    new XElement("email", comment.Email),
                    new XElement("date", FormatDateTime(comment.PublishDate)),
                    new XElement("content", comment.Content),
                    new XAttribute("isAdmin", comment.IsAdmin),
                    new XAttribute("id", comment.Id)
                ));
        }

        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
            await doc.SaveAsync(fs, SaveOptions.None, CancellationToken.None).ConfigureAwait(false);
        }

        if (!_posts.Contains(post))
        {
            _posts.Add(post);
            SortPosts();
        }
    }

    protected bool IsAdmin() => _contextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    private string GetFilePath(PostItem post) => Path.Combine(_folder, $"{post.Id}.xml");
    private static string ReadAttribute(XElement element, XName name, string defaultValue = "") =>
            element.Attribute(name) is null ? defaultValue : element.Attribute(name)?.Value ?? defaultValue;
    private static string ReadValue(XElement doc, XName name, string defaultValue = "") =>
        doc.Element(name) is null ? defaultValue : doc.Element(name)?.Value ?? defaultValue;

    private static void LoadComments(
        PostItem post,
        XElement doc)
    {
        var comments = doc.Element("comments");

        if (comments is null)
        {
            return;
        }

        foreach (var node in comments.Elements("comment"))
        {
            var comment = new CommentItem
            {
                Id = new Guid(ReadAttribute(node, "id")),
                Author = ReadValue(node, "author"),
                Email = ReadValue(node, "email"),
                IsAdmin = bool.Parse(ReadAttribute(node, "isAdmin", "false")),
                Content = ReadValue(node, "content"),
                PublishDate = DateTime.Parse(ReadValue(node, "date", "2000-01-01"),
                    CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
            };

            post.Comments.Add(comment);
        }
    }

    private static void LoadTags(
        PostItem post,
        XElement doc)
    {
        var tags = doc.Element("tags");
        if (tags is null)
        {
            return;
        }

        post.Tags.Clear();
        tags.Elements("tag").Select(node => node.Value).ToList().ForEach(post.Tags.Add);
    }

    private static void LoadCategories(
        PostItem post,
        XElement doc)
    {
        var categories = doc.Element("categories");
        if (categories is null)
        {
            return;
        }

        post.Categories.Clear();
        categories.Elements("category").Select(node => node.Value).ToList().ForEach(post.Categories.Add);
    }

    private static string CleanFromInvalidChars(string input)
    {
        // ToDo: what we are doing here if we switch the blog from windows to unix system or
        // vice versa? we should remove all invalid chars for both systems

        var regexSearch = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
        var r = new Regex($"[{regexSearch}]");
        return r.Replace(input, string.Empty);
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        const string UTC = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

        return dateTime.Kind == DateTimeKind.Utc
            ? dateTime.ToString(UTC, CultureInfo.InvariantCulture)
            : dateTime.ToUniversalTime().ToString(UTC, CultureInfo.InvariantCulture);
    }
}
