using UsersCRUD.Models;

namespace UsersCRUD.Dtos;

public class GetUserDto
{
    public string Login { get; set; } = "null";
    public string Name { get; set; } =  "Unknown";
    public Gender Gender { get; set; }
    public DateTime? Birthday { get; set; }

    public bool Admin { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } =  "system";
    public DateTime? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public bool Active { get; set; }
    public string? RevokedBy { get; set; }
}