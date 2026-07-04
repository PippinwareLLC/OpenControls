using System.Text.Json.Serialization;

namespace OpenControls.State;

/// <summary>
/// Source-generated serializer metadata for UI state persistence so consumers that
/// publish with trimming/AOT (e.g. iOS Release builds) never fall back to
/// reflection-based System.Text.Json.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UiStateSnapshot))]
internal sealed partial class UiStateJsonContextIndented : JsonSerializerContext;

[JsonSerializable(typeof(UiStateSnapshot))]
internal sealed partial class UiStateJsonContextCompact : JsonSerializerContext;
