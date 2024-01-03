using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Prim14.TimedCooker;
using Content.Shared.Stacks;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Prim14.TimedCooker;

public sealed class TimedCookerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;
    private int _multiplier;

    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private Queue<EntityUid> _producingAddQueue = new();
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private Queue<EntityUid> _producingRemoveQueue = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimedCookerComponent, ComponentInit>(HandleTimedCookerInit);
        SubscribeLocalEvent<TimedCookerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<TimedCookerComponent, InteractHandEvent>(OnInteractHand);
    }

    private void HandleTimedCookerInit(EntityUid uid, TimedCookerComponent component, ComponentInit args)
    {
        component.Container = _containerSystem.EnsureContainer<Container>(uid, "cooker_container", out _);
        UpdateAppearance(uid, component, false);
    }

    private void OnInteractHand(EntityUid uid, TimedCookerComponent component, InteractHandEvent args)
    {
        if (component.FuelStorage <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("timed-cooker-no-fuel"), uid, Filter.Entities(args.User), false);
            return;
        }
        _popupSystem.PopupEntity(
            component.IsRunning ? Loc.GetString("timed-cooker-turn-off") : Loc.GetString("timed-cooker-turn-on"), uid,
            Filter.Entities(args.User), false);
        component.IsRunning = !component.IsRunning;
        UpdateAppearance(uid, component, component.IsRunning);
    }

    private void OnInteractUsing(EntityUid uid, TimedCookerComponent component, InteractUsingEvent args)
    {
        // Are we inserting wood?
        if (TryComp(args.Used, out PhysicalCompositionComponent? matComp) && matComp.MaterialComposition.ContainsKey("Wood"))
        {
            _multiplier = TryComp<StackComponent>(args.Used, out var stack) ? stack.Count : 4;
            component.FuelStorage += 30 * _multiplier;
            if (component.IsRunning)
                UpdateAppearance(uid, component, true);
            QueueDel(args.Used);
            return;
        }

        // No? Ok can it insert, is it on the whitelist, and does it have a recipe?
        if (!_containerSystem.CanInsert(args.Used, component.Container) ||
            component.Whitelist != null && !component.Whitelist.IsValid(args.Used) ||
            !TryComp<TimedCookableComponent>(args.Used, out var cookable))
        {
            _popupSystem.PopupEntity(Loc.GetString("timed-cooker-insert-fail"), uid, Filter.Entities(args.User), false);
            return;
        }

        // Make sure it has fuel
        if (component.FuelStorage <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("timed-cooker-no-fuel"), uid, Filter.Entities(args.User), false);
            return;
        }

        // Make sure it's not full
        if (component.Container.ContainedEntities.Count >= component.Max)
        {
            _popupSystem.PopupEntity(Loc.GetString("timed-cooker-insert-full"), uid, Filter.Entities(args.User), false);
            return;
        }

        if (cookable.Recipe == null ||
            !_prototypeManager.TryIndex(cookable.Recipe, out TimedCookerRecipePrototype? recipe))
            return;

        // Attempt to insert the item

        if (!_containerSystem.Insert(args.Used, component.Container))
            return;

        // Play the inserting sound (if any)
        if (component.InsertingSound != null)
        {
            _audioSystem.PlayEntity(component.InsertingSound, uid, uid);
        }

        //Queue it up
        if (cookable.Recipe != null)
        {
            component.Queue.Enqueue(recipe);
        }
    }

    public override void Update(float frameTime)
    {
        foreach (var uid in _producingAddQueue)
        {
            EnsureComp<TimedCookerProducingComponent>(uid);
        }

        _producingAddQueue.Clear();

        foreach (var uid in _producingRemoveQueue)
        {
            RemComp<TimedCookerProducingComponent>(uid);
        }

        _producingRemoveQueue.Clear();

        var cookers = EntityQueryEnumerator<TimedCookerComponent>();

        while (cookers.MoveNext(out var owner, out var cooker))
        {
            // Time frame stuff
            if (!cooker.IsRunning)
                continue;
            cooker.ElapsedTime += frameTime;
            if (cooker.ElapsedTime >= cooker.TimeThreshold)
            {
                // Has wood, keep cooking
                if (cooker.FuelStorage > 0)
                {
                    cooker.FuelStorage -= 10;
                    // Did it run out?
                    if (cooker.FuelStorage <= 0)
                    {
                        UpdateAppearance(owner, cooker, false);
                        cooker.IsRunning = false;
                    }
                    cooker.ElapsedTime = 0;
                }
                // Carry on
                else
                {
                    //UpdateAppearance(cooker.Owner, false);
                    cooker.ElapsedTime = 0;
                }
            }

            if (cooker.ProducingRecipe == null)
            {
                if (cooker.Queue.Count > 0)
                {
                    Produce(cooker, cooker.Queue.Dequeue(), owner);
                    return;
                }
            }
            if (cooker.ProducingRecipe != null && cooker.ProducingAccumulator < cooker.ProducingRecipe.CompleteTime.TotalSeconds)
            {
                cooker.ProducingAccumulator += frameTime;
                continue;
            }

            cooker.ProducingAccumulator = 0;
            if (cooker.ProducingRecipe != null)
                FinishProducing(cooker.ProducingRecipe, cooker, owner);
        }
    }

    /// <summary>
    /// If we were able to produce the recipe,
    /// spawn it and cleanup. If we weren't, just do cleanup.
    /// </summary>
    private void FinishProducing(TimedCookerRecipePrototype recipe, TimedCookerComponent component, EntityUid owner, bool productionSucceeded = true)
    {
        component.ProducingRecipe = null;
        if (productionSucceeded)
        {
            foreach (var result in recipe.Result)
            {
                EntityManager.SpawnEntity(result, Comp<TransformComponent>(owner).Coordinates);
            }
        }

        // Play sound
        if (component.ProducingSound != null)
        {
            _audioSystem.PlayEntity(component.ProducingSound, owner, owner);
        }

        // Continue to next in queue if there are items left
        if (component.Queue.Count > 0)
        {
            Produce(component, component.Queue.Dequeue(), owner);
            return;
        }
        _producingRemoveQueue.Enqueue(owner);
        _containerSystem.CleanContainer(component.Container);
    }

    /// <summary>
    /// This handles the checks to start producing an item
    /// </summary>
    private void Produce(TimedCookerComponent component, TimedCookerRecipePrototype recipe, EntityUid owner)
    {
        component.ProducingRecipe = recipe;
        _producingAddQueue.Enqueue(owner);
    }

    /// <summary>
    /// Update appearance of the TimedCooker
    /// </summary>
    /// <param name="uid">EntityUID</param>
    /// <param name="component">TimedCookerComponent</param>
    /// <param name="isFired">bool, if its on or not</param>
    private void UpdateAppearance(EntityUid uid, TimedCookerComponent component, bool isFired)
    {
        _appearanceSystem.SetData(uid, TimedCookerState.Fired, isFired);
        component.LightOn = isFired;
        _lightSystem.SetEnabled(uid, component.LightOn);
    }
}
