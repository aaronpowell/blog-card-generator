module ImageCreator

open SixLabors.Fonts
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing
open SixLabors.ImageSharp.Drawing.Processing

open System.IO
open System
open SixLabors.ImageSharp.Drawing

let cornerRadius = 10.f
let gutter = 20.f

let getFontName =
    match Environment.OSVersion.Platform with
    | PlatformID.Unix -> "DejaVu Sans Mono"
    | _ -> "Consolas"


let generateBox
    (colour: Color)
    (width: float32)
    (height: float32)
    (xOffset: float32)
    (yOffset: float32)
    (image: Image<Rgba32>)
    =
    let xStart = gutter + cornerRadius
    let yStart = gutter + cornerRadius

    // main box
    image.Mutate(fun ctx ->
        let mutable opts = DrawingOptions()
        opts.GraphicsOptions <- GraphicsOptions()

        ctx.Fill(opts, colour, RectangularPolygon(xStart + xOffset, yStart + yOffset, width - xStart, height - yStart))
        |> ignore)

    // left gutter
    image.Mutate(fun ctx ->
        let mutable opts = DrawingOptions()
        opts.GraphicsOptions <- GraphicsOptions()

        ctx.Fill(
            opts,
            colour,
            RectangularPolygon(gutter + xOffset, yStart + yOffset, cornerRadius, height - yStart - gutter)
        )
        |> ignore)

    // right gutter
    image.Mutate(fun ctx ->
        let mutable opts = DrawingOptions()
        opts.GraphicsOptions <- GraphicsOptions()

        ctx.Fill(
            opts,
            colour,
            RectangularPolygon(width - xStart + xOffset, yStart + yOffset, cornerRadius, height - yStart - gutter)
        )
        |> ignore)

    // top gutter
    image.Mutate(fun ctx ->
        let mutable opts = DrawingOptions()
        opts.GraphicsOptions <- GraphicsOptions()

        ctx.Fill(
            opts,
            colour,
            RectangularPolygon(
                xStart + xOffset,
                gutter + yOffset,
                width - xStart - gutter - cornerRadius,
                cornerRadius
            )
        )
        |> ignore)

    // rounded corner - left top
    image.Mutate(fun ctx ->
        let mutable opts = DrawingOptions()
        opts.GraphicsOptions <- GraphicsOptions()

        ctx.Fill(
            opts,
            colour,
            EllipsePolygon(xStart + xOffset, yStart + yOffset, cornerRadius * 2.f, cornerRadius * 2.f)
        )
        |> ignore)

    // rounded corner - right top
    image.Mutate(fun ctx ->
        let mutable opts = DrawingOptions()
        opts.GraphicsOptions <- GraphicsOptions()

        ctx.Fill(
            opts,
            colour,
            EllipsePolygon(width - xStart + xOffset, yStart + yOffset, cornerRadius * 2.f, cornerRadius * 2.f)
        )
        |> ignore)

    image

let addText (text: string) (fontSize: float32) (xEnd: float32) (y: float32) (image: Image<Rgba32>) =
    let fam = getFontName |> SystemFonts.Get
    let font = Font(fam, fontSize)

    let pb = PathBuilder()
    pb.SetOrigin(PointF(gutter * 2.f, 0.f)) |> ignore
    pb.AddLine(0.f, y, xEnd, y) |> ignore
    let path = pb.Build()

    let mutable ro = TextOptions(font)
    ro.WrappingLength <- path.ComputeLength()

    let glyphs = TextBuilder.GenerateGlyphs(text, path, ro)

    image.Mutate(fun ctx -> ctx.DrawText(ro, text, Color.Black) |> ignore)

    image

let makeImage width height title author (date: DateTimeOffset) tags =
    let image = new Image<Rgba32>(int width, int height)
    image.Mutate(fun ctx -> ctx.Fill(Color.ParseHex "02bdd5") |> ignore)

    let textX = width - (gutter + cornerRadius) * 2.f
    let textY = height / 2.f

    generateBox (Color.ParseHex "333") width height 5.f 5.f image
    |> generateBox Color.White width height 0.f 0.f
    |> addText title 30.f textX textY
    |> addText (sprintf "%s | %s" author (date.ToString "MMMM dd, yyyy")) 20.f textX (textY + 40.f)
    |> addText
        (tags |> Array.map (fun t -> sprintf "#%s" t) |> Array.toSeq |> String.concat " ")
        15.f
        textX
        (textY + 70.f)

let imageToStream (image: Image<Rgba32>) =
    let ms = new MemoryStream()
    image.SaveAsPng ms
    ms.Position <- int64 0
    ms
