namespace Company.Function

open System.IO
open Azure.Storage.Blobs
open Microsoft.Azure.Functions.Worker
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open ImageCreator
open RequestValidator

module BlogCardGenerator =
    [<Function "Test">]
    let runTest ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = "test")>] req: HttpRequest) (log: ILogger) =
        async {
            log.LogInformation "Test"
            return OkResult() :> IActionResult
        }
        |> Async.StartAsTask

    let width = 640.f
    let height = 250.f

    let downloadImage (postImage: BlobClient) =
        async {
            let! content = postImage.DownloadContentAsync() |> Async.AwaitTask

            return content.Value.Content
        }

    [<Function "BlogCardGenerator">]
    let run
        ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = "title-card/{id}")>] req: HttpRequest)
        ([<BlobInput("title-cards/{id}.png", Connection = "ImageStorage")>] postImage: BlobClient)
        (id: string)
        (log: ILogger)
        =
        async {
            log.LogInformation <| sprintf "ID: %s" id

            let! blogData = getBlogMetadata ()

            match tryFindPost id blogData.Posts with
            | Some post ->
                let! exists = postImage.ExistsAsync() |> Async.AwaitTask

                if exists.Value then
                    log.LogInformation "Image existed"
                    let! file = downloadImage postImage
                    return FileStreamResult(file.ToStream(), "image/png") :> IActionResult
                else
                    let title = post.Title
                    let author = "Aaron Powell"
                    let date = post.Date

                    log.LogInformation
                    <| sprintf "title: %s, author: %s, date: %A" title author date

                    log.LogInformation "Image doesn't exist"
                    use image = makeImage width height title author date post.Tags

                    log.LogInformation "Created Image"

                    let ms = imageToStream image

                    log.LogInformation "Copied to stream"

                    let! _ = postImage.UploadAsync ms |> Async.AwaitTask
                    ms.Position <- int64 0

                    log.LogInformation "Uploaded image"

                    return FileStreamResult(ms, "image/png") :> IActionResult
            | None -> return NotFoundResult() :> IActionResult
        }
        |> Async.StartAsTask
