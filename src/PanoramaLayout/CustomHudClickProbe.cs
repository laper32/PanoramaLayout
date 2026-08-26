using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Hooks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PanoramaLayout;

/// <summary>
/// Detours the receiver reached by CS_UM_CustomHudClicked (message 390). This is intentionally the
/// raw receiver rather than the point_script-gated Pulse output, so it also works on normal maps.
/// </summary>
internal sealed unsafe class CustomHudClickProbe : IDisposable
{
    internal const string ClickReceiverAddressKey =
        "CCSCustomHudLayout::CustomHudClickedReceiver";

    private readonly IGameData _gameData;
    private readonly ILogger<CustomHudClickProbe> _logger;
    private readonly IDetourHook _hook;
    private readonly Action<nint, nint, string> _onClick;
    private bool _installed;

    private static CustomHudClickProbe? _active;
    private static delegate* unmanaged<nint, nint, nint, nint, void> _trampoline;

    public CustomHudClickProbe(
        ISharedSystem sharedSystem,
        IGameData gameData,
        ILogger<CustomHudClickProbe> logger,
        Action<nint, nint, string> onClick)
    {
        _gameData = gameData;
        _logger = logger;
        _onClick = onClick;
        _hook = sharedSystem.GetHookManager().CreateDetourHook();
    }

    public bool TryInstall()
    {
        if (_installed)
            return true;

        if (_active is not null)
        {
            _logger.LogWarning("Another CustomHudClickProbe is already active");
            return false;
        }

        if (!_gameData.GetAddress(ClickReceiverAddressKey, out var target) || target == 0)
        {
            _logger.LogWarning("Native address {AddressKey} did not resolve", ClickReceiverAddressKey);
            return false;
        }

        _active = this;
        _hook.Prepare(
            target,
            (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)(&OnClicked));

        if (!_hook.Install())
        {
            _active = null;
            _logger.LogWarning("Failed to install click receiver hook at 0x{Target:X}", target);
            return false;
        }

        _trampoline =
            (delegate* unmanaged<nint, nint, nint, nint, void>)_hook.Trampoline;
        _installed = true;
        _logger.LogInformation(
            "Installed {AddressKey} hook at 0x{Target:X}",
            ClickReceiverAddressKey,
            target);
        return true;
    }

    public void Dispose()
    {
        if (_installed)
        {
            _hook.Uninstall();
            _installed = false;
        }

        _hook.Dispose();

        if (ReferenceEquals(_active, this))
        {
            _active = null;
            _trampoline = null;
        }
    }

    [UnmanagedCallersOnly]
    private static void OnClicked(
        nint pulseBinding,
        nint controller,
        nint layoutEntity,
        nint buttonIdString)
    {
        try
        {
            if (_active is not { } active || buttonIdString == 0)
                return;

            var dataPointer = *(nint*)buttonIdString;
            if (dataPointer == 0)
                return;

            var buttonId = Marshal.PtrToStringUTF8(dataPointer);
            if (!string.IsNullOrWhiteSpace(buttonId))
                active._onClick(controller, layoutEntity, buttonId);
        }
        catch (Exception exception)
        {
            _active?._logger.LogError(exception, "Custom HUD click receiver threw");
        }
        finally
        {
            if (_trampoline is not null)
                _trampoline(pulseBinding, controller, layoutEntity, buttonIdString);
        }
    }
}
