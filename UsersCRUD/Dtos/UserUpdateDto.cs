using UsersCRUD.Models;

namespace UsersCRUD.Dtos;

public class UserUpdateDto
{  
    public string? Login { get; set; } = "null";
    public string? Password { get; set; } =  "";
    public string? Name { get; set; } =  "Unknown";
    public Gender? Gender { get; set; }
    public DateTime? Birthday { get; set; }
}