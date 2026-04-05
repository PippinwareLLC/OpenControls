namespace OpenControls;

public enum UiRenderCachePassAction
{
    None,
    RecordAndReplay,
    Replay,
    Bypass
}

public enum UiRenderCacheMissReason
{
    None,
    Disabled,
    Volatile,
    Empty,
    Invalidation,
    Interaction,
    Font
}

public readonly record struct UiRenderCachePassStatisticsSnapshot(
    string Name,
    bool CacheResident,
    long RecordedInvalidationVersion,
    long LastSeenInvalidationVersion,
    int LastInteractionSignature,
    UiRenderCachePassAction LastAction,
    UiRenderCacheMissReason LastMissReason,
    long RecordCount,
    long ReplayCount,
    long BypassCount,
    long DisabledBypassCount,
    long VolatileBypassCount,
    long EmptyMissCount,
    long InvalidationMissCount,
    long InteractionMissCount,
    long FontMissCount)
{
    public static UiRenderCachePassStatisticsSnapshot Empty { get; } = new(
        string.Empty,
        false,
        0,
        0,
        0,
        UiRenderCachePassAction.None,
        UiRenderCacheMissReason.None,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0);
}

public readonly record struct UiRenderCacheStatisticsSnapshot(
    bool RenderCachingEnabled,
    UiInvalidationReason RootInvalidationReasons,
    long RootInvalidationVersion,
    bool VolatileRenderStateDetected,
    string VolatileElementLabel,
    UiRenderCachePassStatisticsSnapshot RootPass,
    UiRenderCachePassStatisticsSnapshot OverlayPass)
{
    public static UiRenderCacheStatisticsSnapshot Empty { get; } = new(
        false,
        UiInvalidationReason.None,
        0,
        false,
        string.Empty,
        UiRenderCachePassStatisticsSnapshot.Empty,
        UiRenderCachePassStatisticsSnapshot.Empty);
}
