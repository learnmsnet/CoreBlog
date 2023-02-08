using Microsoft.Extensions.Options;

using System.Xml;

using WebEssentials.AspNetCore.Pwa;

namespace LearnMsNet.CoreBlog.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBlogService _blog;
        private readonly IOptionsSnapshot<BlogSettings> _settings;
        private readonly WebManifest _manifest;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IBlogService blogService,
            IOptionsSnapshot<BlogSettings> settings,
            WebManifest manifest,
            ILogger<HomeController> logger)
        {
            _blog = blogService;
            _settings = settings;
            _manifest = manifest;
            _logger = logger;
        }

        [Route("/blog/comment/{postId}")]
        [HttpPost]
        public async Task<IActionResult> AddComment(string postId, CommentItem comment)
        {
            var post = await _blog.GetPostById(postId).ConfigureAwait(true);

            if (!this.ModelState.IsValid)
            {
                return this.View(nameof(Post), post);
            }

            if (post is null || !post.AreCommentsOpen(_settings.Value.CommentsCloseAfterDays))
            {
                return this.NotFound();
            }

            if (comment is null)
            {
                throw new ArgumentNullException(nameof(comment));
            }

            comment.IsAdmin = User.Identity.IsAuthenticated;
            comment.Content = comment.Content.Trim();
            comment.Author = comment.Author.Trim();
            comment.Email = comment.Email.Trim();

            // the website form key should have been removed by javascript unless the comment was
            // posted by a spam robot
            if (!Request.Form.ContainsKey("website"))
            {
                post.Comments.Add(comment);
                await _blog.SavePost(post).ConfigureAwait(false);
            }

            return this.Redirect($"{post.GetEncodedLink()}#{comment.Id}");
        }

        [Route("/blog/category/{category}/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Category(string category, int page = 0)
        {
            // get posts for the selected category.
            var posts = _blog.GetPostsByCategory(category);

            // apply paging filter.
            var filteredPosts = posts.Skip(_settings.Value.PostsPerPage * page).Take(_settings.Value.PostsPerPage);

            // set the view option
            this.ViewData["ViewOption"] = _settings.Value.ListView;

            this.ViewData[BlogConstants.TOTALPOSTCOUNT] = await posts.CountAsync().ConfigureAwait(true);
            this.ViewData[BlogConstants.TITLE] = $"{_manifest.Name} {category}";
            this.ViewData[BlogConstants.DESCRIPTION] = $"Articles posted in the {category} category";
            this.ViewData[BlogConstants.PREV] = $"/blog/category/{category}/{page + 1}/";
            this.ViewData[BlogConstants.NEXT] = $"/blog/category/{category}/{(page <= 1 ? null : page - 1 + "/")}";
            return this.View("~/Views/Home/Index.cshtml", filteredPosts.AsAsyncEnumerable());
        }

        [Route("/blog/tag/{tag}/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Tag(string tag, int page = 0)
        {
            // get posts for the selected tag.
            var posts = _blog.GetPostsByTag(tag);

            // apply paging filter.
            var filteredPosts = posts.Skip(_settings.Value.PostsPerPage * page).Take(_settings.Value.PostsPerPage);

            // set the view option
            this.ViewData["ViewOption"] = _settings.Value.ListView;

            this.ViewData[BlogConstants.TOTALPOSTCOUNT] = await posts.CountAsync().ConfigureAwait(true);
            this.ViewData[BlogConstants.TITLE] = $"{_manifest.Name} {tag}";
            this.ViewData[BlogConstants.DESCRIPTION] = $"Articles posted in the {tag} tag";
            this.ViewData[BlogConstants.PREV] = $"/blog/tag/{tag}/{page + 1}/";
            this.ViewData[BlogConstants.NEXT] = $"/blog/tag/{tag}/{(page <= 1 ? null : page - 1 + "/")}";
            return this.View("~/Views/Home/Index.cshtml", filteredPosts.AsAsyncEnumerable());
        }

        [Route("/blog/comment/{postId}/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(string postId, string commentId)
        {
            var post = await _blog.GetPostById(postId).ConfigureAwait(false);

            if (post is null)
            {
                return this.NotFound();
            }

            var comment = post.Comments.FirstOrDefault(c => c.Id.ToString().Equals(commentId, StringComparison.OrdinalIgnoreCase));

            if (comment is null)
            {
                return this.NotFound();
            }

            post.Comments.Remove(comment);
            await _blog.SavePost(post).ConfigureAwait(false);

            return this.Redirect($"{post.GetEncodedLink()}#comments");
        }

        [Route("/blog/deletepost/{id}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeletePost(string id)
        {
            var existing = await _blog.GetPostById(id).ConfigureAwait(false);
            if (existing is null)
            {
                return this.NotFound();
            }

            await _blog.DeletePost(existing).ConfigureAwait(false);
            return this.Redirect("/");
        }

        [Route("/blog/edit/{id?}")]
        [HttpGet, Authorize]
        public async Task<IActionResult> Edit(string? id)
        {
            var categories = await _blog.GetCategories().ToListAsync();
            categories.Sort();
            this.ViewData[BlogConstants.ALLCATS] = categories;

            var tags = await _blog.GetTags().ToListAsync();
            tags.Sort();
            this.ViewData[BlogConstants.ALLTAGS] = tags;

            if (string.IsNullOrEmpty(id))
            {
                return this.View(new PostItem());
            }

            var post = await _blog.GetPostById(id).ConfigureAwait(false);

            return post is null ? this.NotFound() : (IActionResult)this.View(post);
        }

        [Route("/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Index([FromRoute] int page = 0)
        {
            // get published posts.
            var posts = _blog.GetPosts();

            // apply paging filter.
            var filteredPosts = posts.Skip(_settings.Value.PostsPerPage * page).Take(_settings.Value.PostsPerPage);

            // set the view option
            this.ViewData[BlogConstants.VIEWOPTION] = _settings.Value.ListView;

            this.ViewData[BlogConstants.TOTALPOSTCOUNT] = await posts.CountAsync().ConfigureAwait(true);
            this.ViewData[BlogConstants.TITLE] = _manifest.Name;
            this.ViewData[BlogConstants.DESCRIPTION] = _manifest.Description;
            this.ViewData[BlogConstants.PREV] = $"/{page + 1}/";
            this.ViewData[BlogConstants.NEXT] = $"/{(page <= 1 ? null : $"{page - 1}/")}";

            return this.View("~/Views/Home/Index.cshtml", filteredPosts);
        }

        [Route("/blog/{slug?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Post(string slug)
        {
            var post = await _blog.GetPostBySlug(slug).ConfigureAwait(true);

            return post is null ? this.NotFound() : (IActionResult)this.View(post);
        }

        /// <remarks>This is for redirecting potential existing URLs from the old Miniblog URL format.</remarks>
        [Route("/post/{slug}")]
        [HttpGet]
        public IActionResult Redirects(string slug) => this.LocalRedirectPermanent($"/blog/{slug}");

        [Route("/blog/{slug?}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Consumer preference.")]
        public async Task<IActionResult> UpdatePost(PostItem post)
        {
            if (!this.ModelState.IsValid)
            {
                return View(nameof(Edit), post);
            }

            if (post is null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            var existing = await _blog.GetPostById(post.Id.ToString()).ConfigureAwait(false) ?? post;
            string categories = Request.Form[BlogConstants.CATEGORIES];
            string tags = Request.Form[BlogConstants.TAGS];

            existing.Categories.Clear();
            categories.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToLowerInvariant())
                .ToList()
                .ForEach(existing.Categories.Add);
            existing.Tags.Clear();
            tags.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .ToList()
                .ForEach(existing.Tags.Add);
            existing.Title = post.Title.Trim();
            existing.Slug = !string.IsNullOrWhiteSpace(post.Slug) ? post.Slug.Trim() : PostItem.CreateSlug(post.Title);
            existing.IsPublished = post.IsPublished;
            existing.Content = post.Content.Trim();
            existing.Excerpt = post.Excerpt.Trim();

            await SaveFilesToDisk(existing).ConfigureAwait(false);

            await _blog.SavePost(existing).ConfigureAwait(false);

            return Redirect(post.GetEncodedLink());
        }

        private async Task SaveFilesToDisk(PostItem post)
        {
            var imgRegex = new Regex("<img[^>]+ />", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var base64Regex = new Regex("data:[^/]+/(?<ext>[a-z]+);base64,(?<base64>.+)", RegexOptions.IgnoreCase);
            var allowedExtensions = new[] {
              ".jpg",
              ".jpeg",
              ".gif",
              ".png",
              ".webp"
            };

            foreach (Match? match in imgRegex.Matches(post.Content))
            {
                if (match is null)
                {
                    continue;
                }

                var doc = new XmlDocument();
                doc.LoadXml($"<root>{match.Value}</root>");

                var img = doc.FirstChild.FirstChild;
                var srcNode = img.Attributes["src"];
                var fileNameNode = img.Attributes["data-filename"];

                // The HTML editor creates base64 DataURIs which we'll have to convert to image
                // files on disk
                if (srcNode is null || fileNameNode is null)
                {
                    continue;
                }

                var extension = System.IO.Path.GetExtension(fileNameNode.Value);

                // Only accept image files
                if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var base64Match = base64Regex.Match(srcNode.Value);
                if (base64Match.Success)
                {
                    var bytes = Convert.FromBase64String(base64Match.Groups["base64"].Value);
                    srcNode.Value = await _blog.SaveFile(bytes, fileNameNode.Value).ConfigureAwait(false);

                    img.Attributes.Remove(fileNameNode);
                    post.Content = post.Content.Replace(match.Value, img.OuterXml, StringComparison.OrdinalIgnoreCase);
                }
            }
        }


    }
}