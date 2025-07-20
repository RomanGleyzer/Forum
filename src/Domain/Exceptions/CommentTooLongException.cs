namespace Domain.Exceptions;

public class CommentTooLongException(Guid commentId) : Exception($"The comment with ID {commentId} is too long.")
{
    public Guid CommentId { get; } = commentId;
}