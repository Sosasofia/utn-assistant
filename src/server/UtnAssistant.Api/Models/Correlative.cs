using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UtnAssistant.API.Enums;

namespace UtnAssistant.API.Models;

[Table("Correlative")]
public class Correlative
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("subjectId")]
    public string SubjectId { get; set; } = string.Empty;

    [ForeignKey("SubjectId")]
    public Subject Subject { get; set; } = null!;

    [Column("requiredSubjectId")]
    public string RequiredSubjectId { get; set; } = string.Empty;

    [ForeignKey("RequiredSubjectId")]
    public Subject RequiredSubject { get; set; } = null!;

    [Column("type")]
    public CorrelativeType Type { get; set; }

    [Column("isTransient")]
    public bool IsTransient { get; set; }
}