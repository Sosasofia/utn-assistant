using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UtnAssistant.API.Models;

[Table("Subject")]
public class Subject
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("year")]
    public int Year { get; set; }

    [Column("semester")]
    public int Semester { get; set; }

    [Column("credits")]
    public int? Credits { get; set; }

    [Column("isIntegrator")]
    public bool IsIntegrator { get; set; }

    [Column("isElective")]
    public bool IsElective { get; set; }

    [Column("careerId")]
    public string CareerId { get; set; } = string.Empty;

    [ForeignKey("CareerId")]
    public Career Career { get; set; } = null!;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    public List<SyllabusChunk> Chunks { get; set; } = new();

    [InverseProperty("Subject")]
    public List<Correlative> CorrelativesAsTarget { get; set; } = new();

    [InverseProperty("RequiredSubject")]
    public List<Correlative> CorrelativesAsPrereq { get; set; } = new();
}