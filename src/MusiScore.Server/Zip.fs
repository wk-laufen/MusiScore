namespace MusiScore.Server

open System.IO
open System.IO.Compression

type ArchiveItem =
    | ArchiveFolder of name: string * children: Async<ArchiveItem list>
    /// the content is written when the entry is reached, so it never has to be held in memory as a whole
    | ArchiveFile of name: string * writeContent: (Stream -> Async<unit>)

type Archive = ArchiveItem list

module ArchiveFile =
    let ofBytes name (content: byte[]) =
        ArchiveFile (name, fun target -> target.WriteAsync(content, 0, content.Length) |> Async.AwaitTask)

module Zip =
    let createFile archive = async {
        let file = new TemporaryFile(".zip")
        use fileStream = File.OpenWrite(file.Path)
        use zipStream = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen = true)
        let rec addArchiveItem path item = async {
            match item with
            | ArchiveFolder (name, children) ->
                let path = $"{path}{name}/"
                zipStream.CreateEntry(path) |> ignore
                let! children = children
                return!
                    children
                    |> List.map (addArchiveItem path)
                    |> Async.Sequential
                    |> Async.Ignore
            | ArchiveFile (name, writeContent) ->
                // PDFs are compressed already, deflating them again costs a lot of time and saves close to nothing
                let entry = zipStream.CreateEntry($"{path}{name}", CompressionLevel.NoCompression)
                use target = entry.Open()
                do! writeContent target
        }
        do!
            archive
            |> Seq.map (addArchiveItem "")
            |> Async.Sequential
            |> Async.Ignore
        // the caller is responsible for deleting the file, e.g. by reading it with `FileOptions.DeleteOnClose`
        return file.Path
    }
