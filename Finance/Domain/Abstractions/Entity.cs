namespace Finance.Domain.Abstractions;

internal abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
