using Robust.Shared.Serialization;

namespace Content.Shared.Tools;

[Serializable, NetSerializable]
public enum MultipleToolUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MultipleToolSelectedMessage(uint index) : BoundUserInterfaceMessage
{
    public uint Index = index;
}
