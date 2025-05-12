using System.ComponentModel;
using UsersCRUD.Models;

namespace UsersCRUD.Dtos;

public class UserUpdateDto
{  
    [DefaultValue(null)]
    public string? Login { get; set; }
    [DefaultValue(null)]
    public string? Password { get; set; }
    [DefaultValue(null)]
    public string? Name { get; set; }
    [DefaultValue(null)]
    public Gender? Gender { get; set; }
    [DefaultValue(null)]
    public DateTime? Birthday { get; set; }
}