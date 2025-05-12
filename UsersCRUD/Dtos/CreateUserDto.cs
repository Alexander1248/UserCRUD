using System.ComponentModel.DataAnnotations;
using UsersCRUD.Models;

namespace UsersCRUD.Dtos;

public class CreateUserDto
{
    [Required]
    public string Login { get; set; }
    [Required]
    public string Password { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public Gender Gender { get; set; }
    public DateTime? Birthday { get; set; }
    [Required]
    public bool Admin { get; set; }
}