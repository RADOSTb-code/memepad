using MemePad.Core.Model;

namespace MemePad.Core.Input;

/// <summary>
/// Turns a raw stream of key transitions into hotkey actions. Fires on the main key's
/// first key-down while exactly the bound modifiers are held; auto-repeat is ignored until key-up.
/// </summary>
public sealed class HotkeyMatcher
{
    private readonly HashSet<int> _heldModifierKeys = [];
    private readonly HashSet<int> _heldKeys = [];
    private IReadOnlyDictionary<Hotkey, object> _bindings = new Dictionary<Hotkey, object>();

    public void SetBindings(IReadOnlyDictionary<Hotkey, object> map) => _bindings = map;

    public IReadOnlyList<object> OnInput(InputEvent e)
    {
        if (KeyNames.ModifierOf(e.VirtualKey) != Mods.None)
        {
            if (e.IsDown) _heldModifierKeys.Add(e.VirtualKey);
            else _heldModifierKeys.Remove(e.VirtualKey);
            return [];
        }

        if (!e.IsDown)
        {
            _heldKeys.Remove(e.VirtualKey);
            return [];
        }

        if (!_heldKeys.Add(e.VirtualKey)) return [];   // auto-repeat

        var mods = Mods.None;
        foreach (var vk in _heldModifierKeys) mods |= KeyNames.ModifierOf(vk);

        return _bindings.TryGetValue(new Hotkey(mods, e.VirtualKey), out var action) ? [action] : [];
    }

    /// <summary>Sounds on <paramref name="page"/> other than <paramref name="exceptSound"/> already bound to <paramref name="candidate"/>.</summary>
    public static IReadOnlyList<Guid> FindConflicts(HotkeyPage page, Hotkey candidate, Guid exceptSound) =>
        page.Bindings.Where(b => b.Key != exceptSound && b.Value == candidate).Select(b => b.Key).ToList();
}
