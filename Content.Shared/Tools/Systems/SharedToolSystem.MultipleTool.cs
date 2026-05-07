using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Prying.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Tools.Systems;

public abstract partial class SharedToolSystem
{
    public void InitializeMultipleTool()
    {
        SubscribeLocalEvent<MultipleToolComponent, ComponentStartup>(OnMultipleToolStartup);
        SubscribeLocalEvent<MultipleToolComponent, AfterAutoHandleStateEvent>(OnMultipleToolHandleState);
        SubscribeLocalEvent<MultipleToolComponent, GetVerbsEvent<AlternativeVerb>>(OnMultipleToolGetAltVerbs);
        SubscribeLocalEvent<MultipleToolComponent, MultipleToolSelectedMessage>(OnMultipleToolSelected);
    }

    private void OnMultipleToolHandleState(EntityUid uid, MultipleToolComponent component, ref AfterAutoHandleStateEvent args)
    {
        SetMultipleTool(uid, component);
    }

    private void OnMultipleToolStartup(EntityUid uid, MultipleToolComponent multiple, ComponentStartup args)
    {
        // Only set the multiple tool if we have a tool component.
        if (TryComp(uid, out ToolComponent? tool))
            SetMultipleTool(uid, multiple, tool);
    }

    private void OnMultipleToolGetAltVerbs(EntityUid uid, MultipleToolComponent multiple, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || multiple.Entries.Length <= 1)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("multiple-tool-component-cycle-verb"),
            Act = () => CycleMultipleTool(uid, multiple, user),
            Priority = -1,
        });
    }

    private void OnMultipleToolSelected(EntityUid uid, MultipleToolComponent multiple, MultipleToolSelectedMessage args)
    {
        if (args.Index >= multiple.Entries.Length)
            return;

        if (multiple.CurrentEntry == args.Index)
            return;

        multiple.CurrentEntry = args.Index;
        SetMultipleTool(uid, multiple, playSound: true, user: args.Actor);
    }

    public bool CycleMultipleTool(EntityUid uid, MultipleToolComponent? multiple = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref multiple))
            return false;

        if (multiple.Entries.Length == 0)
            return false;

        multiple.CurrentEntry = (uint)((multiple.CurrentEntry + 1) % multiple.Entries.Length);
        SetMultipleTool(uid, multiple, playSound: true, user: user);

        return true;
    }

    public virtual void SetMultipleTool(EntityUid uid,
        MultipleToolComponent? multiple = null,
        ToolComponent? tool = null,
        bool playSound = false,
        EntityUid? user = null)
    {
        if (!Resolve(uid, ref multiple, ref tool))
            return;

        Dirty(uid, multiple);

        if (multiple.Entries.Length <= multiple.CurrentEntry)
        {
            multiple.CurrentQualityName = Loc.GetString("multiple-tool-component-no-behavior");
            return;
        }

        var current = multiple.Entries[multiple.CurrentEntry];
        tool.UseSound = current.UseSound;
        tool.Qualities = current.Behavior;

        // TODO: Replace this with a better solution later
        if (TryComp<PryingComponent>(uid, out var pryComp))
        {
            pryComp.Enabled = current.Behavior.Contains("Prying");
        }

        if (playSound && current.ChangeSound != null)
            _audioSystem.PlayPredicted(current.ChangeSound, uid, user);

        if (_protoMan.TryIndex(current.Behavior.First(), out ToolQualityPrototype? quality))
            multiple.CurrentQualityName = Loc.GetString(quality.Name);
    }
}

