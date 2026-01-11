using System.IO;
using System.Linq;
using System.Text.Json;
using OpenControls;
using OpenControls.Controls;

namespace OpenControls.State;

public static class UiStateSerializer
{
    public static UiStateSnapshot Capture(UiElement root)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        UiStateSnapshot snapshot = new();
        CaptureElement(root, snapshot);
        return snapshot;
    }

    public static void Apply(UiElement root, UiStateSnapshot snapshot)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        Dictionary<string, UiElement> elementsById = new(StringComparer.Ordinal);
        Dictionary<string, UiDockWorkspace> workspacesById = new(StringComparer.Ordinal);
        Dictionary<string, UiWindow> windowsById = new(StringComparer.Ordinal);
        CollectElements(root, elementsById, workspacesById, windowsById);

        foreach (UiElementState state in snapshot.Elements)
        {
            if (string.IsNullOrWhiteSpace(state.Id))
            {
                continue;
            }

            if (!elementsById.TryGetValue(state.Id, out UiElement? element))
            {
                continue;
            }

            element.Bounds = state.Bounds;
            element.Visible = state.Visible;
            element.Enabled = state.Enabled;

            if (element is UiTextField field && state.Text != null)
            {
                field.Text = state.Text;
                if (state.CaretIndex.HasValue)
                {
                    field.SetCaretIndex(state.CaretIndex.Value);
                }
            }
        }

        foreach (UiDockWorkspaceState workspaceState in snapshot.DockWorkspaces)
        {
            UiDockWorkspace? workspace = null;
            if (!string.IsNullOrWhiteSpace(workspaceState.Id))
            {
                workspacesById.TryGetValue(workspaceState.Id, out workspace);
            }
            else if (workspacesById.Count == 1)
            {
                workspace = workspacesById.Values.First();
            }

            workspace?.ApplyState(workspaceState, windowsById);
        }
    }

    public static string ToJson(UiStateSnapshot snapshot, bool indented = true)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(snapshot, options);
    }

    public static UiStateSnapshot FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON content was empty.", nameof(json));
        }

        UiStateSnapshot? snapshot = JsonSerializer.Deserialize<UiStateSnapshot>(json);
        return snapshot ?? new UiStateSnapshot();
    }

    public static void SaveToFile(string path, UiStateSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path was empty.", nameof(path));
        }

        string json = ToJson(snapshot);
        File.WriteAllText(path, json);
    }

    public static UiStateSnapshot LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path was empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            return new UiStateSnapshot();
        }

        string json = File.ReadAllText(path);
        return FromJson(json);
    }

    private static void CaptureElement(UiElement element, UiStateSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(element.Id))
        {
            UiElementState state = new()
            {
                Id = element.Id,
                Type = element.GetType().Name,
                Bounds = element.Bounds,
                Visible = element.Visible,
                Enabled = element.Enabled
            };

            if (element is UiTextField field)
            {
                state.Text = field.Text;
                state.CaretIndex = field.CaretIndex;
            }

            snapshot.Elements.Add(state);
        }

        if (element is UiDockWorkspace workspace)
        {
            snapshot.DockWorkspaces.Add(workspace.CaptureState());
        }

        foreach (UiElement child in element.Children)
        {
            CaptureElement(child, snapshot);
        }
    }

    private static void CollectElements(
        UiElement element,
        Dictionary<string, UiElement> elementsById,
        Dictionary<string, UiDockWorkspace> workspacesById,
        Dictionary<string, UiWindow> windowsById)
    {
        if (!string.IsNullOrWhiteSpace(element.Id))
        {
            elementsById[element.Id] = element;
        }

        if (element is UiDockWorkspace workspace && !string.IsNullOrWhiteSpace(element.Id))
        {
            workspacesById[element.Id] = workspace;
        }

        if (element is UiWindow window && !string.IsNullOrWhiteSpace(element.Id))
        {
            windowsById[element.Id] = window;
        }

        foreach (UiElement child in element.Children)
        {
            CollectElements(child, elementsById, workspacesById, windowsById);
        }
    }
}
