namespace OpenControls;

internal sealed class UiRenderCommandList
{
    private readonly IReadOnlyList<UiRenderCommand> _commands;

    public UiRenderCommandList(IReadOnlyList<UiRenderCommand> commands)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public int Count => _commands.Count;

    public void Replay(UiRenderContext context)
    {
        for (int i = 0; i < _commands.Count; i++)
        {
            _commands[i].Replay(context);
        }
    }

    internal abstract class UiRenderCommand
    {
        public abstract void Replay(UiRenderContext context);
    }

    internal sealed class FillRectCommand : UiRenderCommand
    {
        private readonly UiRect _rect;
        private readonly UiColor _color;

        public FillRectCommand(UiRect rect, UiColor color)
        {
            _rect = rect;
            _color = color;
        }

        public override void Replay(UiRenderContext context) => context.Renderer.FillRect(_rect, _color);
    }

    internal sealed class DrawRectCommand : UiRenderCommand
    {
        private readonly UiRect _rect;
        private readonly UiColor _color;
        private readonly int _thickness;

        public DrawRectCommand(UiRect rect, UiColor color, int thickness)
        {
            _rect = rect;
            _color = color;
            _thickness = thickness;
        }

        public override void Replay(UiRenderContext context) => context.Renderer.DrawRect(_rect, _color, _thickness);
    }

    internal sealed class FillRectGradientCommand : UiRenderCommand
    {
        private readonly UiRect _rect;
        private readonly UiColor _topLeft;
        private readonly UiColor _topRight;
        private readonly UiColor _bottomLeft;
        private readonly UiColor _bottomRight;

        public FillRectGradientCommand(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
            _rect = rect;
            _topLeft = topLeft;
            _topRight = topRight;
            _bottomLeft = bottomLeft;
            _bottomRight = bottomRight;
        }

        public override void Replay(UiRenderContext context) => context.Renderer.FillRectGradient(_rect, _topLeft, _topRight, _bottomLeft, _bottomRight);
    }

    internal sealed class FillRectCheckerboardCommand : UiRenderCommand
    {
        private readonly UiRect _rect;
        private readonly int _cellSize;
        private readonly UiColor _colorA;
        private readonly UiColor _colorB;

        public FillRectCheckerboardCommand(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
            _rect = rect;
            _cellSize = cellSize;
            _colorA = colorA;
            _colorB = colorB;
        }

        public override void Replay(UiRenderContext context) => context.Renderer.FillRectCheckerboard(_rect, _cellSize, _colorA, _colorB);
    }

    internal sealed class DrawTextCommand : UiRenderCommand
    {
        private readonly string _text;
        private readonly UiPoint _position;
        private readonly UiColor _color;
        private readonly int _scale;
        private readonly UiFont _font;

        public DrawTextCommand(string text, UiPoint position, UiColor color, int scale, UiFont font)
        {
            _text = text ?? string.Empty;
            _position = position;
            _color = color;
            _scale = scale;
            _font = font ?? UiFont.Default;
        }

        public override void Replay(UiRenderContext context) => context.Renderer.DrawText(_text, _position, _color, _scale, _font);
    }

    internal sealed class PushClipCommand : UiRenderCommand
    {
        private readonly UiRect _rect;

        public PushClipCommand(UiRect rect)
        {
            _rect = rect;
        }

        public override void Replay(UiRenderContext context) => context.Renderer.PushClip(_rect);
    }

    internal sealed class PopClipCommand : UiRenderCommand
    {
        public static PopClipCommand Instance { get; } = new();

        private PopClipCommand()
        {
        }

        public override void Replay(UiRenderContext context) => context.Renderer.PopClip();
    }

    internal sealed class RenderSubtreeCommand : UiRenderCommand
    {
        private readonly UiElement _element;
        private readonly UiRenderPassKind _passKind;

        public RenderSubtreeCommand(UiElement element, UiRenderPassKind passKind)
        {
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _passKind = passKind;
        }

        public override void Replay(UiRenderContext context)
        {
            if (_passKind == UiRenderPassKind.Overlay)
            {
                context.RenderChildOverlay(_element);
            }
            else
            {
                context.RenderChild(_element);
            }
        }
    }
}
