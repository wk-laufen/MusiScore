namespace MusiScore.Server

open Dapper
open Npgsql
open System
open System.Text.Json

[<AutoOpen>]
module private DbModels =
    type DbCompositionTagType = {
        key: string
        name: string
        settings: string
    }
    module DbCompositionTagType =
        let toDomain v : CompositionTagType =
            let settings = JsonSerializer.Deserialize<{| value_type: string; overview_display_format: {| order: int; format: string |} option |}>(v.settings)
            {
                Key = v.key
                Name = v.name
                Settings = {|
                    ValueType =
                        if settings.value_type = "text" then TagValueTypeText
                        elif settings.value_type = "multi-line-text" then TagValueTypeMultiLineText
                        else failwith $"Invalid tag type \"%s{settings.value_type}\""
                    OverviewDisplayFormat =
                        settings.overview_display_format |> Option.map (fun v -> {| Order = v.order; Format = v.format |})
                |}
            }
    type DbCompositionTag = {
        composition_id: int
        tag_type: string
        value: string
    }
    module DbCompositionTag =
        let toDomain (tagType: CompositionTagType) value otherValues : ExistingTag =
            {
                Key = tagType.Key
                Title = tagType.Name
                Settings = tagType.Settings
                Value = value
                OtherValues = otherValues
            }


    type DbActiveComposition = {
        id: int
        title: string
    }
    module DbActiveComposition =
        let toDomain v tags voices : ActiveComposition =
            {
                Id = string v.id
                Title = v.title
                Tags = tags
                Voices = voices
            }

    type DbCompositionVoice = {
        composition_id: int
        id: int
        name: string
        print_config_id: string
    }

    type DbVoice = {
        id: int
        name: string
        print_config_id: string
    }
    module DbVoice =
        let toDomain v : Voice =
            { Id = string v.id; Name = v.name; PrintConfigId = v.print_config_id }

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
        let toDomain v tags voices : Composition =
            {
                Id = string v.id
                Title = v.title
                Tags = tags
                IsActive = v.is_active
                Voices = voices
            }

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

[<AutoOpen>]
module private DbHelper =
    let (|ForeignKeyViolation|_|) (e: Exception) =
        let innerException =
            match e with
            | :? AggregateException as e -> e.InnerException
            | _ -> e
        match innerException with
        | :? PostgresException as e when e.SqlState = "23503" (* foreign_key_violation *) ->
            Some e.ConstraintName
        | _ -> None

type Db(connectionString: string) =
    let dataSource = NpgsqlDataSource.Create(connectionString)

    let getTagTypes (connection: NpgsqlConnection) = async {
        let! tagTypes = connection.QueryAsync<DbCompositionTagType>("SELECT key, name, settings FROM composition_tag_type") |> Async.AwaitTask
        return [ for v in tagTypes -> DbCompositionTagType.toDomain v ]
    }

    let getTags (connection: NpgsqlConnection) = async {
        return! connection.QueryAsync<DbCompositionTag>("SELECT composition_id, tag_type, value FROM composition_tag") |> Async.AwaitTask
    }

    let getTagLookupForCompositions (connection: NpgsqlConnection) (compositionIds: int[]) = async {
        let! tagTypes = getTagTypes connection

        let! tags = getTags connection
        let tagsLookup =
            tags
            |> Seq.map (fun v -> (v.composition_id, v.tag_type), v.value)
            |> Map.ofSeq

        return compositionIds
        |> Seq.map (fun compositionId ->
            let tags =
                tagTypes
                |> Seq.map (fun tagType ->
                    let tagValue = tagsLookup |> Map.tryFind (compositionId, tagType.Key)
                    let otherValues =
                        tags
                        |> Seq.filter (fun v -> v.tag_type = tagType.Key && v.composition_id <> compositionId)
                        |> Seq.map _.value
                        |> Seq.distinct
                        |> Seq.sort
                        |> Seq.toList
                    DbCompositionTag.toDomain tagType tagValue otherValues
                )
                |> Seq.toList
            (compositionId, tags)
        )
        |> Map.ofSeq
    }

    let getVoicesLookup (connection: NpgsqlConnection) (compositionIds: int[]) = async {
        let! voices = connection.QueryAsync<DbCompositionVoice>("SELECT composition_id, id, name, print_config_id FROM voice WHERE composition_id = ANY (@CompositionIds)", {| CompositionIds = compositionIds |}) |> Async.AwaitTask
        return
            voices
            |> Seq.groupBy _.composition_id
            |> Seq.map (fun (compositionId, voices) -> (compositionId, [ for v in voices -> DbVoice.toDomain { id = v.id; name = v.name; print_config_id = v.print_config_id } ]))
            |> Map.ofSeq
    }

    let getVoices (connection: NpgsqlConnection) (compositionId) = async {
        let! voices = connection.QueryAsync<DbVoice>("SELECT id, name, print_config_id FROM voice WHERE composition_id = @CompositionId", {| CompositionId = int compositionId |}) |> Async.AwaitTask
        return
            voices
            |> Seq.map DbVoice.toDomain
            |> Seq.toList
    }

    interface IAsyncDisposable with
        member _.DisposeAsync() = dataSource.DisposeAsync()

    member _.GetActiveCompositions() = async {
        use connection = dataSource.CreateConnection()
        let! compositions = connection.QueryAsync<DbActiveComposition>("SELECT id, title FROM composition WHERE is_active = true") |> Async.AwaitTask
        let compositionIds = [| for v in compositions -> v.id |]
        let! tagLookup = getTagLookupForCompositions connection compositionIds
        let! voiceLookup = getVoicesLookup connection compositionIds
        return
            compositions
            |> Seq.map (fun v ->
                let tags = tagLookup.[v.id]
                let voices = Map.tryFind v.id voiceLookup |> Option.defaultValue []
                DbActiveComposition.toDomain v tags voices
            )
            |> Seq.toList
    }

    member _.GetTags() = async {
        use connection = dataSource.CreateConnection()
        let! tagTypes = getTagTypes connection
        let! tags = getTags connection
        return tagTypes
            |> List.map (fun tagType ->
                let values =
                    tags
                    |> Seq.filter (fun v -> v.tag_type = tagType.Key)
                    |> Seq.map _.value
                    |> Seq.distinct
                    |> Seq.sort
                    |> Seq.toList
                DbCompositionTag.toDomain tagType None values
            )
    }

    member _.GetVoiceSortOrderPatterns() = async {
        use connection = dataSource.CreateConnection()
        let! voiceSettings = connection.QueryAsync<string>("SELECT voice_pattern FROM voice_settings ORDER BY sort_order") |> Async.AwaitTask
        return voiceSettings |> Seq.map Text.RegularExpressions.Regex |> Seq.toList
    }

    member _.UpdateVoiceSortOrderPatterns (voiceSortOrderPatterns: Text.RegularExpressions.Regex list) = async {
        use connection = dataSource.CreateConnection()
        // TODO improve
        do! connection.ExecuteAsync("DELETE FROM voice_settings") |> Async.AwaitTask |> Async.Ignore
        let voiceSettings =
            voiceSortOrderPatterns
            |> List.mapi (fun i v -> {|
                VoicePattern = $"%O{v}"
                SortOrder = i + 1
            |})
        do! connection.ExecuteAsync("INSERT INTO voice_settings (voice_pattern, sort_order) VALUES (@VoicePattern, @SortOrder)", voiceSettings) |> Async.AwaitTask |> Async.Ignore
    }

    member _.GetCompositionVoices(compositionId: string) = async {
        use connection = dataSource.CreateConnection()
        return! getVoices connection (int compositionId)
    }

    member _.GetPrintableVoice (_compositionId: string, voiceId: string) = async {
        use connection = dataSource.CreateConnection()
        let! voice = connection.QuerySingleAsync<DbPrintableVoice>("SELECT v.file, vpc.reorder_pages_as_booklet, vpc.cups_command_line_args FROM voice v JOIN voice_print_config vpc ON v.print_config_id = vpc.\"key\" WHERE id = @VoiceId", {| VoiceId = int voiceId |}) |> Async.AwaitTask
        return DbPrintableVoice.toDomain voice
    }

    member _.GetCompositions() = async {
        use connection = dataSource.CreateConnection()
        let! compositions = connection.QueryAsync<DbComposition>("SELECT id, title, is_active FROM composition") |> Async.AwaitTask
        let compositionIds = [| for v in compositions -> v.id |]
        let! tagLookup = getTagLookupForCompositions connection compositionIds
        let! voiceLookup = getVoicesLookup connection compositionIds
        return
            compositions
            |> Seq.map (fun v ->
                let tags = tagLookup.[v.id]
                let voices = Map.tryFind v.id voiceLookup |> Option.defaultValue []
                DbComposition.toDomain v tags voices
            )
            |> Seq.toList
    }

    member _.GetComposition (compositionId: string) = async {
        use connection = dataSource.CreateConnection()
        let! composition = connection.QuerySingleAsync<DbComposition>("SELECT id, title, is_active FROM composition WHERE id = @Id", {| Id = int compositionId |}) |> Async.AwaitTask
        let! tagLookup = getTagLookupForCompositions connection [| composition.id |]
        let! voices = getVoices connection composition.id
        return DbComposition.toDomain composition tagLookup.[composition.id] voices
    }

    member this.CreateComposition (newComposition: NewComposition) = async {
        use connection = dataSource.CreateConnection()
        connection.Open()
        use tx = connection.BeginTransaction()
        let data = {|
            Title = newComposition.Title
            IsActive = newComposition.IsActive
        |}
        let! compositionId = connection.ExecuteScalarAsync<int>("INSERT INTO composition (title, is_active) VALUES(@Title, @IsActive) RETURNING id", data, tx) |> Async.AwaitTask
        let! tagTypes = connection.QueryAsync<string>("SELECT key FROM composition_tag_type") |> Async.AwaitTask
        let tags =
            newComposition.Tags
            |> List.filter (fun v -> tagTypes |> Seq.contains v.Key)
            |> List.map (fun v -> {|
                CompositionId = compositionId
                TagType = v.Key
                Value = v.Value
            |})
        do! connection.ExecuteAsync("INSERT INTO composition_tag (composition_id, tag_type, value) VALUES (@CompositionId, @TagType, @Value)", tags, tx) |> Async.AwaitTask |> Async.Ignore
        do! tx.CommitAsync() |> Async.AwaitTask
        return! this.GetComposition (string compositionId) // TODO clean up
    }

    member _.UpdateComposition (compositionId: string) (compositionUpdate: CompositionUpdate) = async {
        let compositionId = int compositionId
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
                Id = compositionId
                Title = compositionUpdate.Title |> Option.defaultValue ""
                IsActive = compositionUpdate.IsActive |> Option.defaultValue false
            |}
            let command = $"UPDATE composition SET %s{updateFields} WHERE id = @Id"
            do! connection.ExecuteAsync(command, updateArgs, tx) |> Async.AwaitTask |> Async.Ignore
        
        let tagsToAdd =
            compositionUpdate.TagUpdates
            |> List.choose (function AddTag v -> Some v | RemoveTag _ -> None)
            |> List.map (fun v -> {|
                CompositionId = compositionId
                TagType = v.Key
                Value = v.Value
            |})
        if not <| List.isEmpty tagsToAdd then
            let command = "INSERT INTO composition_tag (composition_id, tag_type, value) VALUES (@CompositionId, @TagType, @Value) ON CONFLICT (composition_id, tag_type) DO UPDATE SET value = EXCLUDED.value"
            do! connection.ExecuteAsync(command, tagsToAdd, tx) |> Async.AwaitTask |> Async.Ignore
        
        let tagsToRemove =
            compositionUpdate.TagUpdates
            |> List.choose (function RemoveTag v -> Some {| CompositionId = compositionId; TagType = v; |} | AddTag _ -> None)
        if not <| List.isEmpty tagsToRemove then
            let command = $"DELETE FROM composition_tag WHERE composition_id = @CompositionId AND tag_type = @TagType"
            do! connection.ExecuteAsync(command, tagsToRemove, tx) |> Async.AwaitTask |> Async.Ignore

        let! composition = connection.QuerySingleAsync<DbComposition>("SELECT id, title, is_active FROM composition WHERE id = @Id", {| Id = compositionId |}, tx) |> Async.AwaitTask
        let! tagLookup = getTagLookupForCompositions connection [| composition.id |]
        let! voices = getVoices connection composition.id
        do! tx.CommitAsync() |> Async.AwaitTask
        return DbComposition.toDomain composition tagLookup.[composition.id] voices
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

    member _.GetOtherVoiceNames (excludeCompositionIds: string list) = async {
        use connection = dataSource.CreateConnection()
        let compositionIds = excludeCompositionIds |> List.map int |> List.toArray
        let! voiceNames = connection.QueryAsync<string>("SELECT DISTINCT name FROM voice WHERE NOT (composition_id = ANY(@CompositionIds))", {| CompositionIds = compositionIds |}) |> Async.AwaitTask
        return Seq.toList voiceNames
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

                match update.ReorderPagesAsBooklet with
                | Some _ -> "reorder_pages_as_booklet = @ReorderPagesAsBooklet"
                | None -> ()

                match update.CupsCommandLineArgs with
                | Some _ -> "cups_command_line_args = @CupsCommandLineArgs"
                | None -> ()

                match update.SortOrder with
                | Some _ -> "sort_order = @SortOrder"
                | None -> ()
            ]
            |> String.concat ", "
        if updateFields <> "" then
            let updateArgs = {|
                Key = key
                Name = update.Name |> Option.defaultValue ""
                ReorderPagesAsBooklet = update.ReorderPagesAsBooklet |> Option.defaultValue false
                CupsCommandLineArgs = update.CupsCommandLineArgs |> Option.defaultValue ""
                SortOrder = update.SortOrder |> Option.defaultValue 0
            |}
            let command = $"UPDATE voice_print_config SET %s{updateFields} WHERE \"key\" = @Key"
            do! connection.ExecuteAsync(command, updateArgs) |> Async.AwaitTask |> Async.Ignore
    }

    member _.DeletePrintConfig (key: string) (replacementConfigId: string) = async {
        use connection = dataSource.CreateConnection()
        connection.Open()
        use tx = connection.BeginTransaction()
        try
            do! connection.ExecuteAsync("UPDATE voice SET print_config_id = @NewConfigId WHERE print_config_id = @OldConfigId", {| OldConfigId = key; NewConfigId = replacementConfigId |}, tx) |> Async.AwaitTask |> Async.Ignore
            do! connection.ExecuteAsync("DELETE FROM voice_print_config WHERE \"key\" = @Key", {| Key = key |}, tx) |> Async.AwaitTask |> Async.Ignore
            do! tx.CommitAsync() |> Async.AwaitTask
            return Ok ()
        with
        | ForeignKeyViolation "voice_print_setting_id_fkey" ->
            do! tx.RollbackAsync() |> Async.AwaitTask
            return Error InvalidReplacementConfigId
    }
