namespace OpenControls.Controls;

public sealed class UiTabItem : UiElement
{
    private bool _isActive;

    public UiTabItem()
    {
        ClipChildren = true;
    }

    public string Text { get; set; } = string.Empty;

    public bool IsActive => _isActive;

    internal UiRect TabBounds { get; set; }

    internal void SetActive(bool active)
    {
        _isActive = active;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled || !_isActive)
        {
            return;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible || !_isActive)
        {
            return;
        }

        base.Render(context);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible || !_isActive)
        {
            return;
        }

        base.RenderOverlay(context);
    }
}
