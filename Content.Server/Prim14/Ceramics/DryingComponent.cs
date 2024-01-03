﻿namespace Content.Server.Prim14.Ceramics;

[RegisterComponent]
[Access(typeof(DryingSystem))]
public sealed partial class DryingComponent : Component
{
    /// <summary>
    /// Time it takes to dry
    /// </summary>
    [ViewVariables]
    [DataField("dryingTime")]
    public float? DryingTime = 15f;

    [DataField("result")]
    public string Result = string.Empty;

    public float Accumulator;
}
