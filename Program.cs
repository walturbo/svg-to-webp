using SkiaSharp;
using Svg.Skia;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return;
}

var options = ParseArgs(args);

if (!File.Exists(options.InputPath))
{
    Console.Error.WriteLine($"Errore: file SVG non trovato: {options.InputPath}");
    Environment.Exit(1);
}

try
{
    ConvertSvgToWebp(options.InputPath, options.Output1Path, options.Width1, options.Height1, options.Quality1, options.Lossless1);
    ConvertSvgToWebp(options.InputPath, options.Output2Path, options.Width2, options.Height2, options.Quality2, options.Lossless2);

    Console.WriteLine("Conversione completata con successo.");
    Console.WriteLine($"File creati:\n- {options.Output1Path}\n- {options.Output2Path}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Errore durante la conversione: {ex.Message}");
    Environment.Exit(1);
}

static void ConvertSvgToWebp(string inputSvg, string outputWebp, int width, int height, int quality, bool lossless)
{
    if (width <= 0 || height <= 0)
    {
        throw new ArgumentException("Larghezza e altezza devono essere maggiori di 0.");
    }

    if (quality < 0 || quality > 100)
    {
        throw new ArgumentException("La qualità deve essere compresa tra 0 e 100.");
    }

    using var stream = File.OpenRead(inputSvg);
    var svg = new SKSvg();
    var picture = svg.Load(stream) ?? throw new InvalidOperationException("Impossibile leggere il file SVG.");

    var originalRect = picture.CullRect;
    if (originalRect.Width <= 0 || originalRect.Height <= 0)
    {
        throw new InvalidOperationException("SVG con dimensioni non valide.");
    }

    using var colorSpace = SKColorSpace.CreateSrgb();
    var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul, colorSpace);

    using var surface = SKSurface.Create(imageInfo) ?? throw new InvalidOperationException("Impossibile creare la superficie di rendering.");
    var canvas = surface.Canvas;
    canvas.Clear(SKColors.Transparent);

    var scaleX = width / originalRect.Width;
    var scaleY = height / originalRect.Height;
    var scale = Math.Min(scaleX, scaleY);

    var targetWidth = originalRect.Width * scale;
    var targetHeight = originalRect.Height * scale;

    var offsetX = (width - targetWidth) / 2f;
    var offsetY = (height - targetHeight) / 2f;

    var matrix = SKMatrix.CreateScaleTranslation(scale, scale, offsetX, offsetY);
    using var paint = new SKPaint
    {
        IsAntialias = true,
        FilterQuality = SKFilterQuality.High
    };

    canvas.DrawPicture(picture, ref matrix, paint);
    canvas.Flush();

    using var image = surface.Snapshot();
    using var data = lossless
        ? image.Encode(SKEncodedImageFormat.Webp, 100)
        : image.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossy, quality));

    if (data is null)
    {
        throw new InvalidOperationException("Encoding WebP fallito.");
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputWebp))!);
    using var output = File.Open(outputWebp, FileMode.Create, FileAccess.Write);
    data.SaveTo(output);
}

static Options ParseArgs(string[] args)
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var current = args[i];
        if (!current.StartsWith("--"))
        {
            continue;
        }

        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        {
            values[current] = args[i + 1];
            i++;
            continue;
        }

        flags.Add(current);
    }

    var input = Required(values, "--input");
    var output1 = Required(values, "--output1");
    var output2 = Required(values, "--output2");

    var width1 = ParseInt(values, "--width1", 512);
    var height1 = ParseInt(values, "--height1", 512);
    var width2 = ParseInt(values, "--width2", 1024);
    var height2 = ParseInt(values, "--height2", 1024);

    var qualityGlobal = ParseNullableInt(values, "--quality");
    var quality1 = ParseNullableInt(values, "--quality1") ?? qualityGlobal ?? 80;
    var quality2 = ParseNullableInt(values, "--quality2") ?? qualityGlobal ?? 80;

    var losslessGlobal = flags.Contains("--lossless");
    var lossless1 = flags.Contains("--lossless1") || losslessGlobal;
    var lossless2 = flags.Contains("--lossless2") || losslessGlobal;

    return new Options(input, output1, output2, width1, height1, width2, height2, quality1, quality2, lossless1, lossless2);
}

static string Required(Dictionary<string, string> values, string key)
{
    if (!values.TryGetValue(key, out var val) || string.IsNullOrWhiteSpace(val))
    {
        throw new ArgumentException($"Parametro obbligatorio mancante: {key}");
    }

    return val;
}

static int ParseInt(Dictionary<string, string> values, string key, int fallback)
{
    if (!values.TryGetValue(key, out var val))
    {
        return fallback;
    }

    if (!int.TryParse(val, out var result))
    {
        throw new ArgumentException($"Valore numerico non valido per {key}: {val}");
    }

    return result;
}

static int? ParseNullableInt(Dictionary<string, string> values, string key)
{
    if (!values.TryGetValue(key, out var val))
    {
        return null;
    }

    if (!int.TryParse(val, out var result))
    {
        throw new ArgumentException($"Valore numerico non valido per {key}: {val}");
    }

    return result;
}

static void PrintHelp()
{
    Console.WriteLine("""
    Uso:
      SvgToWebpConverter --input file.svg --output1 out_small.webp --output2 out_large.webp [opzioni]

    Opzioni:
      --width1 <n>        Larghezza primo output (default: 512)
      --height1 <n>       Altezza primo output (default: 512)
      --width2 <n>        Larghezza secondo output (default: 1024)
      --height2 <n>       Altezza secondo output (default: 1024)
      --quality <0-100>   Qualità comune per entrambi i file
      --quality1 <0-100>  Qualità specifica primo file (precedenza su --quality)
      --quality2 <0-100>  Qualità specifica secondo file (precedenza su --quality)
      --lossless          Forza WebP lossless su entrambi gli output
      --lossless1         Forza WebP lossless sul primo output
      --lossless2         Forza WebP lossless sul secondo output
      --help              Mostra questo aiuto

    Esempio:
      SvgToWebpConverter --input logo.svg --output1 logo_512.webp --output2 logo_1024.webp --width1 512 --height1 512 --width2 1024 --height2 1024 --quality 85
    """);
}

internal sealed record Options(
    string InputPath,
    string Output1Path,
    string Output2Path,
    int Width1,
    int Height1,
    int Width2,
    int Height2,
    int Quality1,
    int Quality2,
    bool Lossless1,
    bool Lossless2
);
