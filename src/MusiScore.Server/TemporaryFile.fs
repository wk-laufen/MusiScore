namespace MusiScore.Server

open System
open System.IO

type TemporaryFile(extension) =
    let path = Path.ChangeExtension(Path.GetTempFileName(), extension)
    member _.Path with get() = path
    interface IDisposable with
        member _.Dispose() =
            try
                File.Delete(path)
            with :? FileNotFoundException -> ()
module TemporaryFile =
    let createWithContent extension content = async {
        let file = new TemporaryFile(extension)
        do! File.WriteAllBytesAsync(file.Path, content) |> Async.AwaitTask
        return file
    }
