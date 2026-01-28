using System.Numerics;
using Raylib_cs;

class Texture
{
    public readonly string name;
    public readonly bool valid;
    public readonly Texture2D texture;

    public Texture(string folder, string name)
    {
        this.name = name;
        var file = $"data/{folder}/{name}.png";
        if (File.Exists(file))
        {
            valid = true;
            texture = Raylib.LoadTexture(file);
        }
        else
        {
            valid = false;
        }
    }
}

class Textures
{
    readonly string folder;
    readonly List<Texture> textures = [];

    public void Save(ByteWriter writer)
    {
        writer.SetInt(textures.Count);
        for (var i = 0; i < textures.Count; i++)
        {
            writer.SetString(textures[i].name);
        }
    }

    public Textures(string folder, ByteReader reader)
    {
        this.folder = folder;
        var length = reader.GetInt();
        for (var i = 0; i < length; i++)
        {
            string name = reader.GetString();
            textures.Add(new Texture(folder, name));
        }
    }

    public Textures(string folder)
    {
        this.folder = folder;
    }

    public Texture GetTexture(byte id)
    {
        return textures[id];
    }

    public byte GetID(string name)
    {
        for (var i = 0; i < textures.Count; i++)
        {
            if (textures[i].name == name)
            {
                return (byte)i;
            }
        }
        textures.Add(new Texture(folder, name));
        return (byte)(textures.Count - 1);
    }
}

class Tilemap(int width, int height)
{
    public readonly int width = width;
    public readonly int height = height;
    readonly byte[,] tiles = new byte[width, height];
    const int tileSize = 100;

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
                    if (tiles[x, y] > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void Draw(Textures textures)
    {
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var id = tiles[x, y];
                if (id > 0)
                {
                    var texture = textures.GetTexture((byte)(id - 1));
                    if (texture.valid)
                    {
                        Raylib.DrawTexture(texture.texture, x * tileSize, y * tileSize, Color.White);
                    }
                    else
                    {
                        Raylib.DrawRectangle(x * tileSize, y * tileSize, tileSize, tileSize, Color.Magenta);
                    }
                }
            }
        }
    }

    public void Save(ByteWriter writer)
    {
        writer.SetInt(width);
        writer.SetInt(height);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                writer.SetByte(tiles[x, y]);
            }
        }
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

class Sprite(Vector2 position, Vector2 size, byte id)
{
    public byte id = id;
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

    public void Update(Textures textures)
    {
        if(textures.GetTexture(id).name == "Player")
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
    }

    public void Draw(Textures textures)
    {
        var tex = textures.GetTexture(id).texture;
        Raylib.DrawTexture(tex, (int)(position.X - size.X / 2), (int)(position.Y - size.Y / 2), Color.White);
    }
}

class World
{
    public readonly Textures spriteTextures;
    public readonly Textures tileTextures;
    public readonly Tilemap tilemap;
    readonly List<Sprite> sprites = [];

    public World(string file)
    {
        var byteReader = new ByteReader(File.ReadAllBytes(file));
        spriteTextures = new Textures("sprites", byteReader);
        tileTextures = new Textures("tiles", byteReader);
        tilemap = Tilemap.Load(byteReader);
        var count = byteReader.GetInt();
        for (var i = 0; i < count; i++)
        {
            Add(byteReader.GetVector2(), byteReader.GetByte());
        }
    }

    public World(int width, int height)
    {
        spriteTextures = new("sprites");
        tileTextures = new("tiles");
        tilemap = new(width, height);
    }

    public void Add(Vector2 position, byte id)
    {
        var tex = spriteTextures.GetTexture(id).texture;
        sprites.Add(new Sprite(position, new Vector2(tex.Width, tex.Height), id));
    }

    public void Save(string file)
    {
        ByteWriter writer = new();
        spriteTextures.Save(writer);
        tileTextures.Save(writer);
        tilemap.Save(writer);
        writer.SetInt(sprites.Count);
        foreach (var s in sprites)
        {
            writer.SetVector2(s.position);
            writer.SetByte(s.id);
        }
        File.WriteAllBytes(file, writer.ToBytes());
    }

    public void Update()
    {
        foreach (var s in sprites)
        {
            s.Update(spriteTextures);
        }
    }

    public void Draw()
    {
        tilemap.Draw(tileTextures);
        foreach (var s in sprites)
        {
            s.Draw(spriteTextures);
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
        var file = $"data/maps/{name}";
        world = new(file);
        current = this;
    }

    public float Update(Vector2 pos, float width)
    {
        MouseOver.SetDefault(this);
        world.Update();
        world.Draw();
        return 0;
    }
}
