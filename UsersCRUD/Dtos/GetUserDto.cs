using UsersCRUD.Models;

namespace UsersCRUD.Dtos;

public class GetUserDto
{
    public string Name { get; set; } =  "Unknown";
    public Gender Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public bool Active { get; set; }
}