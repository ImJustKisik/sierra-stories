using Content.Shared.Shuttles.Systems;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Marker component for consoles that should use the advanced radar UI.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedRadarConsoleSystem))]
public sealed partial class AdvancedRadarConsoleComponent : Component
{
    [DataField]
    public bool Active = true;

    [DataField]
    public float PassiveRangeModifier = 0.4f;

    [DataField]
    public float PassiveNoiseDegrees = 12f;

    [DataField]
    public float PassiveDistanceNoise = 30f;

    [DataField]
    public float PassiveRefreshInterval = 1.2f;
}
