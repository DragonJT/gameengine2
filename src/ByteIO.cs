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

    public string GetString()
    {
        var length = GetInt();
        char[] chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)GetByte();
        }
        return new string(chars);
    }
}

class ByteWriter
{
    readonly List<byte> bytes = [];

    public void SetByte(byte b)
    {
        bytes.Add(b);
    }

    public void SetFloat(float f)
    {
        bytes.AddRange(BitConverter.GetBytes(f));
    }

    public void SetVector2(Vector2 v)
    {
        SetFloat(v.X);
        SetFloat(v.Y);
    }

    public void SetInt(int i)
    {
        bytes.AddRange(BitConverter.GetBytes(i));
    }

    public void SetString(string s)
    {
        SetInt(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            SetByte((byte)s[i]);
        }
    }

    public byte[] ToBytes()
    {
        return [.. bytes];
    }
}
