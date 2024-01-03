namespace Content.Server.Prim14.TimedCooker;

[RegisterComponent]
public sealed partial class TimedCookableComponent : Component
{
    /// <summary>
    /// Valid recipe when insert into TimedCookerComponent
    /// </summary>
    [ViewVariables]
    [DataField("recipe", required: true)]
    public string? Recipe;
}
