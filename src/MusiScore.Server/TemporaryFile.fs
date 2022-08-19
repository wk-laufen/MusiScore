namespace MusiScore.Server

open System
open System.IO

type TemporaryFile(path) =
    member _.Path with get() = path
    interface IDisposable with
        member _.Dispose() =
            try
                File.Delete(path)
            with :? FileNotFoundException -> ()
module TemporaryFile =
    let create extension content = async {
        let path = Path.ChangeExtension(Path.GetTempFileName(), extension)
        do! File.WriteAllBytesAsync(path, content) |> Async.AwaitTask
        return new TemporaryFile(path)
    }
