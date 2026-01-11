namespace OpenControls;

public abstract class UiElement
{
    private readonly List<UiElement> _children = new();

    public UiRect Bounds { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public virtual bool IsFocusable => false;
    public virtual bool ClipChildren { get; set; }
    public string Id { get; set; } = string.Empty;
    public UiElement? Parent { get; private set; }
    public IReadOnlyList<UiElement> Children => _children;

    public virtual UiRect ClipBounds => Bounds;

    public void AddChild(UiElement child)
    {
        if (child.Parent != null)
        {
            throw new InvalidOperationException("Child already has a parent.");
        }

        _children.Add(child);
        child.Parent = this;
    }

    public bool RemoveChild(UiElement child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            return true;
        }

        return false;
    }

    public virtual void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        foreach (UiElement child in _children)
        {
            child.Update(context);
        }
    }

    public virtual void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        if (ClipChildren)
        {
            context.Renderer.PushClip(ClipBounds);
        }

        foreach (UiElement child in _children)
        {
            child.Render(context);
        }

        if (ClipChildren)
        {
            context.Renderer.PopClip();
        }
    }

    public virtual void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        foreach (UiElement child in _children)
        {
            child.RenderOverlay(context);
        }
    }

    public virtual UiElement? HitTest(UiPoint point)
    {
        if (!Visible || !Bounds.Contains(point))
        {
            return null;
        }

        for (int i = _children.Count - 1; i >= 0; i--)
        {
            UiElement? childHit = _children[i].HitTest(point);
            if (childHit != null)
            {
                return childHit;
            }
        }

        return this;
    }

    protected internal virtual void OnFocusGained()
    {
    }

    protected internal virtual void OnFocusLost()
    {
    }
}
