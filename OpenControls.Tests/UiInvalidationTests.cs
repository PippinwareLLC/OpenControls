using Xunit;

namespace OpenControls.Tests;

public sealed class UiInvalidationTests
{
    private sealed class TestElement : UiElement
    {
    }

    [Fact]
    public void BasePropertySetters_TrackInvalidationReasonsAndAvoidSameValueChurn()
    {
        TestElement element = new();

        Assert.Equal(UiInvalidationReason.None, element.LocalInvalidationReasons);
        Assert.Equal(0, element.LocalInvalidationVersion);

        element.Bounds = new UiRect(10, 20, 30, 40);
        long boundsVersion = element.LocalInvalidationVersion;
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Layout));
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Paint));
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Clip));
        Assert.Equal(boundsVersion, element.SubtreeInvalidationVersion);

        element.Bounds = new UiRect(10, 20, 30, 40);
        Assert.Equal(boundsVersion, element.LocalInvalidationVersion);

        element.Visible = false;
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Visibility));
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.State));
        Assert.True(element.LocalInvalidationVersion > boundsVersion);

        long visibilityVersion = element.LocalInvalidationVersion;
        element.Enabled = false;
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.State));
        Assert.True(element.LocalInvalidationVersion > visibilityVersion);

        long enabledVersion = element.LocalInvalidationVersion;
        element.ClipChildren = true;
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Clip));
        Assert.True(element.LocalInvalidationVersion > enabledVersion);

        long clipVersion = element.LocalInvalidationVersion;
        element.RenderCacheRootEnabled = true;
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.State));
        Assert.True(element.LocalInvalidationVersion > clipVersion);

        long cacheRootVersion = element.LocalInvalidationVersion;
        element.Font = UiFont.Default;
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Text));
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Style));
        Assert.True(element.LocalInvalidationVersion > cacheRootVersion);

        long fontVersion = element.LocalInvalidationVersion;
        element.Id = "runtime-root";
        Assert.True(element.LocalInvalidationReasons.HasFlag(UiInvalidationReason.State));
        Assert.True(element.LocalInvalidationVersion > fontVersion);
    }

    [Fact]
    public void AddChild_PropagatesPreexistingChildInvalidationToParentSubtree()
    {
        TestElement parent = new();
        TestElement child = new()
        {
            Bounds = new UiRect(4, 6, 20, 30)
        };

        long childVersion = child.SubtreeInvalidationVersion;

        parent.AddChild(child);

        Assert.Same(parent, child.Parent);
        Assert.True(parent.SubtreeInvalidationVersion >= childVersion);
        Assert.True(parent.SubtreeInvalidationReasons.HasFlag(UiInvalidationReason.Children));
        Assert.True(parent.SubtreeInvalidationReasons.HasFlag(UiInvalidationReason.Layout));
        Assert.True(parent.SubtreeInvalidationReasons.HasFlag(UiInvalidationReason.Paint));
        Assert.True(parent.SubtreeInvalidationReasons.HasFlag(UiInvalidationReason.Clip));
        Assert.True(parent.SubtreeInvalidationReasons.HasFlag(UiInvalidationReason.Parent));
    }

    [Fact]
    public void AttachedChildMutation_BubblesInvalidationVersionToAncestors()
    {
        TestElement root = new();
        TestElement child = new();
        root.AddChild(child);

        long before = root.SubtreeInvalidationVersion;

        child.Visible = false;

        Assert.True(root.SubtreeInvalidationVersion > before);
        Assert.True(root.SubtreeInvalidationReasons.HasFlag(UiInvalidationReason.Visibility));
        Assert.True(root.SubtreeInvalidationReasons.HasFlag(UiInvalidationReason.State));
    }

    [Fact]
    public void RemoveChild_MarksParentAndDetachedChild()
    {
        TestElement parent = new();
        TestElement child = new();
        parent.AddChild(child);

        long parentVersionBeforeRemove = parent.LocalInvalidationVersion;
        long childVersionBeforeRemove = child.LocalInvalidationVersion;

        Assert.True(parent.RemoveChild(child));

        Assert.Null(child.Parent);
        Assert.True(parent.LocalInvalidationVersion > parentVersionBeforeRemove);
        Assert.True(child.LocalInvalidationVersion > childVersionBeforeRemove);
        Assert.True(parent.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Children));
        Assert.True(child.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Parent));
        Assert.True(child.SubtreeInvalidationReasons.HasFlag(UiInvalidationReason.Parent));
    }

    [Fact]
    public void CommonDisplayControls_MarkInvalidationWhenRenderedPropertiesChange()
    {
        OpenControls.Controls.UiLabel label = new();
        long labelVersion = label.LocalInvalidationVersion;
        label.Text = "Booting...";
        Assert.True(label.LocalInvalidationVersion > labelVersion);
        Assert.True(label.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Text));

        long labelTextVersion = label.LocalInvalidationVersion;
        label.Scale = 2;
        Assert.True(label.LocalInvalidationVersion > labelTextVersion);
        Assert.True(label.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Layout));

        OpenControls.Controls.UiTextBlock textBlock = new();
        long textBlockVersion = textBlock.LocalInvalidationVersion;
        textBlock.Wrap = false;
        Assert.True(textBlock.LocalInvalidationVersion > textBlockVersion);
        Assert.True(textBlock.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Text));

        long textBlockWrapVersion = textBlock.LocalInvalidationVersion;
        textBlock.Padding = 12;
        Assert.True(textBlock.LocalInvalidationVersion > textBlockWrapVersion);
        Assert.True(textBlock.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Clip));

        OpenControls.Controls.UiProgressBar progressBar = new();
        long progressVersion = progressBar.LocalInvalidationVersion;
        progressBar.Value = 0.75f;
        Assert.True(progressBar.LocalInvalidationVersion > progressVersion);
        Assert.True(progressBar.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Paint));

        long progressValueVersion = progressBar.LocalInvalidationVersion;
        progressBar.Text = "75%";
        Assert.True(progressBar.LocalInvalidationVersion > progressValueVersion);
        Assert.True(progressBar.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Text));

        OpenControls.Controls.UiButton button = new();
        long buttonVersion = button.LocalInvalidationVersion;
        button.Text = "Reveal Output";
        Assert.True(button.LocalInvalidationVersion > buttonVersion);
        Assert.True(button.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Text));

        long buttonTextVersion = button.LocalInvalidationVersion;
        button.CornerRadius = 6;
        Assert.True(button.LocalInvalidationVersion > buttonTextVersion);
        Assert.True(button.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Clip));

        OpenControls.Controls.UiImage image = new();
        long imageVersion = image.LocalInvalidationVersion;
        image.ImageSource = new UiDelegateImageSource((_, _) => { });
        Assert.True(image.LocalInvalidationVersion > imageVersion);
        Assert.True(image.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Paint));

        long imageSourceVersion = image.LocalInvalidationVersion;
        image.Padding = 8;
        Assert.True(image.LocalInvalidationVersion > imageSourceVersion);
        Assert.True(image.LocalInvalidationReasons.HasFlag(UiInvalidationReason.Clip));
    }
}
