using FluentValidation;

namespace Application.Features.Posts.Queries;

public class GetPostsByCursorQueryValidator : AbstractValidator<GetPostsByCursorQuery>
{
    public GetPostsByCursorQueryValidator()
    {
        RuleFor(x => x.Cursor)
            .Must(BeAValidDate).WithMessage("Cursor must be a valid date or null.");
        RuleFor(x => x.Take)
            .InclusiveBetween(1, 100).WithMessage("Take must be between 1 and 100.");
    }
    private bool BeAValidDate(DateTime? date)
    {
        return !date.HasValue || date.Value > DateTime.MinValue;
    }
}
