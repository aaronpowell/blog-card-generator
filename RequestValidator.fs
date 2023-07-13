module RequestValidator

open FSharp.Data

[<Literal>]
let JsonExample =
    """
{
    "posts": [{
        "title": "Post Title",
        "date": "Tue, 17 Dec 2019 08:50:14 +1100",
        "tags": ["tag", "tag2"],
        "id": "Test"
    }]
}
"""

type Blog = JsonProvider<JsonExample>

let getBlogMetadata () =
    Blog.AsyncLoad "https://www.aaron-powell.com/index.json"

let tryFindPost (id: string) (blogs: Blog.Post array) =
    blogs |> Array.tryFind (fun blog -> blog.Id = id)
