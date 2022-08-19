namespace MusiScore.Server

open Dapper
open MySqlConnector
open System.Data

module private DbModels =
    type DbComposition = {
        id: int
        title: string
    }
    module DbComposition =
        let toDomain v : Composition =
            { Id = string v.id; Title = v.title }

    type DbVoice = {
        id: int
        title: string
    }
    module DbVoice =
        let toDomain v : Voice =
            { Id = string v.id; Name = v.title }

    type DbPrintableVoice = {
        file: byte[]
        print_setting_id: string
    }
    module DbPrintSetting =
        let toDomain v =
            match v with
            | "duplex" -> Duplex
            | "a4_to_a3_duplex" -> A4ToA3Duplex
            | "a4_to_booklet" -> A4ToBooklet
            | v -> failwith $"Invalid print setting: \"%s{v}\""
    module DbPrintableVoice =
        let toDomain v : PrintableVoice =
            { File = v.file; PrintSetting = DbPrintSetting.toDomain v.print_setting_id }

type Db(connectionString: string) =
    let createConnection() =
        new MySqlConnection(connectionString) :> IDbConnection

    member _.GetActiveCompositions() = async {
        use connection = createConnection()
        let! compositions = connection.QueryAsync<DbModels.DbComposition>("SELECT id, title FROM composition WHERE is_active = true") |> Async.AwaitTask
        return
            compositions
            |> Seq.map (DbModels.DbComposition.toDomain)
            |> Seq.toList
    }

    member _.GetCompositionVoices(compositionId: string) = async {
        use connection = createConnection()
        let! voices = connection.QueryAsync<DbModels.DbVoice>("SELECT id, name FROM voice WHERE composition_id = @CompositionId", {| CompositionId = compositionId |}) |> Async.AwaitTask
        return
            voices
            |> Seq.map (DbModels.DbVoice.toDomain)
            |> Seq.toList
    }

    member _.GetPrintableVoice(_compositionId: string, voiceId: string) = async {
        use connection = createConnection()
        let! voice = connection.QuerySingleAsync<DbModels.DbPrintableVoice>("SELECT file, print_setting_id FROM voice WHERE id = @VoiceId", {| VoiceId = voiceId |}) |> Async.AwaitTask
        return DbModels.DbPrintableVoice.toDomain voice
    }

