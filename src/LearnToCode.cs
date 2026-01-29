
using System.Numerics;

namespace LearnToCode;

interface IBlock{}


class VarBool:IBlock
{
    Label varLabel = new("var");
    Textbox name = new("x");
    Label eqLabel = new("=");
    Boolbox value = new(false);

    static void DrawHorizontal(Vector2 pos, IElement[] elements)
    {
        float x = pos.X;
        float y = pos.Y;
        foreach(var e in elements)
        {
            var width = e.GetWidth();
            e.Update(new Vector2(x+10,y), width);
            x += width+20;
        }
    }

    public void Update(Vector2 pos)
    {
        DrawHorizontal(pos, [varLabel, name, eqLabel, value]);
    }
}


class LearnToCode : IGUI
{
    VarBool varBool = new();

    public float Update(Vector2 _pos, float _width)
    {
        MouseOver.SetDefault(this);
        varBool.Update(new Vector2(100,100));
        return 0;
    }
}