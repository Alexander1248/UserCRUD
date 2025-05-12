using System.Text;
using Microsoft.AspNetCore.Mvc;
using UsersCRUD.Dtos;
using UsersCRUD.Models;
using UsersCRUD.Services;

namespace UsersCRUD.Controllers;

/// <summary>
/// Controller for user CRUD.
/// </summary>
/// <param name="service"></param>
[ApiController]
[Route("users")]
public class UsersController(UserService service) : ControllerBase
{
    private User? GetCurrentUser()
    {
        var token = Request.Headers.Authorization.ToString();
        if (HttpContext.Session.TryGetValue("token", out var data))
            token = Encoding.UTF8.GetString(data);
        return service.GetUserByToken(token);
    }

    /// <summary>
    /// Authenticates a user and returns a token.
    /// </summary>
    /// <param name="request"></param>
    /// <returns>Token if authentication is successful.</returns>
    /// <response code="200">If authentication is successful.</response>
    /// <response code="401">If credentials are invalid.</response>
    [HttpPost("auth")]
    [ProducesResponseType(typeof(string),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    public IActionResult Auth([FromBody] LoginDto request)
    {
        var token = service.Authenticate(request.Login, request.Password);
        if (token == null) return Unauthorized("Invalid credentials!");
        HttpContext.Session.Set("token", Encoding.UTF8.GetBytes(token));
        return Ok(token);
    }

    /// <summary>
    /// Logs out the currently authenticated user.
    /// </summary>
    /// <response code="200">If log out successful.</response>
    /// <response code="401">If the user isn't authorized.</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(string),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        service.LogOut(currentUser);
        return Ok("Logged out!");
    }
    
    
    /// <summary>
    /// Gets all active (not revoked) users. Admins only.
    /// </summary>
    /// <returns>List of user logins.</returns>
    /// <response code="200">If request returned data.</response>
    /// <response code="401">If the user isn't authorized.</response>
    /// <response code="403">If the current user is not admin.</response>
    [HttpGet("get/active")]
    [ProducesResponseType(typeof(IEnumerable<string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
    public IActionResult GetActiveUsers()
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");

        var users = service.Users
            .Where(u => u.RevokedOn == null)
            .OrderBy(u => u.CreatedOn)
            .Select(u => u.Login);
        return Ok(users);
    }
    
    /// <summary>
    /// Gets users older than a specific age. Admins only.
    /// </summary>
    /// <param name="age">Minimum age of users to retrieve.</param>
    /// <returns>List of user logins.</returns>
    /// <response code="200">If request returned data.</response>
    /// <response code="401">If the user isn't authorized.</response>
    /// <response code="403">If the current user is not admin.</response>
    [HttpGet("get/older")]
    [ProducesResponseType(typeof(IEnumerable<string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
    public IActionResult GetActiveUsers([FromQuery] int age)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");

        var users = service.Users
            .Where(u => u.Birthday.HasValue && u.Birthday.Value.AddYears(age) <= DateTime.Now)
            .Select(u => u.Login);
        return Ok(users);
    }
    
    /// <summary>
    /// Gets user details by login. Admins only.
    /// </summary>
    /// <param name="login">Login of the user.</param>
    /// <returns>Data of the requested user.</returns>
    /// <response code="200">If request returned data.</response>
    /// <response code="401">If the user isn't authorized.</response>
    /// <response code="403">If the current user is not admin.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpGet("get/{login}")]
    [ProducesResponseType(typeof(GetUserDto),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string),StatusCodes.Status404NotFound)]
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
    /// <summary>
    /// Creates a new user. Admins only.
    /// </summary>
    /// <param name="dto">User creation data.</param>
    /// <response code="201">If current user successfully created.</response>
    /// <response code="401">If the user isn't authorized.</response>
    /// <response code="403">If the current user is not admin.</response>
    /// <response code="409">If the user with such login already exists.</response>
    [HttpPost("create")]
    [ProducesResponseType(typeof(string),StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string),StatusCodes.Status409Conflict)]
    public IActionResult CreateUser([FromBody] CreateUserDto dto)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");
        
        if (!service.IsLoginUnique(dto.Login))
            return Conflict("User with such login already exists!");

        var user = service.AddUser(dto, currentUser.Login);
        return CreatedAtAction(nameof(GetUserByLogin), new { login = user.Login }, "User created!");
    }
    
    /// <summary>
    /// Deletes (soft/hard) a user by login. Admins only.
    /// </summary>
    /// <param name="login">User login to delete.</param>
    /// <param name="hard">True for hard delete, false for soft delete.</param>
    /// <response code="200">If a user deleted.</response>
    /// <response code="401">If the user isn't authorized.</response>
    /// <response code="403">If the current user is not admin.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpDelete("delete/{login}")]
    [ProducesResponseType(typeof(string),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string),StatusCodes.Status404NotFound)]
    public IActionResult DeleteUser(string login, bool hard = false)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");
        if (hard)
        {
            if (service.DeleteUser(login))
                return Ok("User was hard deleted!");
        }
        else if (service.RevokeUser(login, currentUser.Login))
            return Ok("User was soft deleted!");
        
        return NotFound("User not found!");
    }
    
    /// <summary>
    /// Restores a previously revoked user. Admins only.
    /// </summary>
    /// <param name="login">User login to restore.</param>
    /// <response code="200">If a user restored.</response>
    /// <response code="401">If the user isn't authorized.</response>
    /// <response code="403">If the current user is not admin.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpPost("restore/{login}")]
    [ProducesResponseType(typeof(string),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string),StatusCodes.Status404NotFound)]
    public IActionResult RestoreUser(string login)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");
        if (!service.RestoreUser(login))
            return NotFound("User not found!");
        return Ok("User was restored!");
    }
    
    /// <summary>
    /// Updates user data by login. Admins only.
    /// </summary>
    /// <param name="login">Login of a user to update.</param>
    /// <param name="data">Fields to update.</param>
    /// <response code="200">If user data updated or no changes provided.</response>
    /// <response code="401">If the user isn't authorized.</response>
    /// <response code="403">If the current user is not admin.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="409">If the user with such login already exists.</response>
    [HttpPatch("update/{login}")]
    [ProducesResponseType(typeof(string),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string),StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(string),StatusCodes.Status404NotFound)]
    public IActionResult UpdateAdmin(string login, [FromBody] UserUpdateDto data)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (!currentUser.Admin) return Forbid("Current user not admin!");
        
        var user = service.Users.FirstOrDefault(u => u.Login == login);
        if (user == null) return NotFound("User not found!");
        switch (service.UpdateUserData(data, user))
        {
            case -1:
                return Conflict("User with such login already exists!");
            case 0:
                return Ok("No changes provided!");
            case 1:
                user.ModifiedOn = DateTime.Now;
                user.ModifiedBy = currentUser.Login;
                return Ok("User was updated!");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Gets the current authenticated user's data.
    /// </summary>
    /// <returns>Data of the current user.</returns>
    /// <response code="200">If request returned data.</response>
    /// <response code="401">If the user isn't authorized.</response>
    /// <response code="403">If the current user revoked.</response>
    [HttpGet("get/current")]
    [ProducesResponseType(typeof(GetUserDto),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
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
    /// <summary>
    /// Updates the currently authenticated user's data.
    /// </summary>
    /// <param name="data">Fields to update.</param>
    /// <response code="200">If user data updated or no changes provided.</response>
    /// <response code="401">If the user isn't authorized.</response>
    /// <response code="403">If the current user is not admin.</response>
    /// <response code="409">If the user with such login already exists.</response>
    [HttpPatch("update")]
    [ProducesResponseType(typeof(string),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string),StatusCodes.Status409Conflict)]
    public IActionResult Update([FromBody] UserUpdateDto data)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Unauthorized("Current user not authorized!");
        if (currentUser.RevokedOn is not null) return Forbid("User revoked!");
        
        switch (service.UpdateUserData(data, currentUser))
        {
            case -1:
                return Conflict("User with such login already exists!");
            case 0:
                return Ok("No changes provided!");
            case 1:
                currentUser.ModifiedOn = DateTime.Now;
                currentUser.ModifiedBy = currentUser.Login;
                return Ok("User was updated!");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}