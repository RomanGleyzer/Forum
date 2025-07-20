using FluentValidation;

namespace Application.Features.Users.Commands;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Login).EmailAddress().NotEmpty();
    }
}
