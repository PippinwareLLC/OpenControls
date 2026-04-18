using System.Text;

namespace OpenControls;

public abstract class UiElement
{
    private static long s_nextInvalidationVersion;
    private readonly List<UiElement> _children = new();
    private UiRect _bounds;
    private bool _visible = true;
    private bool _enabled = true;
    private bool _clipChildren;
    private bool _renderCacheRootEnabled;
    private string _id = string.Empty;
    private string _automationId = string.Empty;
    private string _automationName = string.Empty;
    private string _automationRole = string.Empty;
    private string[] _automationTags = Array.Empty<string>();
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
    public virtual bool IsRenderCacheRoot(UiContext context) => RenderCacheRootEnabled;
    public virtual bool ClipChildren
    {
        get => _clipChildren;
        set => SetInvalidatingValue(ref _clipChildren, value, UiInvalidationReason.Clip | UiInvalidationReason.Paint);
    }

    public bool RenderCacheRootEnabled
    {
        get => _renderCacheRootEnabled;
        set => SetInvalidatingValue(ref _renderCacheRootEnabled, value, UiInvalidationReason.State | UiInvalidationReason.Paint);
    }

    public string Id
    {
        get => _id;
        set => SetInvalidatingValue(ref _id, value ?? string.Empty, UiInvalidationReason.State);
    }

    public string AutomationId
    {
        get => _automationId;
        set => SetInvalidatingValue(ref _automationId, value ?? string.Empty, UiInvalidationReason.State);
    }

    public string AutomationName
    {
        get => _automationName;
        set => SetInvalidatingValue(ref _automationName, value ?? string.Empty, UiInvalidationReason.State);
    }

    public string AutomationRole
    {
        get => _automationRole;
        set => SetInvalidatingValue(ref _automationRole, value ?? string.Empty, UiInvalidationReason.State);
    }

    public IReadOnlyList<string> AutomationTags => _automationTags;

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

    public void SetAutomationTags(params string[] tags)
    {
        SetAutomationTags((IEnumerable<string>?)tags);
    }

    public void SetAutomationTags(IEnumerable<string>? tags)
    {
        string[] normalized = NormalizeAutomationTags(tags);
        if (AreAutomationTagsEqual(_automationTags, normalized))
        {
            return;
        }

        _automationTags = normalized;
        Invalidate(UiInvalidationReason.State);
    }

    public bool HasAutomationTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        string candidate = tag.Trim();
        for (int i = 0; i < _automationTags.Length; i++)
        {
            if (string.Equals(_automationTags[i], candidate, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public bool MatchesAutomationId(string automationId)
    {
        if (string.IsNullOrWhiteSpace(automationId))
        {
            return false;
        }

        return string.Equals(ResolveAutomationId(), automationId, StringComparison.Ordinal);
    }

    public string? ResolveAutomationId()
    {
        if (!string.IsNullOrWhiteSpace(AutomationId))
        {
            return AutomationId;
        }

        return string.IsNullOrWhiteSpace(Id) ? null : Id;
    }

    public string? ResolveAutomationName(string? fallbackText = null)
    {
        if (!string.IsNullOrWhiteSpace(AutomationName))
        {
            return AutomationName;
        }

        if (!string.IsNullOrWhiteSpace(fallbackText))
        {
            return fallbackText;
        }

        return ResolveAutomationId();
    }

    public string ResolveAutomationRole()
    {
        if (!string.IsNullOrWhiteSpace(AutomationRole))
        {
            return AutomationRole;
        }

        return DeriveAutomationRole(GetType().Name);
    }

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

        int childCount = _children.Count;
        if (childCount == 0)
        {
            return;
        }

        UiElement[] snapshot = System.Buffers.ArrayPool<UiElement>.Shared.Rent(childCount);
        try
        {
            for (int index = 0; index < childCount; index++)
            {
                snapshot[index] = _children[index];
            }

            for (int index = 0; index < childCount; index++)
            {
                UiElement child = snapshot[index];
                if (ReferenceEquals(child.Parent, this))
                {
                    child.Update(context.CreateChildContext(this, child));
                }
            }
        }
        finally
        {
            Array.Clear(snapshot, 0, childCount);
            System.Buffers.ArrayPool<UiElement>.Shared.Return(snapshot);
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
            context.RenderChild(child);
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
            context.RenderChildOverlay(child);
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

    private static bool AreAutomationTagsEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string[] NormalizeAutomationTags(IEnumerable<string>? tags)
    {
        if (tags == null)
        {
            return Array.Empty<string>();
        }

        List<string>? normalized = null;
        HashSet<string>? seen = null;
        foreach (string? tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            string trimmed = tag.Trim();
            seen ??= new HashSet<string>(StringComparer.Ordinal);
            if (!seen.Add(trimmed))
            {
                continue;
            }

            normalized ??= new List<string>();
            normalized.Add(trimmed);
        }

        return normalized?.ToArray() ?? Array.Empty<string>();
    }

    private static string DeriveAutomationRole(string typeName)
    {
        ReadOnlySpan<char> source = typeName.AsSpan();
        if (source.StartsWith("Ui", StringComparison.Ordinal))
        {
            source = source[2..];
        }

        if (source.IsEmpty)
        {
            return "element";
        }

        StringBuilder builder = new(source.Length + 8);
        for (int i = 0; i < source.Length; i++)
        {
            char current = source[i];
            if (char.IsUpper(current) && i > 0 && source[i - 1] != '-')
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.Length == 0 ? "element" : builder.ToString();
    }
}
