namespace UsersCRUD.Models;

public enum Gender { Female = 0, Male = 1, Unknown = 2 }

public class User
{
    public Guid Guid { get; set; } = Guid.NewGuid();
    public string Login { get; set; } = "null";
    public string Password { get; set; } =  "";
    public string Name { get; set; } =  "Unknown";
    public Gender Gender { get; set; }
    public DateTime? Birthday { get; set; }

    public bool Admin { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } =  "system";
    public DateTime? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? RevokedOn { get; set; }
    public string? RevokedBy { get; set; }
}