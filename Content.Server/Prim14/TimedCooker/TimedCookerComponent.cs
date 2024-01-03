﻿using Content.Shared.Prim14.TimedCooker;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Prim14.TimedCooker;

[RegisterComponent]
[Access(typeof(TimedCookerSystem))] // typeof(KilnSystem)
public sealed partial class TimedCookerComponent : Component
{
    /// <summary>
    /// Container of entities inside to be processed.
    /// </summary>
    [ViewVariables]
    public Container Container = default!;

    /// <summary>
    /// Whitelist for specifying the kind of stuff can be cooked/processed
    /// </summary>
    [ViewVariables]
    [DataField("whitelist", required: true)]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Maximum number of items that can be queued
    /// </summary>
    [ViewVariables]
    [DataField("max")]
    public int? Max = 2;

    /// <summary>
    /// The sound that plays when finished producing the result
    /// </summary>
    [DataField("producingSound")]
    public SoundSpecifier? ProducingSound;

    /// <summary>
    /// The sound that plays when inserting an item
    /// </summary>
    [DataField("insertingSound")]
    public SoundSpecifier? InsertingSound;

    /// <summary>
    /// The recipe that is currently producing
    /// </summary>
    [ViewVariables]
    public TimedCookerRecipePrototype? ProducingRecipe;

    /// <summary>
    /// Production accumulator for the production time.
    /// </summary>
    [ViewVariables]
    [DataField("producingAccumulator")]
    public float ProducingAccumulator;

    /// <summary>
    /// The cooker's construction queue
    /// </summary>
    [ViewVariables]
    public Queue<TimedCookerRecipePrototype> Queue { get; } = new();

    /// <summary>
    /// Used with handling fuel
    /// </summary>
    public bool IsRunning;
    public float ElapsedTime;
    public int TimeThreshold = 5;

    [ViewVariables]
    public int FuelStorage;

    [ViewVariables]
    public bool LightOn;
}
