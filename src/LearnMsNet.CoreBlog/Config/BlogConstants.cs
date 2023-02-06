namespace LearnMsNet.CoreBlog.Config;

public static class BlogConstants
{
    public static readonly string ALLCATS = "AllCats";
    public static readonly string ALLTAGS = "AllTags";
    public static readonly string CATEGORIES = "categories";
    public static readonly string TAGS = "tags";
    public static readonly string DASH = "-";
    public static readonly string DESCRIPTION = "Description";
    public static readonly string HEAD = "Head";
    public static readonly string NEXT = "next";
    public static readonly string PAGE = "page";
    public static readonly string PRELOAD = "Preload";
    public static readonly string PREV = "prev";
    public static readonly string RETURNURL = "ReturnUrl";
    public static readonly string SCRIPTS = "Scripts";
    public static readonly string SLUG = "slug";
    public static readonly string SPACE = " ";
    public static readonly string TITLE = "Title";
    public static readonly string TOTALPOSTCOUNT = "TotalPostCount";
    public static readonly string VIEWOPTION = "ViewOption";

    public static class Config
    {
        public static class Blog
        {
            public static readonly string NAME = "blog:name";
        }

        public static class User
        {
            public static readonly string PASSWORD = "user:password";
            public static readonly string SALT = "user:salt";
            public static readonly string USERNAME = "user:username";
        }
    }
}
