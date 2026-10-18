namespace MemePad.Core.Input;

/// <summary>One key or mouse-button transition seen by the global input hook.</summary>
public readonly record struct InputEvent(int VirtualKey, bool IsDown);
