namespace Application.Response.JobResp
{
    public record JobResponse(
        string Name,
        string Group,
        string? Description,
        bool IsDurable);

    public record TriggerResponse(
        string Name,
        string Group,
        string JobName,
        string JobGroup,
        DateTime? NextFireTime,
        DateTime? PreviousFireTime,
        string State,
        string Type);
}
