namespace LearnMsNet.CoreBlog.Services.Interfaces;

public interface IUserService
{
    bool ValidateUser(string username, string password);
}
