using Raylib_cs;
using System.Numerics;

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

    public float Update(Vector2 pos, float width)
    {
        var file = $"maps/{name}";
        MouseOver.SetDefault(this);
        if (MouseOver.IsMouseOver(this))
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                var id = world.textures.GetID((string)Scenes.current.GetValue("Png"));
                var (x, y) = Tilemap.GetCell(Raylib.GetMousePosition());
                world.tilemap.SetCell(x, y, id);
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
        return 0;
    }
}
