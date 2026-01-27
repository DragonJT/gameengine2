using Raylib_cs;
using System.Reflection;
using System.Numerics;

enum StyleColor { Background, TextDark, TextLight, SelectedBorder, DeSelectedBorder, Check1, Check2 }

class StyleColors
{
    readonly Dictionary<StyleColor, Color> colors = [];

    static float Parse(string[] parts, int id, float defaultValue)
    {
        if (id < parts.Length)
        {
            if (float.TryParse(parts[id], out float value))
            {
                return value;
            }
        }
        return defaultValue;
    }

    public StyleColors(string file)
    {
        var lines = File.ReadAllLines(file);
        foreach (var line in lines)
        {
            var parts = line.Split(' ');
            if (parts.Length > 0)
            {
                if (Enum.TryParse(typeof(StyleColor), parts[0], false, out object styleColors))
                {
                    var r = Parse(parts, 1, 0);
                    var g = Parse(parts, 2, 0);
                    var b = Parse(parts, 3, 0);
                    var a = Parse(parts, 4, 1);
                    colors.Add((StyleColor)styleColors, new Color(r, g, b, a));
                }
            }
        }
    }

    public Color GetColor(StyleColor layoutColor)
    {
        if (colors.TryGetValue(layoutColor, out Color color))
        {
            return color;
        }
        return Color.Black;
    }
}

class Style(string file)
{
    public const int fontSize = 40;
    public const int lineSize = 60;
    public readonly StyleColors styleColors = new(file);

    public void Clear()
    {
        Raylib.ClearBackground(styleColors.GetColor(StyleColor.Background));
    }

    public void DrawText(Vector2 pos, string text, StyleColor layoutColor)
    {
        Raylib.DrawText(text, (int)pos.X, (int)pos.Y, fontSize, styleColors.GetColor(layoutColor));
    }

    public Rectangle RectBorder(Vector2 pos, float width, StyleColor layoutColor)
    {
        var rect = new Rectangle(pos.X, pos.Y, width, fontSize);
        Raylib.DrawRectangleLinesEx(rect, 2, styleColors.GetColor(layoutColor));
        return rect;
    }
}

static class MouseOver
{
    static IGUI last;
    static IGUI current;

    public static void EndFrame()
    {
        last = current;
        current = null;
    }

    public static void SetDefault(IGUI gui)
    {
        current = gui;
    }

    public static void SetRect(IGUI gui, Rectangle rect)
    {
        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), rect))
        {
            current = gui;
        }
    }

    public static bool IsMouseOver(IGUI gui)
    {
        return last == gui;
    }
}

interface IGUI
{
    void Update(Vector2 pos, float width);
}

interface IForm
{
    string Name { get; }
    object Value { get; }
}

interface IAwake
{
    void Awake(Scene last);
}

class Label(string text) : IGUI
{
    readonly string text = text;

    public void Update(Vector2 pos, float width)
    {
        Scenes.style.DrawText(pos, text, StyleColor.TextDark);
    }
}

class Textbox(string value) : IGUI
{
    public string value = value;
    bool selected = false;

    void UpdateText()
    {
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (key >= 32 && key <= 125)
                value += (char)key;

            key = Raylib.GetCharPressed();
        }
        if (value.Length > 0 && (Raylib.IsKeyPressed(KeyboardKey.Backspace) || Raylib.IsKeyPressedRepeat(KeyboardKey.Backspace)))
        {
            value = value[..^1];
        }
    }

    public void Update(Vector2 pos, float width)
    {
        var mouseOver = MouseOver.IsMouseOver(this);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            selected = mouseOver;
        }
        if (selected)
        {
            UpdateText();
        }
        var rect = Scenes.style.RectBorder(pos, width, selected ? StyleColor.SelectedBorder : StyleColor.DeSelectedBorder);
        MouseOver.SetRect(this, rect);
        Scenes.style.DrawText(pos, value, StyleColor.TextLight);
    }
}

class Colorbox : IGUI
{
    readonly Textbox rbox = new("0");
    readonly Textbox gbox = new("0");
    readonly Textbox bbox = new("0");
    readonly Textbox abox = new("1");

    public Color GetColor()
    {
        return new Color(
            float.Parse(rbox.value),
            float.Parse(gbox.value),
            float.Parse(bbox.value),
            float.Parse(abox.value));
    }

    public void Update(Vector2 pos, float width)
    {
        var w = width / 4f;
        rbox.Update(pos, w);
        gbox.Update(new Vector2(pos.X + w, pos.Y), w);
        bbox.Update(new Vector2(pos.X + w * 2, pos.Y), w);
        abox.Update(new Vector2(pos.X + w * 3, pos.Y), w);
    }
}

class LabeledColorbox(string name) : IGUI, IForm
{
    public string Name { get; } = name;
    readonly Colorbox colorbox = new();
    public object Value => colorbox.GetColor();

    public void Update(Vector2 pos, float width)
    {
        var w = width * 0.3f;
        Scenes.style.DrawText(pos, Name, StyleColor.TextDark);
        colorbox.Update(new Vector2(pos.X + w, pos.Y), width * 0.7f);
    }
}

class LabeledIntbox(string name, int value) : IGUI, IForm
{
    public string Name { get; } = name;
    readonly Textbox textbox = new(value.ToString());
    public object Value => int.Parse(textbox.value);

    public void Update(Vector2 pos, float width)
    {
        var w = width * 0.3f;
        Scenes.style.DrawText(pos, Name, StyleColor.TextDark);
        textbox.Update(new Vector2(pos.X + w, pos.Y), width * 0.7f);
    }
}

class LabeledTextbox(string name, string value) : IGUI, IForm
{
    public string Name { get; } = name;
    readonly Textbox textbox = new(value);
    public object Value => textbox.value;

    public void Update(Vector2 pos, float width)
    {
        var w = width * 0.3f;
        Scenes.style.DrawText(pos, Name, StyleColor.TextDark);
        textbox.Update(new Vector2(pos.X + w, pos.Y), width * 0.7f);
    }
}

class Button(string name) : IGUI, IForm
{
    public string Name { get; } = name;
    public object Value { get; private set; } = false;

    public virtual void OnClick() { }

    public void Update(Vector2 pos, float width)
    {
        var mouseOver = MouseOver.IsMouseOver(this);
        var onclick = mouseOver && Raylib.IsMouseButtonPressed(MouseButton.Left);
        Value = onclick;
        if (onclick)
        {
            OnClick();
        }
        var rect = Scenes.style.RectBorder(pos, width, mouseOver ? StyleColor.SelectedBorder : StyleColor.DeSelectedBorder);
        MouseOver.SetRect(this, rect);
        Scenes.style.DrawText(pos, Name, StyleColor.TextDark);
    }
}

class SceneButton(string name, string scene) : Button(name)
{
    readonly string scene = scene;

    public override void OnClick()
    {
        Scenes.ChangeScene(scene);
    }
}

class Scene
{
    public readonly string name;
    readonly IGUI[] guis;

    static object[] GetArgs(string[] paramValues, ParameterInfo[] parameters)
    {
        List<object> args = [];
        for (var i = 0; i < paramValues.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(string))
            {
                args.Add(paramValues[i]);
            }
            else if (parameters[i].ParameterType == typeof(int))
            {
                args.Add(int.Parse(paramValues[i]));
            }
            else
            {
                return null;
            }
        }
        return [.. args];
    }

    static IGUI CreateGUI(Type type, string[] paramValues)
    {
        var ctors = type.GetConstructors();
        foreach (var ctor in ctors)
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length == paramValues.Length)
            {
                var args = GetArgs(paramValues, parameters);
                if (args != null)
                {
                    return (IGUI)Activator.CreateInstance(type, [.. args])!;
                }
            }
        }
        throw new Exception();
    }

    public Scene(string name, string path)
    {
        this.name = name;
        var guiTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                typeof(IGUI).IsAssignableFrom(t) &&
                t.IsClass &&
                !t.IsAbstract
            )
            .ToList();

        var guis = new List<IGUI>();
        var lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            var parts = line.Split(' ');
            if (parts.Length > 0)
            {
                var type = guiTypes.FirstOrDefault(t => t.Name == parts[0]);
                if (type != null)
                {
                    guis.Add(CreateGUI(type, parts[1..]));
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        this.guis = [.. guis];
    }

    public object GetValue(string name)
    {
        return guis.OfType<IForm>().First(f => f.Name == name).Value;
    }

    public void Awake(Scene last)
    {
        foreach (var g in guis.OfType<IAwake>())
        {
            g.Awake(last);
        }
    }

    public void Update()
    {
        float y = 20;
        foreach (var gui in guis.ToArray())
        {
            gui.Update(new Vector2(20, y), 800);
            y += Style.lineSize;
        }
    }
}

static class Scenes
{
    static readonly List<Scene> scenes = [];
    public static Scene current;
    public static Style style;

    public static void Awake(string styleFile)
    {
        string[] files = Directory.GetFiles("scenes");
        foreach (var f in files)
        {
            var name = Path.GetFileNameWithoutExtension(f);
            scenes.Add(new Scene(name, f));
        }
        style = new Style(styleFile);
        current = scenes.First(s => s.name == "Main");
    }

    public static void Update()
    {
        style.Clear();
        current.Update();
    }

    public static void ChangeScene(string name)
    {
        var last = current;
        current = scenes.First(s => s.name == name);
        current.Awake(last);
    }
}

class Program
{
    static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1000, 800, "GameEngine2");
        Raylib.MaximizeWindow();
        Scenes.Awake("Style.txt");

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Scenes.Update();
            Raylib.EndDrawing();
            MouseOver.EndFrame();
        }
        Raylib.CloseWindow();
    }
}
