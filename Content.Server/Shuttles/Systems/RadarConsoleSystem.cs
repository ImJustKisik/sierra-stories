using System.Numerics;
using Content.Server.UserInterface;
using Content.Shared.Popups;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Movement.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadarConsoleComponent, ComponentStartup>(OnRadarStartup);

        Subs.BuiEvents<RadarConsoleComponent>(RadarConsoleUiKey.Key, subs =>
        {
            subs.Event<RadarConsoleToggleActiveMessage>(OnToggleActiveMode);
            subs.Event<RadarConsoleLinkEmitterMessage>(OnLinkEmitterMessage);
        });
    }

    private void OnRadarStartup(EntityUid uid, RadarConsoleComponent component, ComponentStartup args)
    {
        UpdateState(uid, component);
    }

    protected override void UpdateState(EntityUid uid, RadarConsoleComponent component)
    {
        var xform = Transform(uid);
        var onGrid = xform.ParentUid == xform.GridUid;
        EntityCoordinates? coordinates = onGrid ? xform.Coordinates : null;
        Angle? angle = onGrid ? xform.LocalRotation : null;

        if (component.FollowEntity)
        {
            coordinates = new EntityCoordinates(uid, Vector2.Zero);
            angle = Angle.Zero;
        }

        if (_uiSystem.HasUi(uid, RadarConsoleUiKey.Key))
        {
            NavInterfaceState state;
            var docks = _console.GetAllDocks();

            if (coordinates != null && angle != null)
            {
                state = _console.GetNavState(uid, docks, coordinates.Value, angle.Value);
            }
            else
            {
                state = _console.GetNavState(uid, docks);
            }

            state.RotateWithEntity = !component.FollowEntity;

            // Determine hardware availability on the same grid (physical emitter requirement).
            var gridUid = xform.GridUid;
            var requireEmitter = component.RequireEmitter;
            var requiredEmitterRange = component.RequiredEmitterRange;
            var rangeLimited = requireEmitter && requiredEmitterRange > 0f;
            var requiredEmitterRangeSq = requiredEmitterRange * requiredEmitterRange;
            var hasEmitter = false;
            var emitterMaxRange = 0f;
            var emitterAdvanced = false;
            var emitterEnabled = false;

            if (gridUid != null)
            {
                var linkedUsed = false;

                if (component.LinkedEmitter is { } linked && linked.IsValid() &&
                    TryComp(linked, out RadarEmitterComponent? linkedEmitter) &&
                    TryComp(linked, out TransformComponent? linkedXform) &&
                    linkedXform.GridUid == gridUid)
                {
                    var distanceSq = Vector2.DistanceSquared(linkedXform.LocalPosition, xform.LocalPosition);
                    if (!rangeLimited || distanceSq <= requiredEmitterRangeSq)
                    {
                        hasEmitter = true;
                        emitterMaxRange = linkedEmitter.MaxRange;
                        emitterAdvanced = linkedEmitter.Advanced;
                        emitterEnabled = linkedEmitter.Enabled;
                        linkedUsed = true;
                    }
                }

                if (!linkedUsed && component.LinkedEmitter.HasValue)
                {
                    component.LinkedEmitter = null;
                    Dirty(uid, component);
                }

                if (!hasEmitter)
                {
                    // Find the best emitter on the same grid (highest MaxRange)
                    var emitEnum = EntityQueryEnumerator<RadarEmitterComponent, TransformComponent>();
                    while (emitEnum.MoveNext(out _, out var emitter, out var emXform))
                    {
                        if (emXform.GridUid != gridUid)
                            continue;

                        if (!emitter.Enabled)
                            continue;

                        var distanceSq = Vector2.DistanceSquared(emXform.LocalPosition, xform.LocalPosition);
                        if (rangeLimited && distanceSq > requiredEmitterRangeSq)
                            continue;

                        if (!hasEmitter || emitter.MaxRange > emitterMaxRange)
                        {
                            hasEmitter = true;
                            emitterMaxRange = emitter.MaxRange;
                            emitterAdvanced = emitter.Advanced;
                            emitterEnabled = emitter.Enabled;
                        }
                    }
                }
            }

            // Default to no signal if no hardware or hardware disabled
            if (!hasEmitter || !emitterEnabled)
            {
                state.IsAdvanced = false;
                state.IsRadarActive = false;
                state.MaxRange = 0f;
                state.PassiveRangeModifier = 1f;
                state.PassiveNoiseDegrees = 0f;
                state.PassiveDistanceNoise = 0f;
                state.PassiveRefreshInterval = 1f;

                // Force client to show "No signal"
                state.Coordinates = null;
                state.Angle = null;
                _uiSystem.SetUiState(uid, RadarConsoleUiKey.Key, new NavBoundUserInterfaceState(state));
                return;
            }

            // Console capabilities + hardware capabilities combined
            var maxRange = MathF.Min(component.MaxRange, emitterMaxRange);

            if (TryComp<AdvancedRadarConsoleComponent>(uid, out var advanced))
            {
                state.IsAdvanced = emitterAdvanced; // require hardware to allow advanced UI
                state.IsRadarActive = advanced.Active;
                state.PassiveRangeModifier = advanced.PassiveRangeModifier;
                state.PassiveNoiseDegrees = advanced.PassiveNoiseDegrees;
                state.PassiveDistanceNoise = advanced.PassiveDistanceNoise;
                state.PassiveRefreshInterval = advanced.PassiveRefreshInterval;

                if (!advanced.Active)
                    maxRange *= advanced.PassiveRangeModifier;
            }
            else
            {
                state.IsAdvanced = false;
                state.IsRadarActive = true;
                state.PassiveRangeModifier = 1f;
                state.PassiveNoiseDegrees = 0f;
                state.PassiveDistanceNoise = 0f;
                state.PassiveRefreshInterval = 1f;
            }

            state.MaxRange = maxRange;

            _uiSystem.SetUiState(uid, RadarConsoleUiKey.Key, new NavBoundUserInterfaceState(state));
        }
    }

    private void OnLinkEmitterMessage(EntityUid uid, RadarConsoleComponent component, RadarConsoleLinkEmitterMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        var xform = Transform(uid);
        var gridUid = xform.GridUid;

        if (gridUid == null)
        {
            _popup.PopupEntity(Loc.GetString("radar-console-link-failed"), uid, player);
            return;
        }

        var requireEmitter = component.RequireEmitter;
        var requiredEmitterRange = component.RequiredEmitterRange;
        var rangeLimited = requireEmitter && requiredEmitterRange > 0f;
        var requiredEmitterRangeSq = requiredEmitterRange * requiredEmitterRange;

        var bestEmitter = EntityUid.Invalid;
        RadarEmitterComponent? bestEmitterComp = null;
        TransformComponent? bestEmitterXform = null;
        var bestDistanceSq = float.MaxValue;

        var emitEnum = EntityQueryEnumerator<RadarEmitterComponent, TransformComponent>();
        while (emitEnum.MoveNext(out var emitterUid, out var emitter, out var emXform))
        {
            if (emXform.GridUid != gridUid)
                continue;

            if (!emitter.Enabled)
                continue;

            var distanceSq = Vector2.DistanceSquared(emXform.LocalPosition, xform.LocalPosition);
            if (rangeLimited && distanceSq > requiredEmitterRangeSq)
                continue;

            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestEmitter = emitterUid;
                bestEmitterComp = emitter;
                bestEmitterXform = emXform;
            }
        }

        if (bestEmitterComp == null || bestEmitterXform == null)
        {
            _popup.PopupEntity(Loc.GetString("radar-console-link-failed"), uid, player);
            return;
        }

        component.LinkedEmitter = bestEmitter;
        Dirty(uid, component);

        var emitterName = Comp<MetaDataComponent>(bestEmitter).EntityName;
        _popup.PopupEntity(Loc.GetString("radar-console-link-success", ("name", emitterName)), uid, player);
        UpdateState(uid, component);
    }

    private void OnToggleActiveMode(EntityUid uid, RadarConsoleComponent component, RadarConsoleToggleActiveMessage msg)
    {
        if (!TryComp(uid, out AdvancedRadarConsoleComponent? advanced))
            return;

        if (advanced.Active == msg.Active)
            return;

        advanced.Active = msg.Active;
        UpdateState(uid, component);
    }
}
