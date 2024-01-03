using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Prim14.Ceramics;

public sealed class DryingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DryingComponent, DryingDoneEvent>(OnDryingDone);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<DryingComponent>();

        // TODO add check for entity being near a fire source
        while (query.MoveNext(out var uid, out var component))
        {
            if (_containerSystem.IsEntityInContainer(uid))
                continue;

            if (component.Accumulator < component.DryingTime)
            {
                component.Accumulator += frameTime;
                continue;
            }

            component.Accumulator = 0;
            var ev = new DryingDoneEvent(component);
            RaiseLocalEvent(uid, ev);
        }
    }

    private void OnDryingDone(EntityUid uid, DryingComponent component, DryingDoneEvent args)
    {
        EntityManager.SpawnEntity(component.Result, _transformSystem.GetMapCoordinates(uid));
        QueueDel(uid);
    }

    private sealed class DryingDoneEvent : EntityEventArgs
    {
        private DryingComponent Dry {[UsedImplicitly] get;}
        public DryingDoneEvent(DryingComponent dry)
        {
            Dry = dry;
        }
    }
}
