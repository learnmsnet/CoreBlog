

namespace LearnMsNet.CoreBlog.Services;

public class FileUserService : IUserService
{
    private const string USERS = "Users";
    private const string USERFILENAME = "users.json";
    private const string configSalt = "user:salt";
    private readonly string _folder;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IConfiguration _config;
    private List<User> _users;

    public FileUserService(
        IWebHostEnvironment env,
        IHttpContextAccessor contextAccessor,
        IConfiguration config)
    {
        _folder = Path.Combine(env.WebRootPath, "Data", USERS);
        _contextAccessor = contextAccessor;
        _config = config;
        Initialize();
    }

    private void Initialize()
    {
        _users = new();
        LoadUsers();
        SortUsers();
    }

    private void SortUsers()
    {
        _users.Sort((p1, p2) =>
        p2.UserName.CompareTo(p1.UserName));
    }

    private void LoadUsers()
    {
        if (!Directory.Exists(_folder))
        {
            Directory.CreateDirectory(_folder);
        }
        if (!File.Exists(GetFilePath(USERFILENAME)))
        {
            CreateAdminUser(USERFILENAME);
        }
        List<User> users = new();
        using StreamReader reader = new(GetFilePath(USERFILENAME));
        string json = reader.ReadToEnd();
        users = JsonSerializer.Deserialize<List<User>>(json);
        if (users == null)
        {
            return;
        }
        _users = users;
    }

    private void CreateAdminUser(string userFileName)
    {
        User user = new()
        {
            UserName = "admin",
            Password = HashPassword("admin"),
            Email = "admin@admin.com",
            LastLoginTime = DateTime.UtcNow
        };
        List<User> users = new()
        {
            user
        };
        string jsonUsers = JsonSerializer.Serialize(users,
            new JsonSerializerOptions()
            {
                WriteIndented = true
            });
        using StreamWriter outputFile = new(GetFilePath(userFileName));
        outputFile.WriteLine(jsonUsers);
    }

    private string GetFilePath(string userFileName)
    {
        return Path.Combine(_folder, userFileName);
    }

    private string HashPassword(string password)
    {
        string hashedPassword = "";
        if (!string.IsNullOrEmpty(password))
        {
            string saltString = _config.GetValue(configSalt, "some custom string");
            byte[] salt = Encoding.UTF8.GetBytes(saltString);
            byte[] hashBytes = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 1000,
                numBytesRequested: 256 / 8);
            hashedPassword = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }
        return hashedPassword;
    }
    public bool ValidateUser(
        string username,
        string password)
    {
        var user = _users.FirstOrDefault(
            u => u.UserName.ToLower() == username.ToLower());
        if (user != null)
        {
            if (user.Password == HashPassword(password))
            {
                return true;
            }
            return false;
        }
        return false;
    }
}
