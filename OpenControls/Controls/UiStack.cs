namespace OpenControls.Controls;

public enum UiLayoutOrientation
{
    Horizontal,
    Vertical
}

public enum UiStackAlignment
{
    Start,
    Center,
    End,
    Stretch,
    Baseline
}

public sealed class UiStackItemLayout
{
    public UiLayoutLength PrimaryLength { get; set; } = UiLayoutLength.Auto;
    public UiLayoutLength CrossLength { get; set; } = UiLayoutLength.Auto;
    public UiStackAlignment? Alignment { get; set; }
    public int MinPrimarySize { get; set; }
    public int MaxPrimarySize { get; set; } = int.MaxValue;
    public int MinCrossSize { get; set; }
    public int MaxCrossSize { get; set; } = int.MaxValue;
    public int? BaselineOffset { get; set; }
}

public sealed class UiStack : UiElement
{
    private readonly Dictionary<UiElement, UiStackItemLayout> _layouts = new();

    public UiLayoutOrientation Orientation { get; set; } = UiLayoutOrientation.Horizontal;
    public UiStackAlignment CrossAlignment { get; set; } = UiStackAlignment.Stretch;
    public UiThickness Padding { get; set; } = UiThickness.Uniform(0);
    public int Gap { get; set; } = 4;
    public UiColor Background { get; set; } = UiColor.Transparent;
    public UiColor Border { get; set; } = UiColor.Transparent;
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }

    public UiRect ContentBounds
    {
        get
        {
            UiThickness padding = Padding;
            int x = Bounds.X + padding.Left;
            int y = Bounds.Y + padding.Top;
            int width = Math.Max(0, Bounds.Width - padding.Horizontal);
            int height = Math.Max(0, Bounds.Height - padding.Vertical);
            return new UiRect(x, y, width, height);
        }
    }

    public void AddChild(UiElement child, UiStackItemLayout? layout)
    {
        base.AddChild(child);
        if (layout != null)
        {
            _layouts[child] = layout;
        }
    }

    public UiStackItemLayout GetLayout(UiElement child)
    {
        if (!_layouts.TryGetValue(child, out UiStackItemLayout? layout))
        {
            layout = new UiStackItemLayout();
            _layouts[child] = layout;
        }

        return layout;
    }

    public void SetLayout(UiElement child, UiStackItemLayout layout)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        if (layout == null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        _layouts[child] = layout;
    }

    public new bool RemoveChild(UiElement child)
    {
        if (base.RemoveChild(child))
        {
            _layouts.Remove(child);
            return true;
        }

        return false;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        LayoutChildren(context.DefaultFont);
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        LayoutChildren(context.DefaultFont);

        if (Background.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        base.Render(context);

        if (ClipChildren && CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }
    }

    private void LayoutChildren(UiFont defaultFont)
    {
        List<UiElement> visibleChildren = new();
        foreach (UiElement child in Children)
        {
            if (child.Visible)
            {
                visibleChildren.Add(child);
            }
        }

        UiRect content = ContentBounds;
        if (visibleChildren.Count == 0)
        {
            return;
        }

        int contentPrimary = GetPrimarySize(content);
        int contentCross = GetCrossSize(content);
        int gap = Math.Max(0, Gap);
        int availablePrimary = Math.Max(0, contentPrimary - gap * Math.Max(0, visibleChildren.Count - 1));

        int[] primarySizes = new int[visibleChildren.Count];
        int[] crossSizes = new int[visibleChildren.Count];
        int[] baselineOffsets = new int[visibleChildren.Count];
        UiStackAlignment[] alignments = new UiStackAlignment[visibleChildren.Count];

        int fixedPrimary = 0;
        float totalWeight = 0f;

        for (int i = 0; i < visibleChildren.Count; i++)
        {
            UiElement child = visibleChildren[i];
            UiStackItemLayout layout = GetLayout(child);
            alignments[i] = layout.Alignment ?? CrossAlignment;

            int preferredPrimary = Math.Max(0, GetPrimarySize(child.Bounds));
            if (layout.PrimaryLength.Kind is UiLayoutLengthKind.Fill or UiLayoutLengthKind.Weight)
            {
                totalWeight += layout.PrimaryLength.Kind == UiLayoutLengthKind.Fill
                    ? 1f
                    : Math.Max(0.01f, layout.PrimaryLength.Value);
            }
            else
            {
                int resolvedPrimary = ResolvePrimarySize(layout, preferredPrimary, availablePrimary, 0, 1f);
                primarySizes[i] = resolvedPrimary;
                fixedPrimary += resolvedPrimary;
            }
        }

        int remainingPrimary = Math.Max(0, availablePrimary - fixedPrimary);
        for (int i = 0; i < visibleChildren.Count; i++)
        {
            UiElement child = visibleChildren[i];
            UiStackItemLayout layout = GetLayout(child);
            if (layout.PrimaryLength.Kind is not (UiLayoutLengthKind.Fill or UiLayoutLengthKind.Weight))
            {
                continue;
            }

            float weight = layout.PrimaryLength.Kind == UiLayoutLengthKind.Fill
                ? 1f
                : Math.Max(0.01f, layout.PrimaryLength.Value);
            primarySizes[i] = ResolvePrimarySize(layout, Math.Max(0, GetPrimarySize(child.Bounds)), remainingPrimary, totalWeight, weight);
        }

        bool useBaseline = Orientation == UiLayoutOrientation.Horizontal;
        int maxBaseline = 0;

        for (int i = 0; i < visibleChildren.Count; i++)
        {
            UiElement child = visibleChildren[i];
            UiStackItemLayout layout = GetLayout(child);
            UiStackAlignment alignment = alignments[i];
            int preferredCross = Math.Max(0, GetCrossSize(child.Bounds));
            crossSizes[i] = ResolveCrossSize(layout, preferredCross, contentCross, alignment);

            baselineOffsets[i] = ResolveBaselineOffset(child, layout, defaultFont, crossSizes[i]);
            if (useBaseline && alignment == UiStackAlignment.Baseline)
            {
                maxBaseline = Math.Max(maxBaseline, baselineOffsets[i]);
            }
        }

        int primaryPosition = GetPrimaryStart(content);
        for (int i = 0; i < visibleChildren.Count; i++)
        {
            UiStackAlignment alignment = alignments[i];
            int crossPosition = ResolveCrossPosition(content, contentCross, crossSizes[i], alignment, maxBaseline, baselineOffsets[i]);
            SetChildBounds(visibleChildren[i], primaryPosition, crossPosition, primarySizes[i], crossSizes[i]);
            primaryPosition += primarySizes[i] + gap;
        }
    }

    private int ResolvePrimarySize(UiStackItemLayout layout, int preferredSize, int availablePrimary, float totalWeight, float itemWeight)
    {
        int resolved = layout.PrimaryLength.Kind switch
        {
            UiLayoutLengthKind.Fixed => (int)Math.Round(layout.PrimaryLength.Value),
            UiLayoutLengthKind.Percentage => (int)Math.Round(availablePrimary * layout.PrimaryLength.Value),
            UiLayoutLengthKind.Fill or UiLayoutLengthKind.Weight => totalWeight > 0f
                ? (int)Math.Round(availablePrimary * (itemWeight / totalWeight))
                : 0,
            _ => preferredSize
        };

        int maxPrimary = Math.Max(layout.MinPrimarySize, layout.MaxPrimarySize);
        return Math.Clamp(Math.Max(0, resolved), layout.MinPrimarySize, maxPrimary);
    }

    private int ResolveCrossSize(UiStackItemLayout layout, int preferredSize, int contentCross, UiStackAlignment alignment)
    {
        int resolved = layout.CrossLength.Kind switch
        {
            UiLayoutLengthKind.Fixed => (int)Math.Round(layout.CrossLength.Value),
            UiLayoutLengthKind.Percentage => (int)Math.Round(contentCross * layout.CrossLength.Value),
            UiLayoutLengthKind.Fill or UiLayoutLengthKind.Weight => contentCross,
            _ => alignment == UiStackAlignment.Stretch ? contentCross : preferredSize
        };

        int maxCross = Math.Max(layout.MinCrossSize, layout.MaxCrossSize);
        return Math.Clamp(Math.Max(0, resolved), layout.MinCrossSize, maxCross);
    }

    private int ResolveCrossPosition(
        UiRect content,
        int contentCross,
        int childCross,
        UiStackAlignment alignment,
        int maxBaseline,
        int childBaseline)
    {
        int crossStart = Orientation == UiLayoutOrientation.Horizontal ? content.Y : content.X;
        return alignment switch
        {
            UiStackAlignment.Center => crossStart + Math.Max(0, (contentCross - childCross) / 2),
            UiStackAlignment.End => crossStart + Math.Max(0, contentCross - childCross),
            UiStackAlignment.Baseline when Orientation == UiLayoutOrientation.Horizontal => crossStart + Math.Max(0, maxBaseline - childBaseline),
            _ => crossStart
        };
    }

    private int ResolveBaselineOffset(UiElement child, UiStackItemLayout layout, UiFont defaultFont, int crossSize)
    {
        if (layout.BaselineOffset.HasValue)
        {
            return Math.Clamp(layout.BaselineOffset.Value, 0, Math.Max(0, crossSize));
        }

        UiFont font = ResolveFont(child, defaultFont);
        return child switch
        {
            UiLabel label => font.GetMetrics(label.Scale).Baseline,
            UiTextBlock block => block.Padding + font.GetMetrics(block.Scale).Baseline,
            UiButton button => CenteredTextBaseline(font, button.TextScale, crossSize),
            UiCheckbox checkbox => CenteredTextBaseline(font, checkbox.TextScale, crossSize),
            UiRadioButton radioButton => CenteredTextBaseline(font, radioButton.TextScale, crossSize),
            UiSelectable selectable => CenteredTextBaseline(font, selectable.TextScale, crossSize),
            UiTextField field => CenteredTextBaseline(font, field.TextScale, crossSize),
            _ => Math.Max(0, crossSize)
        };
    }

    private static int CenteredTextBaseline(UiFont font, int scale, int height)
    {
        UiFontMetrics metrics = font.GetMetrics(scale);
        return Math.Max(0, (height - metrics.LineHeight) / 2) + metrics.Baseline;
    }

    private static UiFont ResolveFont(UiElement element, UiFont defaultFont)
    {
        UiElement? current = element;
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

    private int GetPrimaryStart(UiRect bounds)
    {
        return Orientation == UiLayoutOrientation.Horizontal ? bounds.X : bounds.Y;
    }

    private int GetPrimarySize(UiRect bounds)
    {
        return Orientation == UiLayoutOrientation.Horizontal ? bounds.Width : bounds.Height;
    }

    private int GetCrossSize(UiRect bounds)
    {
        return Orientation == UiLayoutOrientation.Horizontal ? bounds.Height : bounds.Width;
    }

    private void SetChildBounds(UiElement child, int primary, int cross, int primarySize, int crossSize)
    {
        child.Bounds = Orientation == UiLayoutOrientation.Horizontal
            ? new UiRect(primary, cross, primarySize, crossSize)
            : new UiRect(cross, primary, crossSize, primarySize);
    }
}
