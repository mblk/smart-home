using SmartHome.Domain.Model;

namespace SmartHome.Domain.State;

public class EntityStore
{
    private readonly Dictionary<string, EntityDefinition> _entityDefinitions = new();

    public IReadOnlyDictionary<string, EntityDefinition> EntityDefinitions => _entityDefinitions;

    public EntityStore()
    {
    }

    public void AddEntityDefinition(EntityDefinition entityDefinition)
    {
        if (_entityDefinitions.ContainsKey(entityDefinition.Id))
            throw new ArgumentException($"Entity with id '{entityDefinition.Id}' already exists");
        
        _entityDefinitions.Add(entityDefinition.Id, entityDefinition);
    }
}
