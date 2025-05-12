using UsersCRUD.Models;

namespace UsersCRUD.Dtos;

public class CreateUserDto
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
    public string Name { get; set; } = "";
    public Gender Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public bool Admin { get; set; }
}