using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class RadarConsoleToggleActiveMessage : BoundUserInterfaceMessage
{
    public readonly bool Active;

    public RadarConsoleToggleActiveMessage(bool active)
    {
        Active = active;
    }
}

[Serializable, NetSerializable]
public sealed class RadarConsoleLinkEmitterMessage : BoundUserInterfaceMessage
{
}
