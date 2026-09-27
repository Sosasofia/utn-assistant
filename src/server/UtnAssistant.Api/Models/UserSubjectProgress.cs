using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UtnAssistant.API.Enums;

namespace UtnAssistant.API.Models;

[Table("UserSubjectProgress")]
public class UserSubjectProgress
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("userId")]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [Column("subjectId")]
    public string SubjectId { get; set; } = string.Empty;

    [ForeignKey("SubjectId")]
    public Subject Subject { get; set; } = null!;

    [Column("status")]
    public ProgressStatus Status { get; set; }

    [Column("grade")]
    public double? Grade { get; set; }

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}