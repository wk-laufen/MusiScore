namespace MusiScore.Server

type Composition = {
    Id: string
    Title: string
}

type Voice = {
    Id: string
    Name: string
}

type PrintSetting =
    | Duplex
    | A4ToA3Duplex
    | A4ToBooklet

type PrintableVoice = {
    File: byte[]
    PrintSetting: PrintSetting
}
