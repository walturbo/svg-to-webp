# SVG to WebP Converter (Windows 11 x64)

Programma console in **C# / .NET 10** che converte un file SVG in **due file WebP** con risoluzione e qualità configurabili.

## Perché C#/.NET è una buona scelta
- Ottimo supporto su Windows 11.
- Possibilità di pubblicare un eseguibile standalone (`.exe`) per `win-x64`.
- Librerie stabili per rendering SVG e encoding WebP (`SkiaSharp` + `Svg.Skia`).

## Requisiti
- .NET SDK 10.0 (x64)
- Connessione Internet (oppure mirror NuGet aziendale configurato)

> Nota dipendenze: il progetto usa `Svg.Skia 3.0.0` e `SkiaSharp 3.116.1` per evitare errori di downgrade NuGet (`NU1605`).

## Build
```bash
dotnet restore
dotnet build -c Release
```

## Esecuzione
```bash
dotnet run -- \
  --input logo.svg \
  --output1 logo_512.webp \
  --output2 logo_1024.webp \
  --width1 512 --height1 512 \
  --width2 1024 --height2 1024 \
  --quality 85
```

## Parametri
- `--input` (obbligatorio): percorso file SVG di ingresso.
- `--output1` (obbligatorio): primo file WebP di uscita.
- `--output2` (obbligatorio): secondo file WebP di uscita.
- `--width1`, `--height1`: risoluzione del primo output (default `512x512`).
- `--width2`, `--height2`: risoluzione del secondo output (default `1024x1024`).
- `--quality`: qualità comune (0-100) per entrambi i file.
- `--quality1`, `--quality2`: qualità per singolo output (hanno precedenza su `--quality`).

## Pubblicazione EXE per Windows 11 x64
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
L'eseguibile sarà nella cartella:
`bin/Release/net10.0/win-x64/publish/`

## Risoluzione errore NU1100 (pacchetti non risolti)
Se vedi errori come `NU1100` o `NU1102` sui pacchetti:

0. Verifica di usare il pacchetto corretto per SVG: `Svg.Skia` (evita `SkiaSharp.Svg` / `SkiaSharp.Extended.Svg` se non presenti su nuget.org).

1. Verifica che `nuget.org` sia presente:
   ```bash
   dotnet nuget list source
   ```
2. Se manca, aggiungilo:
   ```bash
   dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
   ```
3. Se sei in rete aziendale, configura proxy o feed interno NuGet.
4. Pulisci cache e riprova:
   ```bash
   dotnet nuget locals all --clear
   dotnet restore --force
   ```

In questo repository è incluso anche `NuGet.Config` con `nuget.org` già configurato.
