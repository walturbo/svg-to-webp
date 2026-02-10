using System.Globalization;
using SkiaSharp;
using Svg.Skia;
using System.Windows.Forms;

if (args.Length > 0)
{
    try
    {
        var options = CliParser.ParseArgs(args);
        SvgWebpConverter.ConvertSvgToWebp(options.InputPath, options.Output1Path, options.Width1, options.Height1, options.Quality1, options.Lossless1);
        SvgWebpConverter.ConvertSvgToWebp(options.InputPath, options.Output2Path, options.Width2, options.Height2, options.Quality2, options.Lossless2);

        Console.WriteLine("Conversione completata con successo.");
        Console.WriteLine($"File creati:\n- {options.Output1Path}\n- {options.Output2Path}");
    }
    catch (HelpRequestedException)
    {
        CliParser.PrintHelp();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Errore durante la conversione: {ex.Message}");
        Environment.Exit(1);
    }

    return;
}

ApplicationConfiguration.Initialize();
Application.Run(new MainForm());

internal static class SvgWebpConverter
{
    public static void ConvertSvgToWebp(string inputSvg, string outputWebp, int width, int height, int quality, bool lossless)
    {
        if (!File.Exists(inputSvg))
        {
            throw new FileNotFoundException($"File SVG non trovato: {inputSvg}");
        }

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
        canvas.DrawPicture(picture, in matrix);
        canvas.Flush();

        using var image = surface.Snapshot();
        var effectiveQuality = lossless ? 100 : quality;
        using var data = image.Encode(SKEncodedImageFormat.Webp, effectiveQuality);

        if (data is null)
        {
            throw new InvalidOperationException("Encoding WebP fallito.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputWebp))!);
        using var output = File.Open(outputWebp, FileMode.Create, FileAccess.Write);
        data.SaveTo(output);
    }
}

internal static class CliParser
{
    public static Options ParseArgs(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            throw new HelpRequestedException();
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
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

    public static void PrintHelp()
    {
        Console.WriteLine("""
        Uso CLI:
          SvgToWebpConverter --input file.svg --output1 out_small.webp --output2 out_large.webp [opzioni]

        Opzioni:
          --width1 <n>        Larghezza primo output (default: 512)
          --height1 <n>       Altezza primo output (default: 512)
          --width2 <n>        Larghezza secondo output (default: 1024)
          --height2 <n>       Altezza secondo output (default: 1024)
          --quality <0-100>   Qualità comune per entrambi i file
          --quality1 <0-100>  Qualità specifica primo file (precedenza su --quality)
          --quality2 <0-100>  Qualità specifica secondo file (precedenza su --quality)
          --lossless          Forza qualità WebP 100 su entrambi gli output
          --lossless1         Forza qualità WebP 100 sul primo output
          --lossless2         Forza qualità WebP 100 sul secondo output
          --help              Mostra questo aiuto

        Esempio:
          SvgToWebpConverter --input logo.svg --output1 logo_512.webp --output2 logo_1024.webp --width1 512 --height1 512 --width2 1024 --height2 1024 --quality 85
        """);
    }

    private static string Required(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var val) || string.IsNullOrWhiteSpace(val))
        {
            throw new ArgumentException($"Parametro obbligatorio mancante: {key}");
        }

        return val;
    }

    private static int ParseInt(Dictionary<string, string> values, string key, int fallback)
    {
        if (!values.TryGetValue(key, out var val))
        {
            return fallback;
        }

        if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"Valore numerico non valido per {key}: {val}");
        }

        return result;
    }

    private static int? ParseNullableInt(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var val))
        {
            return null;
        }

        if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"Valore numerico non valido per {key}: {val}");
        }

        return result;
    }
}

internal sealed class HelpRequestedException : Exception;

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

internal sealed class MainForm : Form
{
    private readonly TextBox _inputTextBox = new() { Width = 460 };
    private readonly TextBox _output1TextBox = new() { Width = 460 };
    private readonly TextBox _output2TextBox = new() { Width = 460 };

    private readonly NumericUpDown _width1Numeric = CreateNumeric(512, 1, 20000);
    private readonly NumericUpDown _height1Numeric = CreateNumeric(512, 1, 20000);
    private readonly NumericUpDown _quality1Numeric = CreateNumeric(80, 0, 100);

    private readonly NumericUpDown _width2Numeric = CreateNumeric(1024, 1, 20000);
    private readonly NumericUpDown _height2Numeric = CreateNumeric(1024, 1, 20000);
    private readonly NumericUpDown _quality2Numeric = CreateNumeric(80, 0, 100);

    private readonly CheckBox _lossless1Check = new() { Text = "Lossless output 1" };
    private readonly CheckBox _lossless2Check = new() { Text = "Lossless output 2" };

    private readonly Button _convertButton = new() { Text = "Converti", Width = 120, Height = 34 };
    private readonly Label _statusLabel = new() { AutoSize = true, Text = "Pronto" };

    public MainForm()
    {
        Text = "SVG → WebP Converter";
        Width = 840;
        Height = 520;
        StartPosition = FormStartPosition.CenterScreen;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 6,
            AutoSize = true,
        };

        mainLayout.Controls.Add(CreatePathRow("SVG input", _inputTextBox, BrowseInput));
        mainLayout.Controls.Add(CreatePathRow("Output WebP 1", _output1TextBox, () => BrowseOutput(_output1TextBox)));
        mainLayout.Controls.Add(CreatePathRow("Output WebP 2", _output2TextBox, () => BrowseOutput(_output2TextBox)));
        mainLayout.Controls.Add(CreateSettingsGroup("Impostazioni Output 1", _width1Numeric, _height1Numeric, _quality1Numeric, _lossless1Check));
        mainLayout.Controls.Add(CreateSettingsGroup("Impostazioni Output 2", _width2Numeric, _height2Numeric, _quality2Numeric, _lossless2Check));

        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _convertButton.Click += OnConvertClicked;
        actionPanel.Controls.Add(_convertButton);
        actionPanel.Controls.Add(_statusLabel);
        mainLayout.Controls.Add(actionPanel);

        Controls.Add(mainLayout);
    }

    private async void OnConvertClicked(object? sender, EventArgs e)
    {
        try
        {
            _convertButton.Enabled = false;
            _statusLabel.Text = "Conversione in corso...";

            ValidateGuiInputs();

            var inputPath = _inputTextBox.Text.Trim();
            var output1Path = _output1TextBox.Text.Trim();
            var output2Path = _output2TextBox.Text.Trim();

            var width1 = (int)_width1Numeric.Value;
            var height1 = (int)_height1Numeric.Value;
            var quality1 = (int)_quality1Numeric.Value;
            var lossless1 = _lossless1Check.Checked;

            var width2 = (int)_width2Numeric.Value;
            var height2 = (int)_height2Numeric.Value;
            var quality2 = (int)_quality2Numeric.Value;
            var lossless2 = _lossless2Check.Checked;

            await Task.Run(() =>
            {
                SvgWebpConverter.ConvertSvgToWebp(inputPath, output1Path, width1, height1, quality1, lossless1);
                SvgWebpConverter.ConvertSvgToWebp(inputPath, output2Path, width2, height2, quality2, lossless2);
            });

            _statusLabel.Text = "Conversione completata.";
            MessageBox.Show(
                $"Conversione completata con successo.\n\nFile creati:\n- {output1Path}\n- {output2Path}",
                "Successo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Errore durante la conversione.";
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _convertButton.Enabled = true;
        }
    }

    private void ValidateGuiInputs()
    {
        if (string.IsNullOrWhiteSpace(_inputTextBox.Text))
        {
            throw new ArgumentException("Seleziona un file SVG di input.");
        }

        if (!File.Exists(_inputTextBox.Text.Trim()))
        {
            throw new FileNotFoundException("Il file SVG selezionato non esiste.");
        }

        if (string.IsNullOrWhiteSpace(_output1TextBox.Text) || string.IsNullOrWhiteSpace(_output2TextBox.Text))
        {
            throw new ArgumentException("Specifica entrambi i percorsi di output WebP.");
        }
    }

    private static GroupBox CreateSettingsGroup(string title, NumericUpDown width, NumericUpDown height, NumericUpDown quality, CheckBox lossless)
    {
        var group = new GroupBox { Text = title, AutoSize = true, Dock = DockStyle.Fill };
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8) };

        layout.Controls.Add(new Label { Text = "Width", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        layout.Controls.Add(width);
        layout.Controls.Add(new Label { Text = "Height", AutoSize = true, Padding = new Padding(12, 8, 0, 0) });
        layout.Controls.Add(height);
        layout.Controls.Add(new Label { Text = "Quality", AutoSize = true, Padding = new Padding(12, 8, 0, 0) });
        layout.Controls.Add(quality);
        layout.Controls.Add(lossless);

        group.Controls.Add(layout);
        return group;
    }

    private static Panel CreatePathRow(string labelText, TextBox textBox, Action browseAction)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Height = 48 };
        var label = new Label { Text = labelText, AutoSize = true, Left = 0, Top = 14, Width = 100 };

        textBox.Left = 110;
        textBox.Top = 10;

        var button = new Button { Text = "Sfoglia", Width = 90, Height = 28, Left = 580, Top = 9 };
        button.Click += (_, _) => browseAction();

        panel.Controls.Add(label);
        panel.Controls.Add(textBox);
        panel.Controls.Add(button);
        return panel;
    }

    private void BrowseInput()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "SVG files (*.svg)|*.svg|All files (*.*)|*.*",
            Title = "Seleziona file SVG"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _inputTextBox.Text = dialog.FileName;

            var baseName = Path.GetFileNameWithoutExtension(dialog.FileName);
            var folder = Path.GetDirectoryName(dialog.FileName) ?? Environment.CurrentDirectory;

            if (string.IsNullOrWhiteSpace(_output1TextBox.Text))
            {
                _output1TextBox.Text = Path.Combine(folder, $"{baseName}_512.webp");
            }

            if (string.IsNullOrWhiteSpace(_output2TextBox.Text))
            {
                _output2TextBox.Text = Path.Combine(folder, $"{baseName}_1024.webp");
            }
        }
    }

    private void BrowseOutput(TextBox target)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "WebP files (*.webp)|*.webp|All files (*.*)|*.*",
            Title = "Seleziona output WebP"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.FileName;
        }
    }

    private static NumericUpDown CreateNumeric(decimal defaultValue, decimal min, decimal max)
    {
        return new NumericUpDown
        {
            Width = 80,
            Minimum = min,
            Maximum = max,
            Value = defaultValue,
        };
    }
}
