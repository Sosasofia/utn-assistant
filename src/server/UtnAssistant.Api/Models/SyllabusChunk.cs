using Pgvector;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtnAssistant.API.Models;

[Table("SyllabusChunk")]
public class SyllabusChunk
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("subjectId")]
    public string SubjectId { get; set; } = string.Empty;

    [ForeignKey("SubjectId")]
    public Subject Subject { get; set; } = null!;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("embedding", TypeName = "vector(1536)")]
    public Vector? Embedding { get; set; }
}