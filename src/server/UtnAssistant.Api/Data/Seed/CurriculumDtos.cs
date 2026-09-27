using System.Text.Json.Serialization;

namespace UtnAssistant.API.Data.Seed;

public class CurriculumRoot
{
    [JsonPropertyName("career")]
    public CareerDto Career { get; set; } = new();

    [JsonPropertyName("subjects")]
    public List<SubjectDto> Subjects { get; set; } = new();

    [JsonPropertyName("electives")]
    public List<ElectiveGroupDto> Electives { get; set; } = new();
}

public class CareerDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class SubjectDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("semester")]
    public int Semester { get; set; }

    [JsonPropertyName("isIntegrator")]
    public bool IsIntegrator { get; set; }

    [JsonPropertyName("isElective")]
    public bool IsElective { get; set; }

    [JsonPropertyName("correlatives")]
    public List<CorrelativeDto> Correlatives { get; set; } = new();
}

public class CorrelativeDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class ElectiveGroupDto
{
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("prerequisites")]
    public List<CorrelativeDto> Prerequisites { get; set; } = new();

    [JsonPropertyName("subjects")]
    public List<ElectiveSubjectDto> Subjects { get; set; } = new();
}

public class ElectiveSubjectDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public int Year { get; set; }
}