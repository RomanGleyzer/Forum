namespace Application.Abstractions.Identity;

public sealed record OpResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static readonly OpResult Success = new(true, []);

    public static OpResult Fail(params string[] errors)
    {
        return new OpResult(false, errors);
    }
}