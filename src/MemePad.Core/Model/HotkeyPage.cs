using MemePad.Core.Input;

namespace MemePad.Core.Model;

/// <summary>A named set of sound-to-hotkey bindings; only the active page's bindings are live.</summary>
public sealed record HotkeyPage(Guid Id, string Name, Dictionary<Guid, Hotkey> Bindings);
