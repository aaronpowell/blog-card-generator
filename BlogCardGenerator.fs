namespace Company.Function

open System.IO
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open ImageCreator
open Microsoft.WindowsAzure.Storage.Blob
open Microsoft.Extensions.Logging

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
            ms.Position <- int64 0
            return ms
        }

    [<FunctionName("BlogCardGenerator")>]
    let run ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = "title-card/{id}")>] req: HttpRequest)
        ([<Blob("title-cards/{id}.png", FileAccess.ReadWrite)>] postImage: ICloudBlob) (id: string) (log: ILogger) =
        async {
            log.LogInformation <| sprintf "ID: %s" id
            let gqs' = gqs req

            let title = gqs' PostTitle
            let author = gqs' Author
            let date = gqs' Date

            log.LogInformation <| sprintf "title: %s, author: %s, date: %s" title author date

            let! exists = postImage.ExistsAsync() |> Async.AwaitTask

            if exists then
                log.LogInformation "Image existed"
                let! ms = downloadImage postImage
                return FileStreamResult(ms, "image/png")
            else
                log.LogInformation "Image doesn't exist"
                use image = makeImage width height title author date

                log.LogInformation "Created Image"

                let ms = imageToStream image

                log.LogInformation "Copied to stream"

                do! postImage.UploadFromStreamAsync ms |> Async.AwaitTask
                ms.Position <- int64 0

                log.LogInformation "Uploaded image"

                return FileStreamResult(ms, "image/png")
        }
        |> Async.StartAsTask
