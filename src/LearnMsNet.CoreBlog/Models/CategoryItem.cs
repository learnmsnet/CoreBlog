namespace LearnMsNet.CoreBlog.Models;

public class CategoryItem
{
    public Guid Id { get; set; }

    public bool IsChecked { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
}
