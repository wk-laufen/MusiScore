namespace MusiScore.Server

open Dapper
open Npgsql
open System

[<AutoOpen>]
module private DbModels =
    type DbActiveComposition = {
        id: int
        title: string
    }
    module DbActiveComposition =
        let toDomain v voices : ActiveComposition =
            { Id = string v.id; Title = v.title; Voices = voices }

    type DbCompositionVoice = {
        composition_id: int
        id: int
        name: string
    }

    type DbVoice = {
        id: int
        name: string
    }
    module DbVoice =
        let toDomain v : Voice =
            { Id = string v.id; Name = v.name }

    type DbPrintableVoice = {
        file: byte[]
        reorder_pages_as_booklet: bool
        cups_command_line_args: string
    }
    module DbPrintableVoice =
        let toDomain v : PrintableVoice =
            { File = v.file; PrintSettings = { ReorderPagesAsBooklet = v.reorder_pages_as_booklet; CupsCommandLineArgs = v.cups_command_line_args } }

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
        print_config_id: string
    }
    module DbFullVoice =
        let toDomain v : FullVoice =
            { Id = string v.id; Name = v.name; File = v.file; PrintConfig = v.print_config_id }
    
    type DbPrintConfig = {
        key: string
        name: string
        sort_order: int
        reorder_pages_as_booklet: bool
        cups_command_line_args: string
    }
    module DbPrintConfig =
        let toDomain v : PrintConfig =
            { Key = v.key; Name = v.name; SortOrder = v.sort_order; Settings = { ReorderPagesAsBooklet = v.reorder_pages_as_booklet; CupsCommandLineArgs = v.cups_command_line_args } }

type Db(connectionString: string) =
    let dataSource = NpgsqlDataSource.Create(connectionString)
    interface IAsyncDisposable with
        member _.DisposeAsync() = dataSource.DisposeAsync()

    member _.GetActiveCompositions() = async {
        use connection = dataSource.CreateConnection()
        let! compositions = connection.QueryAsync<DbActiveComposition>("SELECT id, title FROM composition WHERE is_active = true") |> Async.AwaitTask
        let compositionIds = [| for v in compositions -> v.id |]
        let! voices = connection.QueryAsync<DbCompositionVoice>("SELECT composition_id, id, name FROM voice WHERE composition_id = ANY (@CompositionIds)", {| CompositionIds = compositionIds |}) |> Async.AwaitTask
        let voiceLookup =
            voices
            |> Seq.groupBy _.composition_id
            |> Seq.map (fun (compositionId, voices) -> (compositionId, [ for v in voices -> DbVoice.toDomain { id = v.id; name = v.name } ]))
            |> Map.ofSeq
        return
            compositions
            |> Seq.map (fun v ->
                let voices = Map.tryFind v.id voiceLookup |> Option.defaultValue []
                DbActiveComposition.toDomain v voices
            )
            |> Seq.toList
    }

    member _.GetCompositionVoices(compositionId: string) = async {
        use connection = dataSource.CreateConnection()
        let! voices = connection.QueryAsync<DbVoice>("SELECT id, name FROM voice WHERE composition_id = @CompositionId", {| CompositionId = int compositionId |}) |> Async.AwaitTask
        return
            voices
            |> Seq.map DbVoice.toDomain
            |> Seq.toList
    }

    member _.GetPrintableVoice (_compositionId: string, voiceId: string) = async {
        use connection = dataSource.CreateConnection()
        let! voice = connection.QuerySingleAsync<DbPrintableVoice>("SELECT v.file, vpc.reorder_pages_as_booklet, vpc.cups_command_line_args FROM voice v JOIN voice_print_config vpc ON v.print_config_id = vpc.\"key\" WHERE id = @VoiceId", {| VoiceId = int voiceId |}) |> Async.AwaitTask
        return DbPrintableVoice.toDomain voice
    }

    member _.GetCompositions() = async {
        use connection = dataSource.CreateConnection()
        let! compositions = connection.QueryAsync<DbComposition>("SELECT id, title, is_active FROM composition") |> Async.AwaitTask
        return
            compositions
            |> Seq.map DbComposition.toDomain
            |> Seq.toList
    }

    member _.GetComposition (compositionId: string) = async {
        use connection = dataSource.CreateConnection()
        let! composition = connection.QuerySingleAsync<DbComposition>("SELECT id, title, is_active FROM composition WHERE id = @Id", {| Id = int compositionId |}) |> Async.AwaitTask
        return DbComposition.toDomain composition
    }

    member _.CreateComposition (newComposition: NewComposition) = async {
        use connection = dataSource.CreateConnection()
        connection.Open()
        let! compositionId = connection.ExecuteScalarAsync<int>("INSERT INTO composition (title, is_active) VALUES(@Title, @IsActive) RETURNING id", {| Title = newComposition.Title; IsActive = newComposition.IsActive |}) |> Async.AwaitTask
        return string compositionId
    }

    member _.UpdateComposition (compositionId: string) (compositionUpdate: CompositionUpdate) = async {
        use connection = dataSource.CreateConnection()
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
        do! tx.CommitAsync() |> Async.AwaitTask
        return DbComposition.toDomain composition
    }

    member _.DeleteComposition (compositionId: string) = async {
        use connection = dataSource.CreateConnection()
        do! connection.ExecuteAsync("DELETE FROM composition WHERE id = @Id", {| Id = int compositionId |}) |> Async.AwaitTask |> Async.Ignore
    }

    member _.GetFullCompositionVoices (compositionId: string) = async {
        use connection = dataSource.CreateConnection()
        let! voices = connection.QueryAsync<DbFullVoice>("SELECT id, name, file, print_config_id FROM voice WHERE composition_id = @CompositionId", {| CompositionId = int compositionId |}) |> Async.AwaitTask
        return
            voices
            |> Seq.map DbFullVoice.toDomain
            |> Seq.toList
    }

    member _.CreateVoice (compositionId: string) (createVoice: CreateVoice) = async {
        use connection = dataSource.CreateConnection()
        connection.Open()
        let command = "INSERT INTO voice (name, file, composition_id, print_config_id) VALUES(@Name, @File, @CompositionId, @PrintConfigId) RETURNING id"
        let commandArgs = {|
            Name = createVoice.Name
            File = createVoice.File
            CompositionId = int compositionId
            PrintConfigId = createVoice.PrintConfig
        |}
        let! voiceId = connection.ExecuteScalarAsync<int>(command, commandArgs) |> Async.AwaitTask
        return string voiceId
    }

    member _.UpdateVoice (_compositionId: string) (voiceId: string) (updateVoice: UpdateVoice) = async {
        use connection = dataSource.CreateConnection()
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

                match updateVoice.PrintConfig with
                | Some _ -> "print_config_id = @PrintConfigId"
                | None -> ()
            ]
            |> String.concat ", "
        if updateFields <> "" then
            let updateArgs = {|
                Id = int voiceId
                Name = updateVoice.Name |> Option.defaultValue ""
                File = updateVoice.File |> Option.defaultValue Array.empty
                PrintConfigId = updateVoice.PrintConfig |> Option.defaultValue ""
            |}
            let command = $"UPDATE voice SET %s{updateFields} WHERE id = @Id"
            do! connection.ExecuteAsync(command, updateArgs, tx) |> Async.AwaitTask |> Async.Ignore
        let! voice = connection.QuerySingleAsync<DbFullVoice>("SELECT id, name, file, print_config_id FROM voice WHERE id = @Id", {| Id = int voiceId |}, tx) |> Async.AwaitTask
        do! tx.CommitAsync() |> Async.AwaitTask
        return DbFullVoice.toDomain voice
    }

    member _.DeleteVoice (_compositionId: string) (voiceId: string) = async {
        use connection = dataSource.CreateConnection()
        do! connection.ExecuteAsync("DELETE FROM voice WHERE id = @Id", {| Id = int voiceId |}) |> Async.AwaitTask |> Async.Ignore
    }

    member _.GetPrintConfigs() = async {
        use connection = dataSource.CreateConnection()
        let! printConfigs = connection.QueryAsync<DbPrintConfig>("SELECT \"key\", name, sort_order, reorder_pages_as_booklet, cups_command_line_args FROM voice_print_config ORDER BY sort_order") |> Async.AwaitTask
        return
            printConfigs
            |> Seq.map DbPrintConfig.toDomain
            |> Seq.toList
    }

    member _.GetPrintConfig (key: string) = async {
        use connection = dataSource.CreateConnection()
        let! printConfigs = connection.QueryAsync<DbPrintConfig>("SELECT \"key\", name, sort_order, reorder_pages_as_booklet, cups_command_line_args FROM voice_print_config WHERE \"key\" = @Key", {| Key = key |}) |> Async.AwaitTask
        return printConfigs |> Seq.tryExactlyOne |> Option.map DbPrintConfig.toDomain
    }

    member _.GetDefaultPrintConfig () = async {
        use connection = dataSource.CreateConnection()
        let! printConfigs = connection.QueryAsync<DbPrintConfig>("SELECT \"key\", name, sort_order, reorder_pages_as_booklet, cups_command_line_args FROM voice_print_config ORDER BY sort_order LIMIT 1") |> Async.AwaitTask
        return printConfigs |> Seq.tryExactlyOne |> Option.map DbPrintConfig.toDomain
    }

    member _.CreatePrintConfig (printConfig: PrintConfig) = async {
        try
            use connection = dataSource.CreateConnection()
            connection.Open()
            let command = "INSERT INTO voice_print_config (key, name, sort_order, reorder_pages_as_booklet, cups_command_line_args) VALUES(@Key, @Name, @SortOrder, @ReorderPagesAsBooklet, @CupsCommandLineArgs)"
            let commandArgs = {|
                Key = printConfig.Key
                Name = printConfig.Name
                SortOrder = printConfig.SortOrder
                ReorderPagesAsBooklet = printConfig.Settings.ReorderPagesAsBooklet
                CupsCommandLineArgs = printConfig.Settings.CupsCommandLineArgs
            |}
            do! connection.ExecuteAsync(command, commandArgs) |> Async.AwaitTask |> Async.Ignore
            return Ok()
        with :? AggregateException as e ->
            e.Handle(fun inner ->
                match inner with
                | :? PostgresException as inner when inner.SqlState = PostgresErrorCodes.UniqueViolation -> true
                | _ -> false
            )
            return Error PrintConfigExists
    }

    member _.UpdatePrintConfig (key: string) (update: PrintConfigUpdate) = async {
        use connection = dataSource.CreateConnection()
        connection.Open()
        let updateFields =
            [
                match update.Name with
                | Some _ -> "name = @Name"
                | None -> ()
            ]
            |> String.concat ", "
        if updateFields <> "" then
            let updateArgs = {|
                Key = key
                Name = update.Name |> Option.defaultValue ""
            |}
            let command = $"UPDATE voice_print_config SET %s{updateFields} WHERE \"key\" = @Key"
            do! connection.ExecuteAsync(command, updateArgs) |> Async.AwaitTask |> Async.Ignore
    }
