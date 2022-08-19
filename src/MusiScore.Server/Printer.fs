namespace MusiScore.Server

open System.Diagnostics

module Printer =
    let printPdf content printSetting count = async {
        let printOptions =
            match printSetting with
            | Duplex -> "-o sides=two-sided-long-edge"
            | A4ToA3Duplex
            | A4ToBooklet -> "-o number-up=2 -o media=A3 -o sides=two-sided-long-edge"
        use! temporaryFile = TemporaryFile.create ".pdf" content
        let psi = ProcessStartInfo("lp", $"%s{printOptions} -n %d{count} %s{temporaryFile.Path}")
        let p = Process.Start(psi)
        let! ct = Async.CancellationToken
        do! p.WaitForExitAsync(ct) |> Async.AwaitTask
    }
