using FluentValidation;

namespace Application.Features.Posts.Queries;

public class GetPostsByCursorQueryValidator : AbstractValidator<GetPostsByCursorQuery>
{
    public GetPostsByCursorQueryValidator()
    {
        RuleFor(x => x.Take)
            .InclusiveBetween(1, 100)
            .WithMessage("Take must be between 1 and 100.");

        RuleFor(x => new { x.CursorCreatedAt, x.CursorId })
            .Must(c => (!c.CursorCreatedAt.HasValue && !c.CursorId.HasValue) ||
                       (c.CursorCreatedAt.HasValue && c.CursorId.HasValue))
            .WithMessage("Both cursorCreatedAt and cursorId must be provided together (or omitted).");

        When(x => x.CursorCreatedAt.HasValue && x.CursorId.HasValue, () =>
        {
            RuleFor(x => x.CursorCreatedAt)
                .Must(d => d.HasValue && d.Value > DateTimeOffset.MinValue)
                .WithMessage("cursorCreatedAt must be a valid date.");

            RuleFor(x => x.CursorId)
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage("cursorId must be a non-empty GUID.");
        });
    }
}
