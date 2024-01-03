using Content.Shared.Prim14;
using Content.Shared.Prim14.TimedCooker;
using Robust.Client.GameObjects;

namespace Content.Client.Prim14;

public sealed class TimedCookerVisualsSystem : VisualizerSystem<TimedCookerVisualsComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    protected override void OnAppearanceChange(EntityUid uid, TimedCookerVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite) ||
            !_appearanceSystem.TryGetData(uid, TimedCookerState.Fired, out bool isFired))
            return;
        sprite.LayerSetVisible(TimedCookerVisualLayers.Fired, isFired);
    }
}

public enum TimedCookerVisualLayers : byte
{
    Fired
}
