namespace global

module Result =
    let toOption = function
        | Ok v -> Some v
        | Error _ -> None
    let errorToOption = function
        | Ok _ -> None
        | Error e -> Some e

module Collation =
    open System
    open System.Globalization

    /// Compares strings the way a German reader expects, e.g. case-insensitive and umlauts next to their base letter.
    let comparer = StringComparer.Create(CultureInfo "de-AT", true)
    let compare (a: string) (b: string) = comparer.Compare(a, b)

module Stream =
    open System.IO

    let readAllBytes (v: Stream) = async {
        use memoryStream = new MemoryStream()
        do! v.CopyToAsync(memoryStream) |> Async.AwaitTask
        return memoryStream.ToArray()
    }
