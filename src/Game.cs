using System.Numerics;
using Raylib_cs;

class Tilemap(int width, int height)
{
    public readonly int width = width;
    public readonly int height = height;
    readonly byte[,] tiles = new byte[width, height];
    const int tileSize = 100;
    Texture2D tex = Raylib.LoadTexture("pngs/Tile1.png");

    public static (int, int) GetCell(Vector2 position)
    {
        return ((int)(position.X / tileSize), (int)(position.Y / tileSize));
    }

    public void SetCell(int x, int y, byte value)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            tiles[x, y] = value;
        }
    }

    public bool Collides(Rectangle rect)
    {
        var min = GetCell(rect.Position);
        var max = GetCell(rect.Position + rect.Size);
        for (var x = min.Item1; x <= max.Item1; x++)
        {
            for (var y = min.Item2; y <= max.Item2; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    if (tiles[x, y] == 1)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void Draw()
    {
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (tiles[x, y] == 1)
                {
                    Raylib.DrawTexture(tex, x * tileSize, y * tileSize, Color.White);
                }
            }
        }
    }

    public byte[] Save()
    {
        List<byte> bytes = [];
        bytes.AddRange(BitConverter.GetBytes(width));
        bytes.AddRange(BitConverter.GetBytes(height));
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                bytes.Add(tiles[x, y]);
            }
        }
        return [.. bytes];
    }

    public static Tilemap Load(ByteReader byteReader)
    {
        var width = byteReader.GetInt();
        var height = byteReader.GetInt();
        var tilemap = new Tilemap(width, height);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                tilemap.tiles[x, y] = byteReader.GetByte();
            }
        }
        return tilemap;
    }
}

class Sprite(Vector2 position, Vector2 size)
{
    public Vector2 position = position;
    public Vector2 size = size;
    public float gravity = 4000f;
    public float jump = 2000f;
    public float velocity = 0;
    public float speed = 500;

    bool TryMove(float x, float y)
    {
        var pos = position + new Vector2(x, y);
        var rect = new Rectangle(pos - size / 2, size);
        rect.Grow(-20);
        if (Game.current.world.tilemap.Collides(rect))
        {
            velocity = 0;
            return false;
        }
        else
        {
            position = pos;
            return true;
        }
    }

    public void Update()
    {
        var dt = Raylib.GetFrameTime();
        if (Raylib.IsKeyDown(KeyboardKey.Left))
        {
            TryMove(-speed * dt, 0);
        }
        if (Raylib.IsKeyDown(KeyboardKey.Right))
        {
            TryMove(speed * dt, 0);
        }
        velocity += gravity * dt;
        if (!TryMove(0, velocity * dt) && Raylib.IsKeyDown(KeyboardKey.Up))
        {
            velocity -= jump;
        }
    }

    public void Draw(Texture2D tex)
    {
        Raylib.DrawTexture(tex, (int)(position.X - size.X / 2), (int)(position.Y - size.Y / 2), Color.White);
    }
}

class World
{
    public readonly Tilemap tilemap;
    readonly List<Sprite> sprites = [];
    public readonly Texture2D tex = Raylib.LoadTexture("pngs/Player.png");

    public World(string file)
    {
        var byteReader = new ByteReader(File.ReadAllBytes(file));
        tilemap = Tilemap.Load(byteReader);
        var count = byteReader.GetInt();
        for (var i = 0; i < count; i++)
        {
            Add(byteReader.GetVector2());
        }
    }

    public World(int width, int height)
    {
        tilemap = new(width, height);
    }

    public void Add(Vector2 position)
    {
        sprites.Add(new Sprite(position, new Vector2(tex.Width, tex.Height)));
    }

    public void Save(string file)
    {
        List<byte> bytes = [];
        bytes.AddRange(tilemap.Save());
        bytes.AddRange(BitConverter.GetBytes(sprites.Count));
        foreach (var s in sprites)
        {
            bytes.AddRange(BitConverter.GetBytes(s.position.X));
            bytes.AddRange(BitConverter.GetBytes(s.position.Y));
        }
        File.WriteAllBytes(file, [.. bytes]);
    }

    public void Update()
    {
        foreach (var s in sprites)
        {
            s.Update();
        }
    }

    public void Draw()
    {
        tilemap.Draw();
        foreach (var s in sprites)
        {
            s.Draw(tex);
        }
    }
}

class Game : IGUI, IAwake, IForm
{
    public static Game current;
    public World world;
    public string Name => "Name";
    public object Value { get; private set; }

    public void Awake(Scene last)
    {
        var name = (string)last.GetValue("Editor");
        Value = name;
        var file = $"maps/{name}";
        world = new(file);
        current = this;
    }

    public void Update(Vector2 pos, float width)
    {
        MouseOver.SetDefault(this);
        world.Update();
        world.Draw();
    }
}
