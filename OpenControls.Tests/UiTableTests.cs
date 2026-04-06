using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiTableTests
{
    private sealed class RecordingRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public List<string> DrawnText { get; } = new();

        public void FillRect(UiRect rect, UiColor color)
        {
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
            DrawText(text, position, color, scale, null);
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
            DrawnText.Add(text);
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return MeasureTextWidth(text, scale, null);
        }

        public int MeasureTextWidth(string text, int scale, UiFont? font)
        {
            return (font ?? DefaultFont).MeasureTextWidth(text, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return MeasureTextHeight(scale, null);
        }

        public int MeasureTextHeight(int scale, UiFont? font)
        {
            return (font ?? DefaultFont).MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
        }

        public void PopClip()
        {
        }
    }

    [Fact]
    public void Table_ReorderedVisibleColumnsStillSortByStableModelIndex()
    {
        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, 240, 120),
            RowHeight = 20,
            HeaderHeight = 20
        };
        table.Columns.Add(new UiTableColumn("Name", width: 80) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Columns.Add(new UiTableColumn("Type", width: 80) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Columns.Add(new UiTableColumn("Size", width: 80) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Rows = new[]
        {
            new UiTableRow("Asset 00", "Scene", "64 KB"),
            new UiTableRow("Asset 01", "Texture", "72 KB")
        };
        table.GetColumnState(1).Visible = false;

        Update(table, new UiInputState());
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(120, 10),
            ScreenMousePosition = new UiPoint(120, 10),
            LeftClicked = true,
            LeftDown = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 10),
            ScreenMousePosition = new UiPoint(20, 10),
            LeftDown = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 10),
            ScreenMousePosition = new UiPoint(20, 10),
            LeftReleased = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 10),
            ScreenMousePosition = new UiPoint(20, 10),
            LeftClicked = true,
            LeftDown = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 10),
            ScreenMousePosition = new UiPoint(20, 10),
            LeftReleased = true
        });

        Assert.Single(table.SortSpecs);
        Assert.Equal(2, table.SortSpecs[0].ColumnIndex);
    }

    [Fact]
    public void Table_ViewStateTracksScrollAndVisibleRows()
    {
        UiTable table = CreateTable(220, 120, rowCount: 20);
        table.ScrollY = 40;

        Update(table, new UiInputState());

        Assert.True(table.ViewState.ContentSize.Y > table.ViewState.ViewportSize.Y);
        Assert.Equal(2, table.ViewState.FirstVisibleRowIndex);
        Assert.Equal(6, table.ViewState.LastVisibleRowIndex);
    }

    [Fact]
    public void Table_TryGetResolvedColumnWidthReturnsVisibleResolvedWidth()
    {
        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, 360, 120),
            RowHeight = 20,
            HeaderHeight = 20
        };
        table.Columns.Add(new UiTableColumn("Name", width: 80) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Columns.Add(new UiTableColumn("Type", width: 100) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Columns.Add(new UiTableColumn("Message", weight: 1f) { MinWidth = 120 });
        table.Rows = new[]
        {
            new UiTableRow("Asset", "Info", "Message")
        };

        Update(table, new UiInputState());

        Assert.True(table.TryGetResolvedColumnWidth(2, out int messageWidth));
        Assert.Equal(180, messageWidth);
    }

    [Fact]
    public void Table_RenderUsesCellRenderTextWhenProvided()
    {
        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, 240, 80),
            RowHeight = 24,
            HeaderHeight = 20
        };
        table.Columns.Add(new UiTableColumn("Message", width: 240) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Rows = new[]
        {
            new UiTableRow
            {
                CellItems = new[]
                {
                    new UiTableCell("This is the full value")
                    {
                        RenderText = "This is the display value"
                    }
                }
            }
        };

        Update(table, new UiInputState());
        RecordingRenderer renderer = new();
        table.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.Contains("This is the display value", renderer.DrawnText);
        Assert.DoesNotContain("This is the full value", renderer.DrawnText);
    }

    [Fact]
    public void Table_HeaderContextMenuCanHideColumns()
    {
        UiTable table = CreateTable(240, 120, rowCount: 6);

        Update(table, new UiInputState());
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(12, 10),
            ScreenMousePosition = new UiPoint(12, 10),
            RightClicked = true,
            RightDown = true
        });
        Update(table, new UiInputState());
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 42),
            ScreenMousePosition = new UiPoint(20, 42),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.False(table.GetColumnState(0).Visible);
        Assert.Equal(2, table.ColumnStates.Count(state => state.Visible));
    }

    [Fact]
    public void Table_RichCellContentParticipatesInHitTesting()
    {
        UiLabel content = new()
        {
            Text = "Nested",
            Bounds = new UiRect(0, 0, 0, 0)
        };

        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, 220, 100),
            RowHeight = 28
        };
        table.Columns.Add(new UiTableColumn("Name", weight: 2f));
        table.Columns.Add(new UiTableColumn("Kind", weight: 1f));
        table.Rows = new[]
        {
            new UiTableRow
            {
                CellItems = new[]
                {
                    new UiTableCell { Content = content },
                    new UiTableCell("Folder")
                }
            }
        };

        Update(table, new UiInputState());

        UiElement? hit = table.HitTest(new UiPoint(12, 36));

        Assert.Same(content, hit);
        Assert.True(content.Bounds.Width > 0);
        Assert.True(content.Bounds.Height > 0);
    }

    [Fact]
    public void Table_RowActivated_FiresOnRowDoubleClick()
    {
        UiTable table = CreateTable(220, 120, rowCount: 3);
        int activatedIndex = -1;
        table.RowActivated += index => activatedIndex = index;

        Update(table, new UiInputState());
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 32),
            ScreenMousePosition = new UiPoint(20, 32),
            LeftClicked = true,
            LeftDown = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 32),
            ScreenMousePosition = new UiPoint(20, 32),
            LeftReleased = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 32),
            ScreenMousePosition = new UiPoint(20, 32),
            LeftDoubleClicked = true,
            LeftDown = true
        });

        Assert.Equal(0, activatedIndex);
        Assert.Equal(0, table.SelectedIndex);
    }

    [Fact]
    public void Table_RichCellContentExposesScreenSpaceDebugBoundsThroughUiContext()
    {
        UiLabel content = new()
        {
            Text = "Nested",
            Bounds = new UiRect(0, 0, 0, 0)
        };

        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 400, 240)
        };
        UiTable table = new()
        {
            Bounds = new UiRect(40, 30, 220, 100),
            RowHeight = 28
        };
        table.Columns.Add(new UiTableColumn("Name", weight: 2f));
        table.Columns.Add(new UiTableColumn("Kind", weight: 1f));
        table.Rows = new[]
        {
            new UiTableRow
            {
                CellItems = new[]
                {
                    new UiTableCell { Content = content },
                    new UiTableCell("Folder")
                }
            }
        };
        root.AddChild(table);

        UiContext context = new(root);
        context.Update(new UiInputState());

        UiItemStateSnapshot state = context.GetItemState(content);

        Assert.Contains(content, context.GetDebugChildren(table));
        Assert.True(context.TryGetVisibleBounds(content, out UiRect visibleBounds));
        Assert.True(state.Bounds.X >= table.Bounds.X);
        Assert.True(state.Bounds.Y >= table.Bounds.Y);
        Assert.Equal(state.Bounds, visibleBounds);
        Assert.True(visibleBounds.Contains(new UiPoint(table.Bounds.X + 12, table.Bounds.Y + 36)));
    }

    [Fact]
    public void Table_NestedRichCellDescendantExposesScreenSpaceDebugBoundsThroughUiContext()
    {
        UiPanel content = new()
        {
            Bounds = new UiRect(0, 0, 0, 0)
        };
        UiTextField field = new()
        {
            Bounds = new UiRect(48, 2, 96, 20),
            Text = "42"
        };
        content.AddChild(field);

        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 400, 240)
        };
        UiTable table = new()
        {
            Bounds = new UiRect(40, 30, 260, 100),
            RowHeight = 28
        };
        table.Columns.Add(new UiTableColumn("Name", width: 120) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Columns.Add(new UiTableColumn("Value", width: 140) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Rows = new[]
        {
            new UiTableRow
            {
                CellItems = new[]
                {
                    new UiTableCell("Position"),
                    new UiTableCell { Content = content, Padding = 4 }
                }
            }
        };
        root.AddChild(table);

        UiContext context = new(root);
        context.Update(new UiInputState());

        UiItemStateSnapshot state = context.GetItemState(field);

        Assert.True(context.TryGetVisibleBounds(field, out UiRect visibleBounds));
        Assert.True(state.Bounds.X >= table.Bounds.X);
        Assert.True(state.Bounds.Y >= table.Bounds.Y);
        Assert.True(visibleBounds.X >= state.Bounds.X);
        Assert.True(visibleBounds.Y >= state.Bounds.Y);
        Assert.True(visibleBounds.Right <= state.Bounds.Right);
        Assert.True(visibleBounds.Bottom <= state.Bounds.Bottom);
        Assert.True(visibleBounds.Contains(new UiPoint(table.Bounds.X + 176, table.Bounds.Y + 40)));
    }

    [Fact]
    public void NestedScrollAndHeaderContainersExposeScreenSpaceDebugBoundsForTableDescendants()
    {
        UiPanel content = new()
        {
            Bounds = new UiRect(0, 0, 0, 0)
        };
        UiTextField field = new()
        {
            Bounds = new UiRect(48, 2, 96, 20),
            Text = "42"
        };
        content.AddChild(field);

        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 800, 600)
        };
        UiScrollPanel scrollPanel = new()
        {
            Bounds = new UiRect(420, 80, 260, 300)
        };
        UiCollapsingHeader header = new()
        {
            Bounds = new UiRect(0, 0, 240, 180),
            HeaderHeight = 28,
            ContentPadding = 6,
            IsOpen = true
        };
        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, 240, 100),
            RowHeight = 28
        };
        table.Columns.Add(new UiTableColumn("Name", width: 120) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Columns.Add(new UiTableColumn("Value", width: 120) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Rows = new[]
        {
            new UiTableRow
            {
                CellItems = new[]
                {
                    new UiTableCell("Position"),
                    new UiTableCell { Content = content, Padding = 4 }
                }
            }
        };

        header.AddChild(table);
        scrollPanel.AddChild(header);
        root.AddChild(scrollPanel);

        UiContext context = new(root);
        context.Update(new UiInputState());

        UiItemStateSnapshot state = context.GetItemState(field);

        Assert.True(context.TryGetVisibleBounds(field, out UiRect visibleBounds));
        Assert.True(visibleBounds.X >= scrollPanel.Bounds.X);
        Assert.True(visibleBounds.Y >= scrollPanel.Bounds.Y);
        Assert.True(visibleBounds.Right <= scrollPanel.Bounds.Right);
        Assert.True(visibleBounds.Bottom <= scrollPanel.Bounds.Bottom);
        Assert.True(visibleBounds.X >= state.Bounds.X);
        Assert.True(visibleBounds.Y >= state.Bounds.Y);
        Assert.True(visibleBounds.Right <= state.Bounds.Right);
        Assert.True(visibleBounds.Bottom <= state.Bounds.Bottom);
    }

    private static UiTable CreateTable(int width, int height, int rowCount)
    {
        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, width, height),
            RowHeight = 20,
            HeaderHeight = 20,
            ScrollbarThickness = 12
        };

        table.Columns.Add(new UiTableColumn("Name", weight: 2f));
        table.Columns.Add(new UiTableColumn("Type", weight: 1f));
        table.Columns.Add(new UiTableColumn("Size", weight: 1f));

        UiTableRow[] rows = new UiTableRow[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            rows[i] = new UiTableRow($"Asset {i:D2}", i % 2 == 0 ? "Scene" : "Texture", $"{64 + i} KB");
        }

        table.Rows = rows;
        return table;
    }

    private static void Update(UiElement element, UiInputState input)
    {
        element.Update(new UiUpdateContext(
            input,
            new UiFocusManager(),
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));
    }
}
