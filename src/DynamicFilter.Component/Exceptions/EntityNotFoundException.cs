namespace DynamicFilter.Component.Exceptions;

public class EntityNotFoundException : ArgumentException
{
    public EntityNotFoundException(string entityName) : base($"Entity '{entityName}' not found")
    {
    }
}