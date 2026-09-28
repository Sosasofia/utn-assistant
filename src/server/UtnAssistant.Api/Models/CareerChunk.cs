using Microsoft.Data.SqlTypes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtnAssistant.API.Models;

[Table("CareerChunk")]
public class CareerChunk
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("careerId")]
    public string CareerId { get; set; } = string.Empty;

    [ForeignKey("CareerId")]
    public Career Career { get; set; } = null!;

    [Column("sectionTitle")]
    public string SectionTitle { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("embedding", TypeName = "vector(1536)")]
    public SqlVector<float> Embedding { get; set; }
}