using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using RadarConsoleWindow = Content.Client.Shuttles.UI.RadarConsoleWindow;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class RadarConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RadarConsoleWindow? _window;

    public RadarConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RadarConsoleWindow>();
        _window.Interface = this;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not NavBoundUserInterfaceState cState)
            return;

        _window?.UpdateState(cState.State);
    }

    public void SetActiveMode(bool active)
    {
        SendMessage(new RadarConsoleToggleActiveMessage(active));
    }

    public void LinkEmitter()
    {
        SendMessage(new RadarConsoleLinkEmitterMessage());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_window != null)
            {
                _window.Interface = null;
                _window = null;
            }
        }

        base.Dispose(disposing);
    }
}
