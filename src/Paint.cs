using Raylib_cs;
using System.Runtime.InteropServices;
using System.Numerics;

class PaintTex
{
    public readonly int width;
    public readonly int height;
    readonly Texture2D tex;
    readonly Image img;

    public PaintTex(string file)
    {
        img = Raylib.LoadImage(file);
        tex = Raylib.LoadTextureFromImage(img);
        width = tex.Width;
        height = tex.Height;
    }

    public PaintTex(int width, int height)
    {
        this.width = width;
        this.height = height;
        img = new Image { Width = width, Height = height, Mipmaps = 1, Format = PixelFormat.UncompressedR8G8B8A8 };
        var length = width * height * 4;
        var data = Marshal.AllocHGlobal(length);
        unsafe
        {
            void* ptr = (void*)data;
            var span = new Span<byte>(ptr, length);
            span.Clear();
            img.Data = ptr;
        }
        tex = Raylib.LoadTextureFromImage(img);
    }

    public void SetPixelUnsafe(int x, int y, Color color)
    {
        unsafe
        {
            byte* b = (byte*)img.Data;
            int loc = (y * width + x) * 4;
            b[loc] = color.R;
            b[loc + 1] = color.G;
            b[loc + 2] = color.B;
            b[loc + 3] = color.A;
        }
    }

    public void SetPixel(int x, int y, Color color)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            SetPixelUnsafe(x, y, color);
        }
    }

    public Color GetPixel(int x, int y)
    {
        unsafe
        {
            byte* b = (byte*)img.Data;
            int loc = (y * width + x) * 4;
            return new Color(b[loc], b[loc + 1], b[loc + 2], b[loc + 3]);
        }
    }

    public void Refresh()
    {
        unsafe
        {
            Raylib.UpdateTexture(tex, img.Data);
        }
    }

    public void Draw()
    {
        Raylib.DrawTexture(tex, 0, 0, Color.White);
    }

    public void Draw(int tx, int ty, int w, int h)
    {
        Raylib.DrawTexturePro(tex, new Rectangle(0, 0, tx, ty), new Rectangle(0, 0, w, h), new Vector2(0, 0), 0, Color.White);
    }

    public void SaveToPng(string folder, string file)
    {
        Raylib.ExportImage(img, $"data/{folder}/{file}.png");
    }
}

class Paint : IGUI, IAwake
{
    string folder;
    string name;
    PaintTex paintTex;
    PaintTex bgTex;
    Camera2D cam;

    public void Awake(Scene last)
    {
        name = (string)last.GetValue("Name");
        if (last.name == "LoadTilePaint")
        {
            folder = "tiles";
            var file = $"data/{folder}/{name}.png";
            paintTex = new(file);
        }
        else if (last.name == "NewTilePaint")
        {
            folder = "tiles";
            paintTex = new(100, 100);
        }
        else if (last.name == "LoadPaint")
        {
            folder = "sprites";
            var file = $"data/{folder}/{name}.png";
            paintTex = new(file);
        }
        else if (last.name == "NewPaint")
        {
            folder = "sprites";
            paintTex = new((int)last.GetValue("Width"), (int)last.GetValue("Height"));
        }
        else
        {
            throw new Exception();
        }

        cam = new Camera2D
        {
            Offset = Raylib.GetScreenCenter(),
            Target = new Vector2(paintTex.width, paintTex.height) * 0.5f,
            Rotation = 0.0f,
            Zoom = 3.0f
        };
        var c1 = Scenes.style.styleColors.GetColor(StyleColor.Check1);
        var c2 = Scenes.style.styleColors.GetColor(StyleColor.Check2);
        bgTex = new(2, 2);
        bgTex.SetPixel(0, 0, c1);
        bgTex.SetPixel(1, 0, c2);
        bgTex.SetPixel(0, 1, c2);
        bgTex.SetPixel(1, 1, c1);
        bgTex.Refresh();
    }

    static void FloodFill(PaintTex paintTex, int width, int height,
                          int startX, int startY, Color newColor)
    {
        // Bounds check
        if (startX < 0 || startX >= width || startY < 0 || startY >= height)
            return;

        Color target = paintTex.GetPixel(startX, startY);

        // Nothing to do
        if (target.Equals(newColor))
            return;

        Stack<(int x, int y)> stack = new();
        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            // Bounds
            if (x < 0 || x >= width || y < 0 || y >= height)
                continue;

            // Only fill matching target color
            if (!paintTex.GetPixel(x, y).Equals(target))
                continue;

            paintTex.SetPixelUnsafe(x, y, newColor);

            // 4-way neighbors
            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }
    }

    Vector2 GetMousePosition()
    {
        return Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), cam);
    }

    public float Update(Vector2 pos, float width)
    {
        MouseOver.SetDefault(this);
        cam.Offset = Raylib.GetScreenCenter();

        var alt = Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt);

        if (alt)
        {
            cam.Zoom *= 1.0f + Raylib.GetMouseDelta().X * 0.01f;
            cam.Zoom = Math.Clamp(cam.Zoom, 0.3f, 30.0f);
        }

        if (MouseOver.IsMouseOver(this))
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Right))
            {
                cam.Target -= Raylib.GetMouseDelta() / cam.Zoom;
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                var brushRadius = (int)Scenes.current.GetValue("BrushRadius");
                var brushPos = GetMousePosition();
                var minX = (int)(brushPos.X - brushRadius);
                var minY = (int)(brushPos.Y - brushRadius);
                var maxX = (int)(brushPos.X + brushRadius + 1);
                var maxY = (int)(brushPos.Y + brushRadius + 1);
                var color = (Color)Scenes.current.GetValue("BrushColor");
                for (var x = minX; x <= maxX; x++)
                {
                    for (var y = minY; y <= maxY; y++)
                    {
                        paintTex.SetPixel(x, y, color);
                    }
                }
                paintTex.Refresh();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.F))
            {
                var brushPos = GetMousePosition();
                FloodFill(paintTex, paintTex.width, paintTex.height,
                    (int)brushPos.X, (int)brushPos.Y, (Color)Scenes.current.GetValue("BrushColor"));
                paintTex.Refresh();
            }
        }
        Raylib.BeginMode2D(cam);
        bgTex.Draw(paintTex.width / 20, paintTex.height / 20, paintTex.width, paintTex.height);
        paintTex.Draw();
        Raylib.EndMode2D();
        if ((bool)Scenes.current.GetValue("Save"))
        {
            paintTex.SaveToPng(folder, name);
        }
        return 0;
    }
}
