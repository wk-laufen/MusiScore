namespace MusiScore.Server

open System.IO
open System.IO.Compression

type ArchiveItem =
    | Folder of name: string * children: ArchiveItem list
    | File of name: string * content: byte[]

type Archive = ArchiveItem list

module Zip =
    let create archive =
        use stream = new MemoryStream()
        do
            use zipStream = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen = true)
            let rec addArchiveItem path = function
                | Folder (name, children) ->
                    let path = $"{path}{name}/"
                    zipStream.CreateEntry(path) |> ignore
                    children
                    |> List.iter (addArchiveItem path)
                | File (name, content) ->
                    let entry = zipStream.CreateEntry($"{path}{name}")
                    use target = entry.Open()
                    use source = new MemoryStream(content)
                    source.CopyTo(target)
            archive |> Seq.iter (addArchiveItem "")
        stream.ToArray()
