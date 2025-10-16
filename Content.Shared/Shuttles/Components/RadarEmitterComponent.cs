using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Physical radar/emitter hardware mounted on a shuttle/grid.
/// Consoles on the same grid require an emitter to provide a radar feed.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class RadarEmitterComponent : Component
{
    /// <summary>
    /// Maximum hardware range in world units the emitter can provide when active.
    /// </summary>
    [DataField]
    public float MaxRange = 512f;

    /// <summary>
    /// If false, consoles should present basic UI even if they are advanced.
    /// </summary>
    [DataField]
    public bool Advanced = true;

    /// <summary>
    /// Whether the emitter is online. If false, no radar feed is available.
    /// Hook up to power / damage later.
    /// </summary>
    [DataField]
    public bool Enabled = true;
}

