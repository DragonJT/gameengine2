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
        if(last.name == "NewEditor")
        {
            name = (string)last.GetValue("Name");
            world = new((int)last.GetValue("Width"), (int)last.GetValue("Height"));
        }
        else if(last.name == "LoadEditor" || last.name == "Game")
        {
            name = (string)last.GetValue("Name");
            var file = $"data/maps/{name}";   
            world = new(file);
        }
        else
        {
            throw new Exception();
        }
    }

    public float Update(Vector2 pos, float width)
    {
        var file = $"data/maps/{name}";
        MouseOver.SetDefault(this);
        if (MouseOver.IsMouseOver(this))
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                var id = world.tileTextures.GetID((string)Scenes.current.GetValue("Tiles"));
                var (x, y) = Tilemap.GetCell(Raylib.GetMousePosition());
                world.tilemap.SetCell(x, y, (byte)(id + 1));
                world.Save(file);
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Right))
            {
                var (x, y) = Tilemap.GetCell(Raylib.GetMousePosition());
                world.tilemap.SetCell(x, y, 0);
                world.Save(file);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                var id = world.spriteTextures.GetID((string)Scenes.current.GetValue("Sprites"));
                world.Add(Raylib.GetMousePosition(), id);
                world.Save(file);
            }
        }

        world.Draw();
        return 0;
    }
}
