using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types.Tier;

namespace PanoramaLayout;

/// <summary>
/// Minimal native bridge for server-driven Custom HUD state experiments. Addresses resolve from
/// PanoramaLayout's own gamedata key so the probe does not depend on the complete CustomHud module.
/// </summary>
internal sealed unsafe class CustomHudNativeProbe
{
    internal const string DialogVariableAddressKey =
        "CCSCustomHudLayout::SetDialogVariableString";

    internal const string HasClassAddressKey =
        "CCSCustomHudLayout::SetHasClass";

    internal const string HasClassForPlayerAddressKey =
        "CCSCustomHudLayout::SetHasClassForPlayer";

    internal const string InputCaptureAddressKey =
        "CCSCustomHudLayout::SetInputCaptureEnabled";

    private readonly IGameData _gameData;
    private readonly ILogger<CustomHudNativeProbe> _logger;

    private delegate* unmanaged<nint, CUtlString*, CUtlString*, CUtlString*, void> _dialogVariableSetter;
    private delegate* unmanaged<nint, CUtlString*, CUtlString*, byte, void> _hasClassSetter;
    private delegate* unmanaged<nint, uint, CUtlString*, CUtlString*, byte, void> _hasClassForPlayerSetter;
    private delegate* unmanaged<nint, uint, byte, void> _inputCaptureSetter;
    private bool _dialogVariableResolved;
    private bool _hasClassResolved;
    private bool _hasClassForPlayerResolved;
    private bool _inputCaptureResolved;

    public CustomHudNativeProbe(
        IGameData gameData,
        ILogger<CustomHudNativeProbe> logger)
    {
        _gameData = gameData;
        _logger = logger;
    }

    public bool TrySetDialogVariable(
        IBaseEntity entity,
        string panelId,
        string variableName,
        string value)
    {
        ResolveDialogVariableSetter();

        if (_dialogVariableSetter is null)
            return false;

        var panel = new CUtlString(panelId);
        var name = new CUtlString(variableName);
        var variableValue = new CUtlString(value);

        try
        {
            _dialogVariableSetter(entity.GetAbsPtr(), &panel, &name, &variableValue);
        }
        finally
        {
            panel.Dispose();
            name.Dispose();
            variableValue.Dispose();
        }

        return true;
    }

    public bool TrySetHasClass(
        IBaseEntity entity,
        string panelId,
        string className,
        bool enabled)
    {
        ResolveHasClassSetter();

        if (_hasClassSetter is null)
            return false;

        var panel = new CUtlString(panelId);
        var panelClass = new CUtlString(className);

        try
        {
            _hasClassSetter(entity.GetAbsPtr(), &panel, &panelClass, enabled ? (byte)1 : (byte)0);
        }
        finally
        {
            panel.Dispose();
            panelClass.Dispose();
        }

        return true;
    }

    public bool TrySetHasClassForPlayer(
        IBaseEntity entity,
        uint slot,
        string panelId,
        string className,
        bool enabled)
    {
        ResolveHasClassForPlayerSetter();

        if (_hasClassForPlayerSetter is null)
            return false;

        var panel = new CUtlString(panelId);
        var panelClass = new CUtlString(className);

        try
        {
            _hasClassForPlayerSetter(
                entity.GetAbsPtr(),
                slot,
                &panel,
                &panelClass,
                enabled ? (byte)1 : (byte)0);
        }
        finally
        {
            panel.Dispose();
            panelClass.Dispose();
        }

        return true;
    }

    public bool TrySetInputCaptureEnabled(IBaseEntity entity, uint slot, bool enabled)
    {
        ResolveInputCaptureSetter();

        if (_inputCaptureSetter is null)
            return false;

        _inputCaptureSetter(entity.GetAbsPtr(), slot, enabled ? (byte)1 : (byte)0);
        return true;
    }

    private void ResolveDialogVariableSetter()
    {
        if (_dialogVariableResolved)
            return;

        _dialogVariableResolved = true;

        if (!TryResolve(DialogVariableAddressKey, out var address))
            return;

        _dialogVariableSetter =
            (delegate* unmanaged<nint, CUtlString*, CUtlString*, CUtlString*, void>)address;
    }

    private void ResolveHasClassSetter()
    {
        if (_hasClassResolved)
            return;

        _hasClassResolved = true;

        if (!TryResolve(HasClassAddressKey, out var address))
            return;

        _hasClassSetter =
            (delegate* unmanaged<nint, CUtlString*, CUtlString*, byte, void>)address;
    }

    private void ResolveHasClassForPlayerSetter()
    {
        if (_hasClassForPlayerResolved)
            return;

        _hasClassForPlayerResolved = true;

        if (!TryResolve(HasClassForPlayerAddressKey, out var address))
            return;

        _hasClassForPlayerSetter =
            (delegate* unmanaged<nint, uint, CUtlString*, CUtlString*, byte, void>)address;
    }

    private void ResolveInputCaptureSetter()
    {
        if (_inputCaptureResolved)
            return;

        _inputCaptureResolved = true;

        if (!TryResolve(InputCaptureAddressKey, out var address))
            return;

        _inputCaptureSetter =
            (delegate* unmanaged<nint, uint, byte, void>)address;
    }

    private bool TryResolve(string addressKey, out nint address)
    {
        if (!_gameData.GetAddress(addressKey, out address) || address == 0)
        {
            _logger.LogWarning("Native address {AddressKey} did not resolve", addressKey);
            address = 0;
            return false;
        }

        _logger.LogInformation("Resolved {AddressKey} at 0x{Address:X}", addressKey, address);
        return true;
    }
}
