using Microsoft.AspNetCore.Mvc;
using UsersCRUD.Dtos;
using UsersCRUD.Models;
using UsersCRUD.Services;

namespace UsersCRUD.Controllers;

[ApiController]
[Route("users")]
public class UsersController(UserService service) : ControllerBase
{
    private User? GetCurrentUser() => service.GetUserByToken(Request.Headers.Authorization.ToString());
    
    [HttpPost("auth")]
    public IActionResult Auth([FromBody] LoginDto request)
    {
        var token = service.Authenticate(request.Login, request.Password);
        return token == null ? Unauthorized("Invalid credentials.") : Ok(token);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        service.LogOut(currentUser);
        return Ok("Logged out!");
    }
    
    
    // Admins only
    [HttpGet("get/active")]
    public IActionResult GetActiveUsers()
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");

        var users = service.Users
            .Where(u => u.RevokedOn == null)
            .OrderBy(u => u.CreatedOn)
            .Select(u => u.Login);
        return Ok(new { Users = users });
    }
    
    [HttpGet("get/older")]
    public IActionResult GetActiveUsers([FromQuery] int age)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");

        var users = service.Users
            .Where(u => u.Birthday.HasValue && u.Birthday.Value.AddYears(age) <= DateTime.Now)
            .Select(u => u.Login);
        return Ok(new { Users = users });
    }
    
    [HttpGet("get/{login}")]
    public IActionResult GetUserByLogin(string login)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");

        var user = service.Users.FirstOrDefault(u => u.Login == login);
        if (user == null) return NotFound("User not found!");
        return Ok(new GetUserDto
        {
            Name = user.Name,
            Gender = user.Gender,
            Birthday = user.Birthday,
            Active = user.RevokedOn == null
        });
    }
    
    [HttpPost("create")]
    public IActionResult CreateUser([FromBody] CreateUserDto dto)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");
        
        if (!service.IsLoginUnique(dto.Login))
            return BadRequest("User with such login already exists!");

        var user = service.AddUser(dto, currentUser.Login);
        return CreatedAtAction(nameof(GetUserByLogin), new { login = user.Login }, "User created!");
    }

    [HttpDelete("delete/{login}")]
    public IActionResult DeleteUser(string login, bool hard = false)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");
        if (hard) service.DeleteUser(login);
        else if (!service.RevokeUser(login, currentUser.Login))
            return BadRequest("User not found!");
        return Ok("User was deleted!");
    }

    [HttpPost("restore/{login}")]
    public IActionResult RestoreUser(string login)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");
        if (!service.RestoreUser(login))
            return BadRequest("User not found!");
        return Ok("User was restored!");
    }
    
    [HttpPatch("update/{login}")]
    public IActionResult UpdateAdmin(string login, [FromBody] UserUpdateDto data)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");
        
        var user = service.Users.FirstOrDefault(u => u.Login == login);
        if (user == null) return NotFound("User not found!");
        
        if (data.Name is not null) user.Name = data.Name;
        if (data.Login is not null)
        {
            service.LogOut(user);
            user.Login = data.Login;
        }
        if (data.Password is not null) service.SetPassword(user, data.Password);
        if (data.Birthday is not null) user.Birthday = data.Birthday;
        if (data.Gender.HasValue) user.Gender = data.Gender.Value;
        return Ok("User was updated!");
    }
    
    // Users only
    [HttpGet("get/current")]
    public IActionResult GetUserByLoginAndPassword()
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("User not authorized!");
        if (currentUser.RevokedOn is not null) return Forbid("User revoked!");
        
        return Ok(new GetUserDto
        {
            Name = currentUser.Name,
            Gender = currentUser.Gender,
            Birthday = currentUser.Birthday,
            Active = currentUser.RevokedOn == null
        });
    }
    
    [HttpPatch("update")]
    public IActionResult Update([FromBody] UserUpdateDto data)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (currentUser.RevokedOn is not null) return Forbid("User revoked!");
        
        if (data.Name is not null) currentUser.Name = data.Name;
        if (data.Login is not null)
        {
            service.LogOut(currentUser);
            currentUser.Login = data.Login;
        }
        if (data.Password is not null) service.SetPassword(currentUser, data.Password);
        if (data.Birthday is not null) currentUser.Birthday = data.Birthday;
        if (data.Gender.HasValue) currentUser.Gender = data.Gender.Value;
        return Ok("User was updated!");
    }
}