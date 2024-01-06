namespace MusiScore.Server

open System.IO
open System.IO.Compression

type ArchiveItem =
    | ArchiveFolder of name: string * children: Async<ArchiveItem list>
    | ArchiveFile of name: string * content: byte[]

type Archive = ArchiveItem list

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
            | ArchiveFile (name, content) ->
                let entry = zipStream.CreateEntry($"{path}{name}")
                use target = entry.Open()
                use source = new MemoryStream(content)
                source.CopyTo(target)
        }
        do!
            archive
            |> Seq.map (addArchiveItem "")
            |> Async.Sequential
            |> Async.Ignore
        return file.Path // TODO delete temporary file
    }
