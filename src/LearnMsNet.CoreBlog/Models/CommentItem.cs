

namespace LearnMsNet.CoreBlog.Models;

public class CommentItem
{
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsAdmin { get; set; } = false;

    [Required]
    public string Author { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public DateTime PublishDate { get; set; } = DateTime.UtcNow;

    [SuppressMessage(
            "Security",
            "CA5351:Do Not Use Broken Cryptographic Algorithms",
            Justification = "We aren't using it for encryption so we don't care.")]
    public string GetGravatar()
    {
        var inputBytes = Encoding.UTF8.GetBytes(this.Email.Trim().ToLowerInvariant());
        var hashBytes = MD5.HashData(inputBytes);

        // Convert the byte array to hexadecimal string
        var sb = new StringBuilder();
        for (var i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("X2", CultureInfo.InvariantCulture));
        }

        return $"https://www.gravatar.com/avatar/{sb.ToString().ToLowerInvariant()}?s=60&d=blank";
    }

    public string RenderContent() => this.Content;
}
