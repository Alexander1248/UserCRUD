using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace UsersCRUD.Models;

public enum Gender { Female = 0, Male = 1, Unknown = 2 }

public class User
{ 
    [Required]
    public string Login { get; set; }
    [Required]
    public string Password { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public Gender Gender { get; set; }
    [DefaultValue(null)]
    public DateTime? Birthday { get; set; }
    
    [Required]
    public bool Admin { get; set; }
    public DateTime CreatedOn { get; set; }
    [DefaultValue("system")]
    public string CreatedBy { get; set; }
    [DefaultValue(null)]
    public DateTime? ModifiedOn { get; set; }
    [DefaultValue(null)]
    public string? ModifiedBy { get; set; }
    [DefaultValue(null)]
    public DateTime? RevokedOn { get; set; }
    [DefaultValue(null)]
    public string? RevokedBy { get; set; }
}