// Генерирует src/app.ico (все размеры, PNG внутри ICO) и превью.
// Запуск: dotnet run --project tools/IconGen -- <путь к app.ico> [путь к preview.png]
using System.Drawing.Imaging;
using LangSwitcher;

string ico = args.Length > 0 ? args[0] : Path.Combine("..", "..", "src", "app.ico");
string? preview = args.Length > 1 ? args[1] : null;
int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

var pngs = new List<(int Size, byte[] Data)>();
foreach (int s in sizes)
{
    using var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bmp)) IconPainter.DrawApp(g, s);
    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    pngs.Add((s, ms.ToArray()));
}

using (var fs = File.Create(ico))
using (var w = new BinaryWriter(fs))
{
    w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)pngs.Count);
    int offset = 6 + 16 * pngs.Count;
    foreach (var (s, data) in pngs)
    {
        w.Write((byte)(s >= 256 ? 0 : s)); w.Write((byte)(s >= 256 ? 0 : s));
        w.Write((byte)0); w.Write((byte)0);
        w.Write((ushort)1); w.Write((ushort)32);
        w.Write(data.Length); w.Write(offset);
        offset += data.Length;
    }
    foreach (var (_, data) in pngs) w.Write(data);
}
Console.WriteLine($"ICO: {Path.GetFullPath(ico)}");

if (preview != null)
{
    // Превью: крупная 256 + все размеры в натуральную величину и увеличенные ×4 (до 64), на светлом и тёмном фоне.
    using var sheet = new Bitmap(1100, 620);
    using var g = Graphics.FromImage(sheet);
    g.Clear(Color.White);
    g.FillRectangle(new SolidBrush(Color.FromArgb(32, 32, 32)), 0, 310, 1100, 310);
    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
    for (int row = 0; row < 2; row++)
    {
        int y0 = row * 310 + 20;
        using (var big = Image.FromStream(new MemoryStream(pngs[^1].Data))) g.DrawImage(big, 20, y0, 256, 256);
        int x = 300;
        foreach (var (s, data) in pngs.Where(p => p.Size <= 64))
        {
            using var img = Image.FromStream(new MemoryStream(data));
            g.DrawImage(img, x, y0, s, s);
            int z = Math.Min(4, 256 / s);
            g.DrawImage(img, x, y0 + 70, s * z, s * z);
            x += Math.Max(s * z, 40) + 14;
        }
    }
    sheet.Save(preview, ImageFormat.Png);
    Console.WriteLine($"Preview: {Path.GetFullPath(preview)}");

    // Значки трея (EN/RU/выкл) в размерах 16/20/24/32, увеличенные ×8, с центральными линиями для проверки.
    using var tray = new Bitmap(600, 420);
    using var tg = Graphics.FromImage(tray);
    tg.Clear(Color.FromArgb(40, 40, 40));
    tg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
    tg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
    var variants = new (string T, Color C, bool X)[]
    {
        ("EN", Color.FromArgb(30, 102, 208), false), ("RU", Color.FromArgb(206, 52, 42), false), ("RU", Color.FromArgb(128, 128, 128), true),
    };
    int tx = 10;
    foreach (int s in new[] { 16, 20, 24, 32 })
    {
        int ty = 10;
        int z = 256 / s / 3 * 1 + 2;
        foreach (var v in variants)
        {
            using var b = new Bitmap(s, s, PixelFormat.Format32bppArgb);
            using (var bg = Graphics.FromImage(b)) IconPainter.DrawTray(bg, s, v.T, v.C, v.X);
            tg.DrawImage(b, tx, ty, s * z, s * z);
            using var guide = new Pen(Color.FromArgb(160, 0, 255, 0));
            tg.DrawLine(guide, tx + s * z / 2f, ty, tx + s * z / 2f, ty + s * z);
            tg.DrawLine(guide, tx, ty + s * z / 2f, tx + s * z, ty + s * z / 2f);
            ty += s * z + 6;
        }
        tx += s * z + 20;
    }
    string trayPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(preview))!, "tray.png");
    tray.Save(trayPath, ImageFormat.Png);
    Console.WriteLine($"Tray: {trayPath}");
}
