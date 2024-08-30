namespace MusiScore.Server

open iText.Kernel.Pdf
open System.IO

module PDF =
    let isValid (content: byte array) =
        try
            use docStream = new MemoryStream(content)
            use doc = new PdfDocument(new PdfReader(docStream))
            let _ = doc.GetNumberOfPages()
            true
        with _ -> false

    let getPageSizes (content: byte[]) =
        use docStream = new MemoryStream(content)
        use doc = new PdfDocument(new PdfReader(docStream))
        [ 1..doc.GetNumberOfPages() ]
        |> List.map (doc.GetPage >> (fun v -> v.GetPageSize()) >> (fun v -> v.GetWidth(), v.GetHeight()))

    let getBookletPageOrder pageCount =
        let n = if pageCount % 4 = 0 then pageCount else pageCount + (4 - pageCount % 4)
        [1 .. 2 .. n / 2]
        |> List.collect (fun i ->
            [ n - i + 1; i; i + 1; n - i ]
        )
        |> List.map (fun i ->
            if i <= pageCount then Some i else None
        )

    let reorderAsBooklet (content: byte[]) =
        use sourceStream = new MemoryStream(content)
        use sourceDocument = new PdfDocument(new PdfReader(sourceStream))
        use targetStream = new MemoryStream()
        do
            use targetDocument = new PdfDocument(new PdfWriter(targetStream))
            getBookletPageOrder (sourceDocument.GetNumberOfPages())
            |> List.iter (fun pageNumber ->
                match pageNumber with
                | Some pageNumber -> sourceDocument.CopyPagesTo([| pageNumber |], targetDocument) |> ignore
                | None -> sourceDocument.AddNewPage() |> ignore
            )
        targetStream.ToArray()
