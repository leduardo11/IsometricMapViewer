using System;
using System.Collections.Generic;

namespace IsometricMapViewer.Editor;

public sealed class CommandHistory
{
    private const int MaxHistory = 100;
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public string LastUndoDescription => CanUndo ? _undoStack.Peek().Description : "";
    public string LastRedoDescription => CanRedo ? _redoStack.Peek().Description : "";

    public void Execute(IEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();

        // Enforce maximum history limit
        if (_undoStack.Count > MaxHistory)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = MaxHistory - 1; i >= 0; i--)
            {
                _undoStack.Push(items[i]);
            }
        }
    }

    public bool Undo()
    {
        if (!CanUndo) return false;
        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        return true;
    }

    public bool Redo()
    {
        if (!CanRedo) return false;
        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
