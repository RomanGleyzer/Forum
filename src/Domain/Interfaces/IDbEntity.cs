namespace Domain.Interfaces;

public interface IDbEntity<TId>
{
    TId Id { get; set; }
}
