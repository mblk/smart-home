using SmartHome.Domain.Model;
using SmartHome.Domain.State;

namespace SmartHome.Infrastructure;

public abstract class EntitySource : IDisposable
{
    protected EntityStore EntityStore { get; }

    public EntitySource(EntityStore entityStore)
    {
        EntityStore = entityStore;
    }

    public void Dispose()
    {
    }

    protected void AddEntity(EntityDefinition entityDefinition)
    {
        EntityStore.AddEntityDefinition(entityDefinition);
    }
}
