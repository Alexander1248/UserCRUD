using Microsoft.AspNetCore.Identity;
using UsersCRUD.Dtos;
using UsersCRUD.Models;

namespace UsersCRUD.Services;

/// <summary>
/// Service that provides interaction with user data.
/// </summary>
public class UserService
{
    private readonly List<User> users = [];
    private static readonly Dictionary<string, string> Tokens = new();
    private static readonly PasswordHasher<User> Hasher = new();

    /// <summary>
    /// A read-only list of users.
    /// </summary>
    public IReadOnlyList<User> Users => users;

    /// <summary>
    /// Constructor with admin initialization.
    /// </summary>
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

    /// <summary>
    /// Add user to database.
    /// </summary>
    /// <param name="dto">Data for user creation.</param>
    /// <param name="createdBy">Creator of user.</param>
    /// <returns>Added user.</returns>
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
    /// <summary>
    /// Set new password to user.
    /// </summary>
    /// <param name="user">User for password change.</param>
    /// <param name="newPassword">New password.</param>
    public void SetPassword(User user, string newPassword) =>
        user.Password = Hasher.HashPassword(user, newPassword);
    
    /// <summary>
    /// Delete user by login.
    /// </summary>
    /// <param name="login">User login to delete.</param>
    public bool DeleteUser(string login) => users.RemoveAll(x => x.Login == login) != 0;

    /// <summary>
    /// Revoke user by login.
    /// </summary>
    /// <param name="login">User login to revoke.</param>
    /// <param name="revokedBy"></param>
    /// <returns></returns>
    public bool RevokeUser(string login, string revokedBy)
    {
        var user = users.FirstOrDefault(x => x.Login == login);
        if (user == null) return false;
        user.RevokedOn = DateTime.Now;
        user.RevokedBy = revokedBy;
        return true;
    }
    
    /// <summary>
    /// Restore user by login.
    /// </summary>
    /// <param name="login">User login to restore.</param>
    /// <returns></returns>
    public bool RestoreUser(string login)
    {
        var user = users.FirstOrDefault(x => x.Login == login);
        if (user == null) return false;
        user.RevokedOn = null;
        user.RevokedBy = null;
        return true;
    }
    
    /// <summary>
    /// Authenticate user by login and password.
    /// </summary>
    /// <param name="login">Login for authentication.</param>
    /// <param name="password">Password for authentication.</param>
    /// <returns>Token</returns>
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
    
    /// <summary>
    /// Log out user.
    /// </summary>
    /// <param name="user">User data.</param>
    public void LogOut(User user) => Tokens.Remove(Tokens.First(pair => pair.Value == user.Login).Key);

    /// <summary>
    /// Gets user by token.
    /// </summary>
    /// <param name="token">Token linked to user.</param>
    /// <returns></returns>
    public User? GetUserByToken(string token)
    {
        token = token.Replace("Bearer ", "");
        return Tokens.TryGetValue(token, out var login) ? Users.FirstOrDefault(u => u.Login == login) : null;
    }

    /// <summary>
    /// Check that login is unique.
    /// </summary>
    /// <param name="login">Login for check.</param>
    /// <returns></returns>
    public bool IsLoginUnique(string login) =>
        !Users.Any(u => u.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
    
    
    /// <summary>
    /// Update user data.
    /// </summary>
    /// <param name="data">Fields to update.</param>
    /// <param name="user">User to update.</param>
    /// <returns>Result code</returns>
    public int UpdateUserData(UserUpdateDto data, User user)
    { 
        var modified = false;
        if (data.Name is not null)
        {
            user.Name = data.Name;
            modified = true;
        }
        if (data.Login is not null && user.Login != data.Login)
        {
            if (!IsLoginUnique(data.Login)) return -1;
            LogOut(user);
            user.Login = data.Login;
            modified = true;
        }

        if (data.Password is not null)
        {
            SetPassword(user, data.Password);
            modified = true;
        }

        if (data.Birthday is not null)
        {
            user.Birthday = data.Birthday;
            modified = true;
        }

        if (data.Gender.HasValue)
        {
            user.Gender = data.Gender.Value;
            modified = true;
        }

        return modified ? 1 : 0;
    }
}