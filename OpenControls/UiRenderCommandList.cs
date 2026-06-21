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

    internal sealed class DrawPolylineCommand : UiRenderCommand
    {
        private readonly IReadOnlyList<UiPoint> _points;
        private readonly int _thickness;
        private readonly UiColor _color;

        public DrawPolylineCommand(IReadOnlyList<UiPoint> points, int thickness, UiColor color)
        {
            _points = points ?? throw new ArgumentNullException(nameof(points));
            _thickness = Math.Max(1, thickness);
            _color = color;
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiVectorRenderer vectorRenderer)
            {
                vectorRenderer.DrawPolyline(_points, _thickness, _color);
                return;
            }

            UiRenderHelpers.DrawPolylineFallback(context.Renderer, _points, _thickness, _color);
        }
    }

    internal sealed class BeginVectorPassCommand : UiRenderCommand
    {
        public static BeginVectorPassCommand Instance { get; } = new();

        private BeginVectorPassCommand()
        {
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiVectorPassRenderer vectorPassRenderer)
            {
                vectorPassRenderer.BeginVectorPass();
            }
        }
    }

    internal sealed class DrawPolylineTransformedCommand : UiRenderCommand
    {
        private readonly IReadOnlyList<UiPoint> _points;
        private readonly int _thickness;
        private readonly UiColor _color;
        private readonly UiPoint _origin;
        private readonly float _zoom;
        private readonly float _panX;
        private readonly float _panY;

        public DrawPolylineTransformedCommand(
            IReadOnlyList<UiPoint> points,
            int thickness,
            UiColor color,
            UiPoint origin,
            float zoom,
            float panX,
            float panY)
        {
            _points = points ?? throw new ArgumentNullException(nameof(points));
            _thickness = Math.Max(1, thickness);
            _color = color;
            _origin = origin;
            _zoom = zoom;
            _panX = panX;
            _panY = panY;
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiTransformedVectorRenderer transformedRenderer)
            {
                transformedRenderer.DrawPolylineTransformed(_points, _thickness, _color, _origin, _zoom, _panX, _panY);
                return;
            }

            UiPoint[] transformed = new UiPoint[_points.Count];
            for (int i = 0; i < _points.Count; i++)
            {
                transformed[i] = new UiPoint(
                    _origin.X + (int)MathF.Round((_points[i].X - _panX) * _zoom),
                    _origin.Y + (int)MathF.Round((_points[i].Y - _panY) * _zoom));
            }

            if (context.Renderer is IUiVectorRenderer vectorRenderer)
            {
                vectorRenderer.DrawPolyline(transformed, _thickness, _color);
                return;
            }

            UiRenderHelpers.DrawPolylineFallback(context.Renderer, transformed, _thickness, _color);
        }
    }

    internal sealed class EndVectorPassCommand : UiRenderCommand
    {
        public static EndVectorPassCommand Instance { get; } = new();

        private EndVectorPassCommand()
        {
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiVectorPassRenderer vectorPassRenderer)
            {
                vectorPassRenderer.EndVectorPass();
            }
        }
    }

    internal sealed class FillRoundedRectCommand : UiRenderCommand
    {
        private readonly UiRect _rect;
        private readonly int _radius;
        private readonly UiColor _color;

        public FillRoundedRectCommand(UiRect rect, int radius, UiColor color)
        {
            _rect = rect;
            _radius = radius;
            _color = color;
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiShapeRenderer shapeRenderer)
            {
                shapeRenderer.FillRoundedRect(_rect, _radius, _color);
                return;
            }

            UiRenderHelpers.FillRectRoundedFallback(context.Renderer, _rect, _radius, _color);
        }
    }

    internal sealed class FillTopRoundedRectCommand : UiRenderCommand
    {
        private readonly UiRect _rect;
        private readonly int _radius;
        private readonly UiColor _color;

        public FillTopRoundedRectCommand(UiRect rect, int radius, UiColor color)
        {
            _rect = rect;
            _radius = radius;
            _color = color;
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiShapeRenderer shapeRenderer)
            {
                shapeRenderer.FillTopRoundedRect(_rect, _radius, _color);
                return;
            }

            UiRenderHelpers.FillRectTopRoundedFallback(context.Renderer, _rect, _radius, _color);
        }
    }

    internal sealed class DrawRoundedRectCommand : UiRenderCommand
    {
        private readonly UiRect _rect;
        private readonly int _radius;
        private readonly UiColor _color;
        private readonly int _thickness;

        public DrawRoundedRectCommand(UiRect rect, int radius, UiColor color, int thickness)
        {
            _rect = rect;
            _radius = radius;
            _color = color;
            _thickness = thickness;
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiShapeRenderer shapeRenderer)
            {
                shapeRenderer.DrawRoundedRect(_rect, _radius, _color, _thickness);
                return;
            }

            UiRenderHelpers.DrawRectRoundedFallback(context.Renderer, _rect, _radius, _color, _thickness);
        }
    }

    internal sealed class DrawTopRoundedRectCommand : UiRenderCommand
    {
        private readonly UiRect _rect;
        private readonly int _radius;
        private readonly UiColor _color;
        private readonly int _thickness;

        public DrawTopRoundedRectCommand(UiRect rect, int radius, UiColor color, int thickness)
        {
            _rect = rect;
            _radius = radius;
            _color = color;
            _thickness = thickness;
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiShapeRenderer shapeRenderer)
            {
                shapeRenderer.DrawTopRoundedRect(_rect, _radius, _color, _thickness);
                return;
            }

            UiRenderHelpers.DrawRectTopRoundedFallback(context.Renderer, _rect, _radius, _color, _thickness);
        }
    }

    internal sealed class FillCircleCommand : UiRenderCommand
    {
        private readonly UiPoint _center;
        private readonly int _radius;
        private readonly UiColor _color;

        public FillCircleCommand(UiPoint center, int radius, UiColor color)
        {
            _center = center;
            _radius = radius;
            _color = color;
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiShapeRenderer shapeRenderer)
            {
                shapeRenderer.FillCircle(_center, _radius, _color);
                return;
            }

            UiRenderHelpers.FillCircleFallback(context.Renderer, _center, _radius, _color);
        }
    }

    internal sealed class DrawCircleCommand : UiRenderCommand
    {
        private readonly UiPoint _center;
        private readonly int _radius;
        private readonly UiColor _color;
        private readonly int _thickness;

        public DrawCircleCommand(UiPoint center, int radius, UiColor color, int thickness)
        {
            _center = center;
            _radius = radius;
            _color = color;
            _thickness = thickness;
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiShapeRenderer shapeRenderer)
            {
                shapeRenderer.DrawCircle(_center, _radius, _color, _thickness);
                return;
            }

            UiRenderHelpers.DrawCircleFallback(context.Renderer, _center, _radius, _color, _thickness);
        }
    }

    internal sealed class FillTriangleRightCommand : UiRenderCommand
    {
        private readonly UiRect _rect;
        private readonly UiColor _color;

        public FillTriangleRightCommand(UiRect rect, UiColor color)
        {
            _rect = rect;
            _color = color;
        }

        public override void Replay(UiRenderContext context)
        {
            if (context.Renderer is IUiShapeRenderer shapeRenderer)
            {
                shapeRenderer.FillTriangleRight(_rect, _color);
                return;
            }

            UiRenderHelpers.FillTriangleRightFallback(context.Renderer, _rect, _color);
        }
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
