namespace global

module Result =
    let toOption = function
        | Ok v -> Some v
        | Error _ -> None
    let errorToOption = function
        | Ok _ -> None
        | Error e -> Some e

module Stream =
    open System.IO

    let readAllBytes (v: Stream) = async {
        use memoryStream = new MemoryStream()
        do! v.CopyToAsync(memoryStream) |> Async.AwaitTask
        return memoryStream.ToArray()
    }
