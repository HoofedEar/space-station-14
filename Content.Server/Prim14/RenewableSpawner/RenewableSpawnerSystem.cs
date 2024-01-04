using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Prim14.RenewableSpawner;

public sealed class RenewableSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var spawners = EntityManager.EntityQueryEnumerator<RenewableSpawnerComponent>();
        while (spawners.MoveNext(out var owner, out var spawner))
        {
            spawner.ElapsedTime += frameTime;

            if (!(spawner.ElapsedTime >= spawner.IntervalSeconds))
                return;
            Respawn(owner, spawner);
            spawner.ElapsedTime = 0;
        }
    }

    /// <summary>
    /// Spawn the chosen entity, then delete the spawner.
    /// </summary>
    /// <param name="owner">Owner of the component</param>
    /// <param name="component">Component passthrough</param>
    private void Respawn(EntityUid owner, RenewableSpawnerComponent component)
    {
        var entity = _robustRandom.Pick(component.Prototypes);
        var xform = Transform(owner);
        var tile = xform.Coordinates.GetTileRef(EntityManager, _mapMan);
        if (tile == null)
            return;

        if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
        {
            return;
        }

        var pos = _transformSystem.GetMapCoordinates(owner);
        EntityManager.SpawnEntity(entity, pos);
        EntityManager.DeleteEntity(owner);
    }
}
