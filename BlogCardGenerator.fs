namespace Company.Function

open System
open System.IO
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System.Security
open System.Text
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats
open ImageCreator
open Microsoft.WindowsAzure.Storage.Blob

module BlogCardGenerator =
    [<Literal>]
    let PostTitle = "title"

    [<Literal>]
    let Author = "author"

    [<Literal>]
    let Date = "date"

    let gqs (req: HttpRequest) name = req.Query.[name].[0]

    let width = 640.f
    let height = 250.f

    [<FunctionName("BlogCardGenerator")>]
    let run ([<HttpTrigger(AuthorizationLevel.Function, "get", "post")>] req: HttpRequest)
        ([<Blob("title-cards/{title}.png", FileAccess.ReadWrite)>] postImage: ICloudBlob) (log: ILogger) =
        async {
            let gqs' = gqs req

            let title = gqs' PostTitle
            let author = gqs' Author
            let date = gqs' Date

            let! exists = postImage.ExistsAsync() |> Async.AwaitTask

            if exists then
                let ms = new MemoryStream()
                do! postImage.DownloadToStreamAsync ms |> Async.AwaitTask
                return FileStreamResult(ms, "image/png")
            else
                use image = new Image<Rgba32>(int width, int height)
                generateBox (Rgba32.op_Implicit <| Rgba32.FromHex "333") width height 5.f 5.f image
                |> generateBox (Rgba32.op_Implicit Rgba32.White) width height 0.f 0.f
                |> addText title 30.f (width - 60.f) (height / 2.f)
                |> addText (sprintf "%s | %s" author date) 20.f (width - 60.f) (height / 2.f + 40.f)
                |> ignore

                let ms = new MemoryStream()
                image.SaveAsPng ms
                ms.Position <- int64 0

                do! postImage.UploadFromStreamAsync ms |> Async.AwaitTask

                return FileStreamResult(ms, "image/png")
        }
        |> Async.StartAsTask
