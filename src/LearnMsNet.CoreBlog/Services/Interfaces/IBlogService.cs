namespace LearnMsNet.CoreBlog.Services.Interfaces;

public interface IBlogService
{
    Task DeletePost(PostItem post);

    IAsyncEnumerable<string> GetCategories();

    IAsyncEnumerable<string> GetTags();

    Task<PostItem?> GetPostById(string id);

    Task<PostItem?> GetPostBySlug(string slug);
    IAsyncEnumerable<PostItem> GetPosts();

    IAsyncEnumerable<PostItem> GetPosts(int count, int skip = 0);

    IAsyncEnumerable<PostItem> GetPostsByCategory(string category);

    IAsyncEnumerable<PostItem> GetPostsByTag(string tag);

    Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null);

    Task SavePost(PostItem post);
}
