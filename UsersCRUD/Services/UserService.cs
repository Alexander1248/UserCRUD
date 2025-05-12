using Microsoft.AspNetCore.Identity;
using UsersCRUD.Dtos;
using UsersCRUD.Models;

namespace UsersCRUD.Services;

public class UserService
{
    private readonly List<User> users = [];
    private static readonly Dictionary<string, string> Tokens = new();
    private static readonly PasswordHasher<User> Hasher = new();

    public IReadOnlyList<User> Users => users;

    public UserService()
    {
        AddUser(
            new CreateUserDto{
                Login = "admin",
                Name = "Admin",
                Password = "12345",
                Gender = Gender.Unknown,
                Admin = true
            }, 
            "system"
        );
    }

    public User AddUser(CreateUserDto dto, string createdBy)
    {
        var user = new User
        {
            Login = dto.Login,
            Name = dto.Name,
            Gender = dto.Gender,
            Birthday = dto.Birthday,
            Admin = dto.Admin,
            CreatedBy = createdBy
        };
        user.Password = Hasher.HashPassword(user, dto.Password);
        users.Add(user);
        return user;
    }
    public void SetPassword(User user, string newPassword) =>
        user.Password = Hasher.HashPassword(user, newPassword);
    
    public void DeleteUser(string login) => users.RemoveAll(x => x.Login == login);

    public bool RevokeUser(string login, string revokedBy)
    {
        var user = users.FirstOrDefault(x => x.Login == login);
        if (user == null) return false;
        user.RevokedOn = DateTime.Now;
        user.RevokedBy = revokedBy;
        return true;
    }
    
    public bool RestoreUser(string login)
    {
        var user = users.FirstOrDefault(x => x.Login == login);
        if (user == null) return false;
        user.RevokedOn = null;
        user.RevokedBy = null;
        return true;
    }
    
    public string? Authenticate(string login, string password)
    {
        var user = Users.FirstOrDefault(u => 
            u.Login == login 
            && Hasher.VerifyHashedPassword(u, u.Password, password) != PasswordVerificationResult.Failed
            && u.RevokedOn == null);
        if (user == null) return null;

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        Tokens[token] = login;
        return token;
    }
    
    public void LogOut(User user) => Tokens.Remove(user.Login);

    public User? GetUserByToken(string token)
    {
        token = token.Replace("Bearer ", "");
        return Tokens.TryGetValue(token, out var login) ? Users.FirstOrDefault(u => u.Login == login) : null;
    }

    public bool IsLoginUnique(string login) =>
        !Users.Any(u => u.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
    
    public bool IsUserActive(string login) =>
        Users.Any(u => u.Login.Equals(login, StringComparison.OrdinalIgnoreCase) && u.RevokedOn == null);
}