namespace Company.Function

open System.IO
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
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

    let downloadImage (postImage: ICloudBlob) =
        async {
            let ms = new MemoryStream()
            do! postImage.DownloadToStreamAsync ms |> Async.AwaitTask
            return ms
        }

    [<FunctionName("BlogCardGenerator")>]
    let run ([<HttpTrigger(AuthorizationLevel.Function, "get", "post")>] req: HttpRequest)
        ([<Blob("title-cards/{title}.png", FileAccess.ReadWrite)>] postImage: ICloudBlob) =
        async {
            let gqs' = gqs req

            let title = gqs' PostTitle
            let author = gqs' Author
            let date = gqs' Date

            let! exists = postImage.ExistsAsync() |> Async.AwaitTask

            if exists then
                let! ms = downloadImage postImage
                return FileStreamResult(ms, "image/png")
            else
                use image = makeImage width height title author date

                let ms = imageToStream image

                do! postImage.UploadFromStreamAsync ms |> Async.AwaitTask

                return FileStreamResult(ms, "image/png")
        }
        |> Async.StartAsTask
