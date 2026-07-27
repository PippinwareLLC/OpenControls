namespace OpenControls;

/// <summary>
/// Owns the terminal boundary for one renderer pass.
/// </summary>
/// <remarks>
/// A successful pass is completed exactly once. A failed pass is aborted so
/// any queued commands or transient clip/vector state are discarded without
/// being submitted. <see cref="AbortRenderPass"/> must be safe to call more
/// than once for the same failed pass.
/// </remarks>
public interface IUiRenderPassController : IUiRenderer
{
    void CompleteRenderPass();

    void AbortRenderPass();
}
