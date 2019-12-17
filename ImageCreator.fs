module ImageCreator

open SixLabors.Fonts
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing
open SixLabors.Shapes
open SixLabors.Primitives
open System.IO

let generateBox (colour: Color) (width: float32) (height: float32) (offsetX: float32) (offsetY: float32)
    (image: Image<Rgba32>) =

    // main box
    image.Mutate
        (fun ctx ->
        ctx.Fill
            (GraphicsOptions(true), colour,
             RectangularPolygon(40.f + offsetX, 40.f + offsetY, width - 70.f, height - 60.f)) |> ignore)

    // left gutter
    image.Mutate
        (fun ctx ->
        ctx.Fill(GraphicsOptions(true), colour, RectangularPolygon(20.f + offsetX, 30.f + offsetY, 20.f, height - 50.f))
        |> ignore)

    // right gutter
    image.Mutate
        (fun ctx ->
        ctx.Fill
            (GraphicsOptions(true), colour,
             RectangularPolygon(width - 40.f + offsetX, 30.f + offsetY, 20.f, height - 50.f)) |> ignore)

    // top gutter
    image.Mutate
        (fun ctx ->
        ctx.Fill(GraphicsOptions(true), colour, RectangularPolygon(30.f + offsetX, 20.f + offsetY, width - 60.f, 20.f))
        |> ignore)

    // rounded corner - left top
    image.Mutate
        (fun ctx ->
        ctx.Fill(GraphicsOptions(true), colour, EllipsePolygon(30.f + offsetX, 30.f + offsetY, 20.f, 20.f)) |> ignore)

    // rounded corner - right top
    image.Mutate
        (fun ctx ->
        ctx.Fill(GraphicsOptions(true), colour, EllipsePolygon(width - 30.f + offsetX, 30.f + offsetY, 20.f, 20.f))
        |> ignore)

    image

let addText (text: string) (fontSize: float32) (xEnd: float32) (y: float32) (image: Image<Rgba32>) =
    let fam = SystemFonts.Find "DejaVu Sans Mono"
    let font = Font(fam, fontSize)

    let pb = PathBuilder()
    pb.SetOrigin(PointF(40.f, 0.f)) |> ignore
    pb.AddLine(0.f, y, xEnd, y) |> ignore
    let path = pb.Build()

    let mutable opts = TextGraphicsOptions true
    opts.WrapTextWidth <- path.Length

    let mutable ro = RendererOptions(font, 72.f)
    ro.HorizontalAlignment <- opts.HorizontalAlignment
    ro.TabWidth <- opts.TabWidth
    ro.VerticalAlignment <- opts.VerticalAlignment
    ro.WrappingWidth <- opts.WrapTextWidth
    ro.ApplyKerning <- opts.ApplyKerning

    let glyphs = TextBuilder.GenerateGlyphs(text, path, ro)

    let gopts = TextGraphicsOptions.op_Explicit opts

    image.Mutate(fun ctx -> ctx.Fill(gopts, Color.Black, glyphs) |> ignore)

    image

let makeImage width height title author date =
    let image = new Image<Rgba32>(int width, int height)
    image.Mutate(fun ctx -> ctx.Fill(Color.FromHex "02bdd5") |> ignore)

    generateBox (Rgba32.op_Implicit <| Rgba32.FromHex "333") width height 5.f 5.f image
    |> generateBox (Rgba32.op_Implicit Rgba32.White) width height 0.f 0.f
    |> addText title 30.f (width - 60.f) (height / 2.f)
    |> addText (sprintf "%s | %s" author date) 20.f (width - 60.f) (height / 2.f + 40.f)
    |> ignore
    image

let imageToStream (image: Image<Rgba32>) =
    let ms = new MemoryStream()
    image.SaveAsPng ms
    ms.Position <- int64 0
    ms
