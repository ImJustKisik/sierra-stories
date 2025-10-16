using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRadarConsoleSystem))]
public sealed partial class RadarConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float RangeVV
    {
        get => MaxRange;
        set => IoCManager
            .Resolve<IEntitySystemManager>()
            .GetEntitySystem<SharedRadarConsoleSystem>()
            .SetRange(Owner, value, this);
    }

    [DataField, AutoNetworkedField]
    public float MaxRange = 256f;

    /// <summary>
    /// If true, the radar will be centered on the entity. If not - on the grid on which it is located.
    /// </summary>
    [DataField]
    public bool FollowEntity = false;

    /// <summary>
    /// When true the console will refuse to provide a radar feed unless a powered emitter is nearby.
    /// </summary>
    [DataField("requireEmitter")]
    public bool RequireEmitter = false;

    /// <summary>
    /// Maximum allowed distance in world units between the console and the external emitter.
    /// Only used when <see cref="RequireEmitter"/> is true.
    /// </summary>
    [DataField("requiredEmitterRange")]
    public float RequiredEmitterRange = 0f;

    /// <summary>
    /// Explicitly linked emitter entity. When set, the console will prefer (or require) this emitter.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("linkedEmitter")]
    public EntityUid? LinkedEmitter;
}
