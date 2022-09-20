namespace MusiScore.Server

open Dapper
open Npgsql
open System.Data

[<AutoOpen>]
module private DbModels =
    type DbActiveComposition = {
        id: int
        title: string
    }
    module DbActiveComposition =
        let toDomain v : ActiveComposition =
            { Id = string v.id; Title = v.title }

    type DbVoice = {
        id: int
        name: string
    }
    module DbVoice =
        let toDomain v : Voice =
            { Id = string v.id; Name = v.name }

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
        let fromDomain v =
            match v with
            | Duplex -> "duplex"
            | A4ToA3Duplex -> "a4_to_a3_duplex"
            | A4ToBooklet -> "a4_to_booklet"
    module DbPrintableVoice =
        let toDomain v : PrintableVoice =
            { File = v.file; PrintSetting = DbPrintSetting.toDomain v.print_setting_id }

    type DbComposition = {
        id: int
        title: string
        is_active: bool
    }
    module DbComposition =
        let toDomain v : Composition =
            { Id = string v.id; Title = v.title; IsActive = v.is_active }

    type DbFullVoice = {
        id: int
        name: string
        file: byte[]
        print_setting_id: string
    }
    module DbFullVoice =
        let toDomain v : FullVoice =
            { Id = string v.id; Name = v.name; File = v.file; PrintSetting = DbPrintSetting.toDomain v.print_setting_id }
    
    type DbVoicePrintSetting = {
        Key: string
        Name: string
    }
    module DbVoicePrintSetting =
        let toDomain v : VoicePrintSetting =
            { Key = DbPrintSetting.toDomain v.Key; Name = v.Name }

type Db(connectionString: string) =
    let createConnection() =
        new NpgsqlConnection(connectionString) :> IDbConnection

    member _.GetActiveCompositions() = async {
        use connection = createConnection()
        let! compositions = connection.QueryAsync<DbActiveComposition>("SELECT id, title FROM composition WHERE is_active = true") |> Async.AwaitTask
        return
            compositions
            |> Seq.map DbActiveComposition.toDomain
            |> Seq.toList
    }

    member _.GetCompositionVoices(compositionId: string) = async {
        use connection = createConnection()
        let! voices = connection.QueryAsync<DbVoice>("SELECT id, name FROM voice WHERE composition_id = @CompositionId", {| CompositionId = int compositionId |}) |> Async.AwaitTask
        return
            voices
            |> Seq.map DbVoice.toDomain
            |> Seq.toList
    }

    member _.GetPrintableVoice (_compositionId: string, voiceId: string) = async {
        use connection = createConnection()
        let! voice = connection.QuerySingleAsync<DbPrintableVoice>("SELECT file, print_setting_id FROM voice WHERE id = @VoiceId", {| VoiceId = int voiceId |}) |> Async.AwaitTask
        return DbPrintableVoice.toDomain voice
    }

    member _.GetCompositions() = async {
        use connection = createConnection()
        let! compositions = connection.QueryAsync<DbComposition>("SELECT id, title, is_active FROM composition") |> Async.AwaitTask
        return
            compositions
            |> Seq.map DbComposition.toDomain
            |> Seq.toList
    }

    member _.CreateComposition (newComposition: NewComposition) = async {
        use connection = createConnection()
        connection.Open()
        let! compositionId = connection.ExecuteScalarAsync<int>("INSERT INTO composition (title, is_active) VALUES(@Title, false) RETURNING id", {| Title = newComposition.Title |}) |> Async.AwaitTask
        return string compositionId
    }

    member _.UpdateComposition (compositionId: string) (compositionUpdate: CompositionUpdate) = async {
        use connection = createConnection()
        connection.Open()
        use tx = connection.BeginTransaction()
        let updateFields =
            [
                match compositionUpdate.Title with
                | Some _ -> "title = @Title"
                | None -> ()

                match compositionUpdate.IsActive with
                | Some _ -> "is_active = @IsActive"
                | None -> ()
            ]
            |> String.concat ", "
        if updateFields <> "" then
            let updateArgs = {|
                Id = int compositionId
                Title = compositionUpdate.Title |> Option.defaultValue ""
                IsActive = compositionUpdate.IsActive |> Option.defaultValue false
            |}
            let command = $"UPDATE composition SET %s{updateFields} WHERE id = @Id"
            do! connection.ExecuteAsync(command, updateArgs, tx) |> Async.AwaitTask |> Async.Ignore
        let! composition = connection.QuerySingleAsync<DbComposition>("SELECT id, title, is_active FROM composition WHERE id = @Id", {| Id = int compositionId |}, tx) |> Async.AwaitTask
        tx.Commit()
        return DbComposition.toDomain composition
    }

    member _.DeleteComposition (compositionId: string) = async {
        use connection = createConnection()
        do! connection.ExecuteAsync("DELETE FROM composition WHERE id = @Id", {| Id = int compositionId |}) |> Async.AwaitTask |> Async.Ignore
    }

    member _.GetFullCompositionVoices (compositionId: string) = async {
        use connection = createConnection()
        let! voices = connection.QueryAsync<DbFullVoice>("SELECT id, name, file, print_setting_id FROM voice WHERE composition_id = @CompositionId", {| CompositionId = int compositionId |}) |> Async.AwaitTask
        return
            voices
            |> Seq.map DbFullVoice.toDomain
            |> Seq.toList
    }

    member _.CreateVoice (compositionId: string) (createVoice: CreateVoice) = async {
        use connection = createConnection()
        connection.Open()
        let command = "INSERT INTO voice (name, file, composition_id, print_setting_id) VALUES(@Name, @File, @CompositionId, @PrintSettingId) RETURNING id"
        let commandArgs = {|
            Name = createVoice.Name
            File = createVoice.File
            CompositionId = int compositionId
            PrintSettingId = DbPrintSetting.fromDomain createVoice.PrintSetting
        |}
        let! voiceId = connection.ExecuteScalarAsync<int>(command, commandArgs) |> Async.AwaitTask
        return string voiceId
    }

    member _.UpdateVoice (_compositionId: string) (voiceId: string) (updateVoice: UpdateVoice) = async {
        use connection = createConnection()
        connection.Open()
        use tx = connection.BeginTransaction()
        let updateFields =
            [
                match updateVoice.Name with
                | Some _ -> "name = @Name"
                | None -> ()

                match updateVoice.File with
                | Some _ -> "file = @File"
                | None -> ()

                match updateVoice.PrintSetting with
                | Some _ -> "print_setting_id = @PrintSettingId"
                | None -> ()
            ]
            |> String.concat ", "
        if updateFields <> "" then
            let updateArgs = {|
                Id = int voiceId
                Name = updateVoice.Name |> Option.defaultValue ""
                File = updateVoice.File |> Option.defaultValue Array.empty
                PrintSettingId = updateVoice.PrintSetting |> Option.map DbPrintSetting.fromDomain |> Option.defaultValue ""
            |}
            let command = $"UPDATE voice SET %s{updateFields} WHERE id = @Id"
            do! connection.ExecuteAsync(command, updateArgs, tx) |> Async.AwaitTask |> Async.Ignore
        let! voice = connection.QuerySingleAsync<DbFullVoice>("SELECT id, name, file, print_setting_id FROM voice WHERE id = @Id", {| Id = int voiceId |}, tx) |> Async.AwaitTask
        tx.Commit()
        return DbFullVoice.toDomain voice
    }

    member _.DeleteVoice (_compositionId: string) (voiceId: string) = async {
        use connection = createConnection()
        do! connection.ExecuteAsync("DELETE FROM voice WHERE id = @Id", {| Id = int voiceId |}) |> Async.AwaitTask |> Async.Ignore
    }

    member _.GetPrintSettings() = async {
        use connection = createConnection()
        let! compositions = connection.QueryAsync<DbVoicePrintSetting>("SELECT \"key\", name FROM voice_print_setting") |> Async.AwaitTask
        return
            compositions
            |> Seq.map DbVoicePrintSetting.toDomain
            |> Seq.toList
    }
