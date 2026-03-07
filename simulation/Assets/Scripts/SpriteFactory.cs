using UnityEngine;

/// <summary>
/// 运行时创建精灵 — 不需要任何外部资源文件
/// </summary>
public static class SpriteFactory
{
    private static Sprite _filingSprite;
    private static Sprite _magnetSprite;
    private static Sprite _circleSprite;

    /// <summary>
    /// 铁屑精灵：细长椭圆形（模拟真实铁屑）
    /// </summary>
    public static Sprite FilingSprite
    {
        get
        {
            if (_filingSprite == null)
                _filingSprite = CreateEllipseSprite(48, 16, Color.white);
            return _filingSprite;
        }
    }

    /// <summary>
    /// 磁铁精灵：圆形
    /// </summary>
    public static Sprite MagnetSprite
    {
        get
        {
            if (_magnetSprite == null)
                _magnetSprite = CreateCircleSprite(32, Color.white);
            return _magnetSprite;
        }
    }

    /// <summary>
    /// 通用圆形精灵（用于阈值线等）
    /// </summary>
    public static Sprite CircleSprite
    {
        get
        {
            if (_circleSprite == null)
                _circleSprite = CreateCircleSprite(16, Color.white);
            return _circleSprite;
        }
    }

    static Sprite CreateCircleSprite(int radius, Color color)
    {
        int size = radius * 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(0, 0, 0, 0);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < radius - 1)
                    tex.SetPixel(x, y, color);
                else if (dist < radius)
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, (radius - dist)));
                else
                    tex.SetPixel(x, y, clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    static Sprite CreateEllipseSprite(int width, int height, Color color)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float hw = width * 0.5f;
        float hh = height * 0.5f;
        Color clear = new Color(0, 0, 0, 0);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float dx = (x - hw + 0.5f) / hw;
                float dy = (y - hh + 0.5f) / hh;
                float dist = dx * dx + dy * dy;
                if (dist < 0.7f)
                    tex.SetPixel(x, y, color);
                else if (dist < 1f)
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, 1f - (dist - 0.7f) / 0.3f));
                else
                    tex.SetPixel(x, y, clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), Vector2.one * 0.5f, 100f);
    }

    /// <summary>
    /// 创建一个铁屑 GameObject
    /// </summary>
    public static GameObject CreateFiling(Vector2 position, Color color)
    {
        var go = new GameObject("Filing");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.35f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FilingSprite;
        sr.color = color;
        sr.sortingOrder = 1;

        var filing = go.AddComponent<IronFiling>();
        filing.baseColor = color;

        return go;
    }

    /// <summary>
    /// 创建一个磁铁 GameObject
    /// </summary>
    public static GameObject CreateMagnet(Vector2 position, float S, Color color, bool draggable = true)
    {
        var go = new GameObject("Magnet");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MagnetSprite;
        sr.color = color;
        sr.sortingOrder = 5;

        var magnet = go.AddComponent<Magnet>();
        magnet.S = S;
        magnet.magnetColor = color;
        magnet.isDraggable = draggable;

        return go;
    }
}
