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
    
    
    // Create
    // 1) Создание пользователя по логину, паролю, имени, полу и дате рождения + указание будет ли
    //     пользователь админом (Доступно Админам)
    // ++++++++++++
    // Update-1
    // 2) Изменение имени, пола или даты рождения пользователя (Может менять Администратор, либо
    //     лично пользователь, если он активен (отсутствует RevokedOn))
    // 3) Изменение пароля (Пароль может менять либо Администратор, либо лично пользователь, если
    //     он активен (отсутствует RevokedOn))
    // 4) Изменение логина (Логин может менять либо Администратор, либо лично пользователь, если
    //     он активен (отсутствует RevokedOn), логин должен оставаться уникальным)
    // Read
    // 5) Запрос списка всех активных (отсутствует RevokedOn) пользователей, список отсортирован по
    //     CreatedOn (Доступно Админам)
    // ++++++++++++
    // 6) Запрос пользователя по логину, в списке долны быть имя, пол и дата рождения статус активный
    //     или нет (Доступно Админам)
    // ++++++++++++
    // 7) Запрос пользователя по логину и паролю (Доступно только самому пользователю, если он
    //     активен (отсутствует RevokedOn))
    // ++++++++++++
    // 8) Запрос всех пользователей старше определённого возраста (Доступно Админам)
    // ++++++++++++
    // Delete
    // 9) Удаление пользователя по логину полное или мягкое (При мягком удалении должна
    //     происходить простановка RevokedOn и RevokedBy) (Доступно Админам)
    // ++++++++++++
    // Update-2
    // 10) Восстановление пользователя - Очистка полей (RevokedOn, RevokedBy) (Доступно Админам)
    // ++++++++++++
    
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
        if (!service.RevokeUser(login, currentUser.Login))
            return BadRequest("User not found!");
        return Ok("User was restored!");
    }
    
    // Users and admins
    
    
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
}