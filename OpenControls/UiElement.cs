namespace OpenControls;

public abstract class UiElement
{
    private static long s_nextInvalidationVersion;
    private readonly List<UiElement> _children = new();
    private UiRect _bounds;
    private bool _visible = true;
    private bool _enabled = true;
    private bool _clipChildren;
    private string _id = string.Empty;
    private UiFont? _font;
    private UiInvalidationReason _localInvalidationReasons;
    private UiInvalidationReason _subtreeInvalidationReasons;
    private long _localInvalidationVersion;
    private long _subtreeInvalidationVersion;

    public UiRect Bounds
    {
        get => _bounds;
        set => SetInvalidatingValue(ref _bounds, value, UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.Clip);
    }

    public bool Visible
    {
        get => _visible;
        set => SetInvalidatingValue(ref _visible, value, UiInvalidationReason.Visibility | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.Clip | UiInvalidationReason.State);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetInvalidatingValue(ref _enabled, value, UiInvalidationReason.State | UiInvalidationReason.Paint);
    }

    public virtual bool IsFocusable => false;
    public virtual bool HandlesTabInput => false;
    public virtual bool WantsTextInput => false;
    public virtual bool CapturesPointerInput => IsFocusable;
    public virtual bool IsRenderCacheVolatile(UiContext context) => false;
    public virtual bool ClipChildren
    {
        get => _clipChildren;
        set => SetInvalidatingValue(ref _clipChildren, value, UiInvalidationReason.Clip | UiInvalidationReason.Paint);
    }

    public string Id
    {
        get => _id;
        set => SetInvalidatingValue(ref _id, value ?? string.Empty, UiInvalidationReason.State);
    }

    public UiFont? Font
    {
        get => _font;
        set => SetInvalidatingValue(ref _font, value, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.Style);
    }

    public UiElement? Parent { get; private set; }
    public IReadOnlyList<UiElement> Children => _children;
    public UiInvalidationReason LocalInvalidationReasons => _localInvalidationReasons;
    public UiInvalidationReason SubtreeInvalidationReasons => _subtreeInvalidationReasons;
    public long LocalInvalidationVersion => _localInvalidationVersion;
    public long SubtreeInvalidationVersion => _subtreeInvalidationVersion;
    public bool IsLocallyInvalidated => _localInvalidationReasons != UiInvalidationReason.None;
    public bool IsSubtreeInvalidated => _subtreeInvalidationReasons != UiInvalidationReason.None;

    public virtual UiRect ClipBounds => Bounds;

    public void AddChild(UiElement child)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        if (child.Parent != null)
        {
            throw new InvalidOperationException("Child already has a parent.");
        }

        _children.Add(child);
        child.Parent = this;
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
        child.NotifyAttachedToParent();
    }

    public bool RemoveChild(UiElement child)
    {
        if (_children.Remove(child))
        {
            Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
            child.Parent = null;
            child.MarkDetachedInvalidation(UiInvalidationReason.Parent);
            return true;
        }

        return false;
    }

    public void Invalidate(UiInvalidationReason reason)
    {
        if (reason == UiInvalidationReason.None)
        {
            return;
        }

        long version = NextInvalidationVersion();
        MarkLocalInvalidation(reason, version);
        PropagateSubtreeInvalidation(reason, version);
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

    protected internal virtual bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        cursor = UiMouseCursor.Arrow;
        return false;
    }

    protected internal virtual bool TryGetTextInputRequest(out UiTextInputRequest request)
    {
        request = default;
        return false;
    }

    protected internal virtual UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = UiItemStatusFlags.None;
        if (hovered)
        {
            status |= UiItemStatusFlags.Hovered;
        }

        if (focused)
        {
            status |= UiItemStatusFlags.Focused | UiItemStatusFlags.Active;
        }

        if (hovered && (input.LeftClicked || input.RightClicked || input.MiddleClicked))
        {
            status |= UiItemStatusFlags.Clicked;
        }

        return status;
    }

    protected UiFont ResolveFont(UiFont defaultFont)
    {
        UiElement? current = this;
        while (current != null)
        {
            if (current.Font != null)
            {
                return current.Font;
            }

            current = current.Parent;
        }

        return defaultFont;
    }

    protected bool SetInvalidatingValue<T>(ref T field, T value, UiInvalidationReason reason)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Invalidate(reason);
        return true;
    }

    private void NotifyAttachedToParent()
    {
        long version = NextInvalidationVersion();
        MarkLocalInvalidation(UiInvalidationReason.Parent, version);
        PropagateSubtreeInvalidation(_subtreeInvalidationReasons | UiInvalidationReason.Parent, version);
    }

    private void MarkLocalInvalidation(UiInvalidationReason reason)
    {
        MarkLocalInvalidation(reason, NextInvalidationVersion());
    }

    private void MarkLocalInvalidation(UiInvalidationReason reason, long version)
    {
        if (reason == UiInvalidationReason.None)
        {
            return;
        }

        _localInvalidationReasons |= reason;
        _localInvalidationVersion = version;
    }

    private void MarkDetachedInvalidation(UiInvalidationReason reason)
    {
        long version = NextInvalidationVersion();
        MarkLocalInvalidation(reason, version);
        _subtreeInvalidationReasons |= reason;
        if (version > _subtreeInvalidationVersion)
        {
            _subtreeInvalidationVersion = version;
        }
    }

    private void PropagateSubtreeInvalidation(UiInvalidationReason reason, long version)
    {
        if (reason == UiInvalidationReason.None)
        {
            return;
        }

        bool alreadyTracked = (_subtreeInvalidationReasons & reason) == reason;
        if (alreadyTracked && version <= _subtreeInvalidationVersion)
        {
            return;
        }

        _subtreeInvalidationReasons |= reason;
        if (version > _subtreeInvalidationVersion)
        {
            _subtreeInvalidationVersion = version;
        }

        Parent?.PropagateSubtreeInvalidation(reason, version);
    }

    private static long NextInvalidationVersion()
    {
        return Interlocked.Increment(ref s_nextInvalidationVersion);
    }
}
