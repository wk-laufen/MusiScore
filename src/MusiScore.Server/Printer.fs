namespace MusiScore.Server

open System.Diagnostics

type Printer(printServer: string, printerName) =
    member _.PrintPdf content (printSettings: PrintSettings) count = async {
        let content = if printSettings.ReorderPagesAsBooklet then PDF.reorderAsBooklet content else content
        use! temporaryFile = TemporaryFile.createWithContent ".pdf" content
        let psi = ProcessStartInfo("lp", $"-h %s{printServer} -d %s{printerName} %s{printSettings.CupsCommandLineArgs} -n %d{count} %s{temporaryFile.Path}")
        printfn $"Running \"%s{psi.FileName} %s{psi.Arguments}\""
        let p = Process.Start(psi)
        let! ct = Async.CancellationToken
        do! p.WaitForExitAsync(ct) |> Async.AwaitTask
        if p.ExitCode <> 0 then failwith "Error while printing: lp exited with code %d{p.ExitCode}"
    }
