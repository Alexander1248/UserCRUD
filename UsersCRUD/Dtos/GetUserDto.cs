using System.ComponentModel.DataAnnotations;
using UsersCRUD.Models;

namespace UsersCRUD.Dtos;

public class GetUserDto
{
    [Required]
    public string Name { get; set; }
    [Required]
    public Gender Gender { get; set; }
    public DateTime? Birthday { get; set; }
    [Required]
    public bool Active { get; set; }
}