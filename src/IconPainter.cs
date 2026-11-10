using System.Drawing;
using System.Drawing.Drawing2D;

namespace LangSwitcher;

/// <summary>Рисование иконок: значок в трее и иконка приложения (её же использует tools/IconGen).</summary>
internal static class IconPainter
{
    /// <summary>Контур текста, отцентрированный по фактическим границам глифов
    /// (а не по строке шрифта с её межстрочным интервалом) и вписанный в maxW × maxH.</summary>
    public static GraphicsPath CenteredText(string text, string font, FontStyle style, PointF center, float maxW, float maxH)
    {
        var path = new GraphicsPath();
        using (var family = new FontFamily(font))
            path.AddString(text, family, (int)style, 100f, PointF.Empty, StringFormat.GenericTypographic);
        RectangleF b = path.GetBounds();
        float k = Math.Min(maxW / b.Width, maxH / b.Height);
        using var m = new Matrix();
        m.Translate(-(b.X + b.Width / 2), -(b.Y + b.Height / 2), MatrixOrder.Append);
        m.Scale(k, k, MatrixOrder.Append);
        m.Translate(center.X, center.Y, MatrixOrder.Append);
        path.Transform(m);
        return path;
    }

    public static GraphicsPath RoundRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    private static Icon? _appIcon;

    /// <summary>Иконка приложения, встроенная в exe (для окон программы).</summary>
    public static Icon? AppIcon
    {
        get
        {
            if (_appIcon == null && Environment.ProcessPath is { } exe)
                try { _appIcon = Icon.ExtractAssociatedIcon(exe); } catch { }
            return _appIcon;
        }
    }

    // ---------------- значок в трее ----------------

    public static void DrawTray(Graphics g, int s, string text, Color bg, bool crossed)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        using (var p = RoundRect(new RectangleF(0, 0, s, s), s / 4f))
        using (var brush = new SolidBrush(bg))
            g.FillPath(brush, p);

        using (var t = CenteredText(text, "Segoe UI", FontStyle.Bold, new PointF(s / 2f, s / 2f), s * 0.78f, s * 0.46f))
            g.FillPath(Brushes.White, t);

        if (crossed)
        {
            using var pen = new Pen(Color.FromArgb(235, 255, 255, 255), Math.Max(1.5f, s / 12f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, s * 0.14f, s * 0.86f, s * 0.86f, s * 0.14f);
        }
    }

    // ---------------- иконка приложения ----------------

    private static readonly Color Blue = Color.FromArgb(37, 99, 235);
    private static readonly Color BlueDark = Color.FromArgb(29, 58, 160);
    private static readonly Color Red = Color.FromArgb(220, 38, 38);
    private static readonly Color RedDark = Color.FromArgb(150, 24, 40);

    /// <summary>Иконка: сине-красный фон, две клавиши «A» и «Я» и стрелки переключения.
    /// Рисуется в координатах 256×256 и масштабируется. На мелких размерах — упрощённый вариант.</summary>
    public static void DrawApp(Graphics g, int size)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Color.Transparent);
        g.ScaleTransform(size / 256f, size / 256f);

        bool small = size <= 48;

        // Фон: диагональный переход синий → красный.
        var bgRect = small ? new RectangleF(0, 0, 256, 256) : new RectangleF(8, 8, 240, 240);
        using (var bgPath = RoundRect(bgRect, small ? 56 : 54))
        {
            using (var grad = new LinearGradientBrush(new PointF(0, 0), new PointF(256, 256), Blue, Red))
            {
                grad.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Blue, Blue, Color.FromArgb(124, 58, 237), Red, Red },
                    Positions = new[] { 0f, 0.3f, 0.5f, 0.7f, 1f },
                };
                g.FillPath(grad, bgPath);
            }
            if (!small)
            {
                // лёгкий блик сверху
                using var gloss = new LinearGradientBrush(new PointF(0, 6), new PointF(0, 250),
                    Color.FromArgb(70, 255, 255, 255), Color.FromArgb(0, 255, 255, 255));
                gloss.Blend = new Blend { Factors = new[] { 0f, 1f, 1f }, Positions = new[] { 0f, 0.55f, 1f } };
                g.FillPath(gloss, bgPath);
            }
        }

        if (small)
        {
            // Мелкие размеры: две крупные клавиши без стрелок.
            Key(g, new RectangleF(18, 18, 132, 132), 26, "A", BlueDark, 10);
            Key(g, new RectangleF(106, 106, 132, 132), 26, "Я", RedDark, 10);
            return;
        }

        // Стрелки переключения (под клавишами).
        using (var pen = new Pen(Color.White, 15) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawArc(pen, 112, 44, 104, 104, 270, 75);   // сверху справа: от «A» к «Я»
            g.DrawArc(pen, 40, 108, 104, 104, 90, 75);    // снизу слева: от «Я» к «A»
        }
        Arrowhead(g, Point(164, 96, 52, 345), 345 + 90);
        Arrowhead(g, Point(92, 160, 52, 165), 165 + 90);

        Key(g, new RectangleF(36, 36, 112, 112), 24, "A", BlueDark, 9);
        Key(g, new RectangleF(108, 108, 112, 112), 24, "Я", RedDark, 9);
    }

    private static PointF Point(float cx, float cy, float r, float deg)
    {
        double a = deg * Math.PI / 180;
        return new PointF(cx + r * (float)Math.Cos(a), cy + r * (float)Math.Sin(a));
    }

    private static void Arrowhead(Graphics g, PointF tip, float dirDeg)
    {
        // треугольник, указывающий в направлении dirDeg
        PointF a = Point(tip.X, tip.Y, 20, dirDeg);
        PointF b = Point(tip.X, tip.Y, 17, dirDeg + 130);
        PointF c = Point(tip.X, tip.Y, 17, dirDeg - 130);
        using var p = new GraphicsPath();
        p.AddPolygon(new[] { a, b, c });
        g.FillPath(Brushes.White, p);
    }

    private static void Key(Graphics g, RectangleF r, float radius, string letter, Color ink, float depth)
    {
        // тень
        using (var shadow = RoundRect(new RectangleF(r.X + 2, r.Y + depth + 4, r.Width, r.Height), radius))
        using (var sb = new SolidBrush(Color.FromArgb(70, 0, 0, 20)))
            g.FillPath(sb, shadow);
        // боковина клавиши
        using (var side = RoundRect(new RectangleF(r.X, r.Y + depth, r.Width, r.Height), radius))
        using (var sideBrush = new SolidBrush(Color.FromArgb(203, 213, 225)))
            g.FillPath(sideBrush, side);
        // верхняя грань
        using (var top = RoundRect(r, radius))
        using (var topBrush = new LinearGradientBrush(r, Color.White, Color.FromArgb(241, 245, 249), LinearGradientMode.Vertical))
            g.FillPath(topBrush, top);
        // буква
        using var t = CenteredText(letter, "Segoe UI", FontStyle.Bold,
            new PointF(r.X + r.Width / 2, r.Y + r.Height / 2), r.Width * 0.6f, r.Height * 0.58f);
        using var ib = new SolidBrush(ink);
        g.FillPath(ib, t);
    }
}
