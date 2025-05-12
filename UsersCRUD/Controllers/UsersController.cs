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
    
    [HttpGet("{login}")]
    public IActionResult GetUserByLogin(string login)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("User not authorized!");
        if (!currentUser.Admin) return Forbid("User not admin!");

        var user = service.Users.FirstOrDefault(u => u.Login == login);
        if (user == null) return NotFound("User not found!");
        return Ok(new GetUserDto
        {
            Login = user.Login,
            Name = user.Name,
            Gender = user.Gender,
            Birthday = user.Birthday,
            Admin = user.Admin,
            Active = user.RevokedOn == null,
            CreatedOn = user.CreatedOn,
            CreatedBy = user.CreatedBy,
            ModifiedOn = user.ModifiedOn,
            ModifiedBy = user.ModifiedBy,
            RevokedBy = user.RevokedBy
        });
    }
    
    // Admins only
    [HttpPost("create")]
    public IActionResult CreateUser([FromBody] CreateUserDto dto)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("User not authorized!");
        if (!currentUser.Admin) return Forbid("User not admin!");
        
        if (!service.IsLoginUnique(dto.Login))
            return BadRequest("User with such login already exists!");

        var user = service.AddUser(dto, currentUser.Login);
        return CreatedAtAction(nameof(GetUserByLogin), new { login = user.Login }, new GetUserDto
        {
            Login = user.Login,
            Name = user.Name,
            Gender = user.Gender,
            Birthday = user.Birthday,
            Admin = user.Admin,
            Active = user.RevokedOn == null,
            CreatedOn = user.CreatedOn,
            CreatedBy = user.CreatedBy,
            ModifiedOn = user.ModifiedOn,
            ModifiedBy = user.ModifiedBy,
            RevokedBy = user.RevokedBy
        });
    }
    
    // Users and admins
}