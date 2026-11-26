namespace SmartCare.Application.DTOs.Responses;

public sealed class ReleasedNoteResponse
{
    public int NoteId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}
