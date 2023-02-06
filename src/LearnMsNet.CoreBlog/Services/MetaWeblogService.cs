using LearnMsNet.CoreBlog.Config;

using Microsoft.AspNetCore.Authentication.Cookies;

using System.Security.Claims;

using WilderMinds.MetaWeblog;

namespace LearnMsNet.CoreBlog.Services;

public class MetaWeblogService : IMetaWeblogProvider
{
    private readonly IBlogService _blog;

    private readonly IConfiguration _config;

    private readonly IHttpContextAccessor _context;

    private readonly IUserService _userServices;

    public MetaWeblogService(
        IBlogService blog,
        IConfiguration config,
        IHttpContextAccessor context,
        IUserService userService)
    {
        _blog = blog;
        _config = config;
        _userServices = userService;
        _context = context;
    }

    public Task<int> AddCategoryAsync(
        string key,
        string username,
        string password,
        NewCategory category)
    {
        ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public Task<string> AddPageAsync(
        string blogid,
        string username,
        string password,
        Page page,
        bool publish)
    {
        ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public async Task<string> AddPostAsync(
        string blogid,
        string username,
        string password,
        Post post,
        bool publish)
    {
        ValidateUser(username, password);

        if (post is null)
        {
            throw new ArgumentNullException(nameof(post));
        }

        var newPost = new PostItem
        {
            Title = post.title,
            Slug = !string.IsNullOrWhiteSpace(post.wp_slug) ? post.wp_slug : PostItem.CreateSlug(post.title),
            Excerpt = post.mt_excerpt,
            Content = post.description,
            IsPublished = publish
        };

        post.categories.ToList().ForEach(newPost.Categories.Add);
        post.mt_keywords.Split(',').ToList().ForEach(newPost.Tags.Add);

        if (post.dateCreated != DateTime.MinValue)
        {
            newPost.PublishDate = post.dateCreated;
        }

        await _blog.SavePost(newPost).ConfigureAwait(false);

        return newPost.Id.ToString();
    }

    public Task<bool> DeletePageAsync(
        string blogid,
        string username,
        string password,
        string pageid)
    {
        ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public async Task<bool> DeletePostAsync(
        string key,
        string postid,
        string username,
        string password,
        bool publish)
    {
        ValidateUser(username, password);

        var post = await _blog.GetPostById(postid).ConfigureAwait(false);
        if (post is null)
        {
            return false;
        }

        await _blog.DeletePost(post).ConfigureAwait(false);
        return true;
    }

    public Task<bool> EditPageAsync(
        string blogid,
        string pageid,
        string username,
        string password,
        Page page,
        bool publish)
    {
        ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public async Task<bool> EditPostAsync(
        string postid,
        string username,
        string password,
        Post post,
        bool publish)
    {
        ValidateUser(username, password);

        var existing = await _blog.GetPostById(postid).ConfigureAwait(false);

        if (existing is null || post is null)
        {
            return false;
        }

        existing.Title = post.title;
        existing.Slug = post.wp_slug;
        existing.Excerpt = post.mt_excerpt;
        existing.Content = post.description;
        existing.IsPublished = publish;
        existing.Categories.Clear();
        post.categories.ToList().ForEach(existing.Categories.Add);
        existing.Tags.Clear();
        post.mt_keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(existing.Tags.Add);

        if (post.dateCreated != DateTime.MinValue)
        {
            existing.PublishDate = post.dateCreated;
        }

        await _blog.SavePost(existing).ConfigureAwait(false);

        return true;
    }

    public Task<Author[]> GetAuthorsAsync(
        string blogid,
        string username,
        string password) =>
        throw new NotImplementedException();

    public async Task<CategoryInfo[]> GetCategoriesAsync(
        string blogid,
        string username,
        string password)
    {
        ValidateUser(username, password);

        return await _blog.GetCategories()
            .Select(
                cat =>
                    new CategoryInfo
                    {
                        categoryid = cat,
                        title = cat
                    })
            .ToArrayAsync();
    }

    public Task<Page> GetPageAsync(
        string blogid,
        string pageid,
        string username,
        string password) =>
        throw new NotImplementedException();

    public Task<Page[]> GetPagesAsync(
        string blogid,
        string username,
        string password,
        int numPages) =>
        throw new NotImplementedException();

    public async Task<Post?> GetPostAsync(
        string postid,
        string username,
        string password)
    {
        ValidateUser(username, password);

        var post = await _blog.GetPostById(postid).ConfigureAwait(false);

        return post is null ? null : ToMetaWebLogPost(post);
    }

    public async Task<Post[]> GetRecentPostsAsync(
        string blogid,
        string username,
        string password,
        int numberOfPosts)
    {
        ValidateUser(username, password);

        return await _blog.GetPosts(numberOfPosts)
            .Select(ToMetaWebLogPost)
            .ToArrayAsync();
    }

    public async Task<Tag[]> GetTagsAsync(
        string blogid,
        string username,
        string password)
    {
        ValidateUser(username, password);

        return await _blog.GetTags()
            .Select(
                tag =>
                    new Tag
                    {
                        name = tag
                    })
            .ToArrayAsync();
    }

    public Task<UserInfo> GetUserInfoAsync(
        string key,
        string username,
        string password)
    {
        ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public Task<BlogInfo[]> GetUsersBlogsAsync(
        string key,
        string username,
        string password)
    {
        ValidateUser(username, password);

        var request = _context?.HttpContext?.Request;
        var url = $"{request?.Scheme}://{request?.Host}";

        return Task.FromResult(
            new[]
            {
                    new BlogInfo
                    {
                        blogid ="1",
                        blogName = _config[BlogConstants.Config.Blog.NAME] ?? nameof(MetaWeblogService),
                        url = url
                    }
            });
    }

    public async Task<MediaObjectInfo> NewMediaObjectAsync(
        string blogid,
        string username,
        string password,
        MediaObject mediaObject)
    {
        ValidateUser(username, password);

        if (mediaObject is null)
        {
            throw new ArgumentNullException(nameof(mediaObject));
        }

        var bytes = Convert.FromBase64String(mediaObject.bits);
        var path = await _blog.SaveFile(bytes, mediaObject.name).ConfigureAwait(false);

        return new MediaObjectInfo { url = path };
    }

    private Post ToMetaWebLogPost(PostItem post)
    {
        var request = _context?.HttpContext?.Request;
        var url = $"{request?.Scheme}://{request?.Host}";

        return new Post
        {
            postid = post.Id,
            title = post.Title,
            wp_slug = post.Slug,
            permalink = url + post.GetLink(),
            dateCreated = post.PublishDate,
            mt_excerpt = post.Excerpt,
            description = post.Content,
            categories = post.Categories.ToArray(),
            mt_keywords = string.Join(',', post.Tags)
        };
    }

    private void ValidateUser(string username, string password)
    {
        if (_userServices.ValidateUser(username, password) == false)
        {
            // throw new MetaWeblogException(Properties.Resources.Unauthorized);
            throw new MetaWeblogException("UNauthorized");
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, username));

        _context.HttpContext.User = new ClaimsPrincipal(identity);
    }
}
