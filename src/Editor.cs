using Raylib_cs;
using System.Numerics;

class ByteReader(byte[] bytes)
{
    readonly byte[] bytes = bytes;
    int i = 0;

    public int GetInt()
    {
        var _int = BitConverter.ToInt32(bytes, i);
        i += 4;
        return _int;
    }

    public byte GetByte()
    {
        var b = bytes[i];
        i++;
        return b;
    }

    public float GetFloat()
    {
        var f = BitConverter.ToSingle(bytes, i);
        i += 4;
        return f;
    }

    public Vector2 GetVector2()
    {
        var x = GetFloat();
        var y = GetFloat();
        return new(x, y);
    }
}

class Editor : IGUI, IAwake, IForm
{
    string name;
    World world;
    public string Name => "Editor";
    public object Value => name;

    public void Awake(Scene last)
    {
        name = (string)last.GetValue("Name");
        var file = $"maps/{name}";
        if (File.Exists(file))
        {
            world = new(file);
        }
        else
        {
            world = new((int)last.GetValue("Width"), (int)last.GetValue("Height"));
        }
    }

    public void Update(Vector2 pos, float width)
    {
        var file = $"maps/{name}";
        MouseOver.SetDefault(this);
        if (MouseOver.IsMouseOver(this))
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                var (x, y) = Tilemap.GetCell(Raylib.GetMousePosition());
                world.tilemap.SetCell(x, y, 1);
                world.Save(file);
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Right))
            {
                var (x, y) = Tilemap.GetCell(Raylib.GetMousePosition());
                world.tilemap.SetCell(x, y, 0);
                world.Save(file);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.P))
            {
                world.Add(Raylib.GetMousePosition());
                world.Save(file);
            }
        }

        world.Draw();
    }
}
