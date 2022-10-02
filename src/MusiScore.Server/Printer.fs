namespace MusiScore.Server

open System.Diagnostics

type Printer(printServer: string, printerName) =
    member _.PrintPdf content printSetting count = async {
        let printOptions =
            match printSetting with
            | Duplex -> "-o media=A4 -o sides=two-sided-long-edge"
            | A4ToA3Duplex
            | A4ToBooklet -> "-o number-up=2 -o media=A3 -o sides=two-sided-long-edge"
        use! temporaryFile = TemporaryFile.createWithContent ".pdf" content
        let psi = ProcessStartInfo("lp", $"-h %s{printServer} -d %s{printerName} %s{printOptions} -n %d{count} %s{temporaryFile.Path}")
        printfn $"Running \"%s{psi.FileName} %s{psi.Arguments}\""
        let p = Process.Start(psi)
        let! ct = Async.CancellationToken
        do! p.WaitForExitAsync(ct) |> Async.AwaitTask
        if p.ExitCode <> 0 then failwith "Error while printing: lp exited with code %d{p.ExitCode}"
    }
