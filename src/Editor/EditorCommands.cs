using System.Collections.Generic;

namespace IsometricMapViewer.Editor;

public sealed class PaintGroundCommand : IEditorCommand
{
    private readonly Map _map;
    private readonly int _x;
    private readonly int _y;
    private readonly short _newSprite;
    private readonly short _newFrame;
    private readonly short _oldSprite;
    private readonly short _oldFrame;

    public string Description => $"Paint Ground ({_x}, {_y}) -> {_newSprite}:{_newFrame}";

    public PaintGroundCommand(Map map, int x, int y, short newSprite, short newFrame)
    {
        _map = map;
        _x = x;
        _y = y;
        _newSprite = newSprite;
        _newFrame = newFrame;
        var tile = _map.Tiles[x, y];
        _oldSprite = tile.TileSprite;
        _oldFrame = tile.TileFrame;
    }

    public void Execute() => _map.SetGround(_x, _y, _newSprite, _newFrame);
    public void Undo() => _map.SetGround(_x, _y, _oldSprite, _oldFrame);
}

public sealed class PlaceObjectCommand : IEditorCommand
{
    private readonly Map _map;
    private readonly int _x;
    private readonly int _y;
    private readonly short _newSprite;
    private readonly short _newFrame;
    private readonly short _oldSprite;
    private readonly short _oldFrame;

    public string Description => $"Place Object ({_x}, {_y}) -> {_newSprite}:{_newFrame}";

    public PlaceObjectCommand(Map map, int x, int y, short newSprite, short newFrame)
    {
        _map = map;
        _x = x;
        _y = y;
        _newSprite = newSprite;
        _newFrame = newFrame;
        var tile = _map.Tiles[x, y];
        _oldSprite = tile.ObjectSprite;
        _oldFrame = tile.ObjectFrame;
    }

    public void Execute() => _map.SetObject(_x, _y, _newSprite, _newFrame);
    public void Undo() => _map.SetObject(_x, _y, _oldSprite, _oldFrame);
}

public sealed class ClearObjectCommand : IEditorCommand
{
    private readonly Map _map;
    private readonly int _x;
    private readonly int _y;
    private readonly short _oldSprite;
    private readonly short _oldFrame;

    public string Description => $"Clear Object ({_x}, {_y})";

    public ClearObjectCommand(Map map, int x, int y)
    {
        _map = map;
        _x = x;
        _y = y;
        var tile = _map.Tiles[x, y];
        _oldSprite = tile.ObjectSprite;
        _oldFrame = tile.ObjectFrame;
    }

    public void Execute() => _map.ClearObject(_x, _y);
    public void Undo() => _map.SetObject(_x, _y, _oldSprite, _oldFrame);
}

public sealed class PaintCollisionCommand : IEditorCommand
{
    private readonly Map _map;
    private readonly int _x;
    private readonly int _y;
    private readonly TileProperties _newProps;
    private readonly TileProperties _oldProps;

    public string Description => $"Set Collision ({_x}, {_y})";

    public PaintCollisionCommand(Map map, int x, int y, TileProperties newProps)
    {
        _map = map;
        _x = x;
        _y = y;
        _newProps = newProps;
        var tile = _map.Tiles[x, y];
        _oldProps = new TileProperties(tile.IsMoveAllowed, tile.IsTeleport, tile.IsFarmingAllowed, tile.IsWater);
    }

    public void Execute() => _map.UpdateTileProperties(_x, _y, _newProps.IsMoveAllowed, _newProps.IsTeleport, _newProps.IsFarmingAllowed, _newProps.IsWater);
    public void Undo() => _map.UpdateTileProperties(_x, _y, _oldProps.IsMoveAllowed, _oldProps.IsTeleport, _oldProps.IsFarmingAllowed, _oldProps.IsWater);
}

public sealed class BatchEditorCommand : IEditorCommand
{
    private readonly List<IEditorCommand> _commands = [];
    public string Description { get; }

    public int Count => _commands.Count;

    public BatchEditorCommand(string description)
    {
        Description = description;
    }

    public void Add(IEditorCommand command)
    {
        _commands.Add(command);
    }

    public void Execute()
    {
        for (int i = 0; i < _commands.Count; i++)
            _commands[i].Execute();
    }

    public void Undo()
    {
        for (int i = _commands.Count - 1; i >= 0; i--)
            _commands[i].Undo();
    }
}
