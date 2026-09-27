using UtnAssistant.API.Enums;
using UtnAssistant.API.Models;

namespace UtnAssistant.API.Domain;

public record MissingPrerequisite(
    string RequiredSubjectCode,
    string RequiredSubjectName,
    CorrelativeType Type,
    ProgressStatus UserStatus
);

public record SubjectEvaluationResult(
    ProgressStatus Status,
    bool CanTake,
    List<MissingPrerequisite> MissingPrerequisites
);

public static class AcademicRuleEngine
{
    public static SubjectEvaluationResult Evaluate(
        Subject subject,
        IReadOnlyDictionary<string, ProgressStatus> historyMap)
    {
        var myStatus = historyMap.GetValueOrDefault(subject.Id, ProgressStatus.NOT_ENROLLED);

        if (myStatus == ProgressStatus.APPROVED)
        {
            return new SubjectEvaluationResult(myStatus, CanTake: false, new List<MissingPrerequisite>());
        }

        var missing = new List<MissingPrerequisite>();

        foreach (var rule in subject.CorrelativesAsTarget)
        {
            if (rule.IsTransient) continue;

            var reqStatus = historyMap.GetValueOrDefault(rule.RequiredSubjectId, ProgressStatus.NOT_ENROLLED);

            var isMet = rule.Type switch
            {
                CorrelativeType.APPROVED => reqStatus == ProgressStatus.APPROVED,
                _ => reqStatus is ProgressStatus.ATTENDED or ProgressStatus.APPROVED
            };

            if (!isMet)
            {
                missing.Add(new MissingPrerequisite(
                    rule.RequiredSubject.Code,
                    rule.RequiredSubject.Name,
                    rule.Type,
                    reqStatus
                ));
            }
        }

        return new SubjectEvaluationResult(
            myStatus,
            CanTake: missing.Count == 0,
            missing
        );
    }
}