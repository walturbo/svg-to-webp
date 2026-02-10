# SVG to WebP Converter (Windows 11 x64)

Programma in **C# / .NET 10** con:
- **GUI Windows Forms** per uso semplice (senza riga di comando)
- **modalità CLI** opzionale per automazioni/script

Converte un file SVG in **due file WebP** con risoluzione e qualità configurabili.

## Requisiti
- Windows 11 x64
- .NET SDK 10.0 (x64)
- Connessione Internet (oppure mirror NuGet aziendale configurato)

> Nota dipendenze: il progetto usa `Svg.Skia 3.0.0` e `SkiaSharp 3.116.1`.

## Avvio GUI (consigliato)
```bash
dotnet run
```

Nella finestra:
1. Seleziona il file SVG di input.
2. Scegli i due file di output WebP.
3. Imposta width/height/quality per ogni output.
4. (Opzionale) attiva `Lossless` per massima fedeltà colore.
5. Clicca **Converti**.

## Modalità CLI
```bash
dotnet run -- \
  --input logo.svg \
  --output1 logo_512.webp \
  --output2 logo_1024.webp \
  --width1 512 --height1 512 \
  --width2 1024 --height2 1024 \
  --quality 85
```

### Parametri CLI
- `--input` (obbligatorio): percorso file SVG di ingresso.
- `--output1` (obbligatorio): primo file WebP di uscita.
- `--output2` (obbligatorio): secondo file WebP di uscita.
- `--width1`, `--height1`: risoluzione del primo output (default `512x512`).
- `--width2`, `--height2`: risoluzione del secondo output (default `1024x1024`).
- `--quality`: qualità comune (0-100) per entrambi i file.
- `--quality1`, `--quality2`: qualità per singolo output (precedenza su `--quality`).
- `--lossless`: forza qualità WebP 100 su entrambi i file.
- `--lossless1`, `--lossless2`: forza qualità WebP 100 su un output specifico.
- `--help`: mostra aiuto CLI.

## Build
```bash
dotnet restore
dotnet build -c Release
```

## Pubblicazione EXE per Windows 11 x64
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

L'eseguibile sarà nella cartella:
`bin/Release/net10.0-windows/win-x64/publish/`

## Colori “sbiaditi”
Il rendering usa una superficie Skia con **spazio colore sRGB esplicito**. Per la massima fedeltà usa `Lossless` (GUI) o `--lossless` (CLI).

## Risoluzione errori NuGet
Se vedi errori come `NU1100` / `NU1102`:

1. Verifica che `nuget.org` sia presente:
   ```bash
   dotnet nuget list source
   ```
2. Se manca, aggiungilo:
   ```bash
   dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
   ```
3. Pulisci cache e riprova:
   ```bash
   dotnet nuget locals all --clear
   dotnet restore --force
   ```

In questo repository è incluso anche `NuGet.Config` con `nuget.org` già configurato.
