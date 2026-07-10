namespace OpenControls.Controls;

/// <summary>
/// Opaque, single-use token for returning a group detached from a
/// <see cref="UiDockWorkspace"/> to its exact source position.
/// </summary>
/// <remarks>
/// Create leases with <see cref="UiDockWorkspace.BeginExternalDockGroup"/>.
/// A lease remains active until it is restored or explicitly abandoned.
/// </remarks>
public sealed class UiDockExternalGroupLease
{
    private readonly UiDockWorkspace _workspace;
    private readonly long _leaseId;

    internal UiDockExternalGroupLease(UiDockWorkspace workspace, long leaseId)
    {
        _workspace = workspace;
        _leaseId = leaseId;
    }

    /// <summary>
    /// Gets whether this lease can still be restored or abandoned.
    /// </summary>
    public bool IsActive => _workspace.IsExternalDockGroupLeaseActive(this, _leaseId);

    /// <summary>
    /// Relinquishes the recorded return position without changing the current
    /// dock layout. An abandoned lease cannot be restored.
    /// </summary>
    public void Abandon()
    {
        _workspace.AbandonExternalDockGroup(this, _leaseId);
    }

    internal bool BelongsTo(UiDockWorkspace workspace, out long leaseId)
    {
        leaseId = _leaseId;
        return ReferenceEquals(_workspace, workspace);
    }
}
