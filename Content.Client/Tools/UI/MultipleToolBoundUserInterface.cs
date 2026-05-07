using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Tools.UI;

[UsedImplicitly]
public sealed class MultipleToolBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SimpleRadialMenu? _menu;

    public MultipleToolBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<MultipleToolComponent>(Owner, out var multiple))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _menu.SetButtons(BuildButtons(multiple));
        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> BuildButtons(MultipleToolComponent multiple)
    {
        var buttons = new List<RadialMenuOptionBase>(multiple.Entries.Length);
        for (var i = 0; i < multiple.Entries.Length; i++)
        {
            var entry = multiple.Entries[i];
            var firstQuality = entry.Behavior.FirstOrDefault();
            ToolQualityPrototype? qualityProto = null;
            if (firstQuality != null)
                _prototypeManager.TryIndex(firstQuality, out qualityProto);

            var icon = RadialMenuIconSpecifier.With(entry.Sprite)
                       ?? RadialMenuIconSpecifier.With(qualityProto?.Icon);

            var tooltip = qualityProto != null
                ? Loc.GetString(qualityProto.Name)
                : firstQuality ?? string.Empty;

            var index = (uint)i;
            buttons.Add(new RadialMenuActionOption<uint>(OnButtonPressed, index)
            {
                IconSpecifier = icon,
                ToolTip = tooltip,
            });
        }
        return buttons;
    }

    private void OnButtonPressed(uint index)
    {
        SendPredictedMessage(new MultipleToolSelectedMessage(index));
    }
}
