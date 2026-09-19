namespace IsometricMapViewer.Editor;

public interface IEditorCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}
