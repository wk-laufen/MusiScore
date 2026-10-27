namespace MusiScore.Shared.DataTransfer

module Admin =
    type ExistingTag = {
        Key: string
        Title: string
        Settings: {|
            ValueType: string
            OverviewDisplayFormat: {| Order: int; Format: string |} option
        |}
        Value: string option
        OtherValues: string list
    }

    type ExistingCompositionDto = {
        Id: string
        Title: string
        Tags: ExistingTag list
        EventIds: string list
        Links: {|
            Self: string
            Voices: string
            Print: string
        |}
    }

    type EventDto = {
        Id: string
        Name: string
        IsActive: bool
        Links: {|
            Self: string
            // append a composition id to add or remove a single composition
            Compositions: string
        |}
    }

    type NewEventDto = {
        Name: string
        IsActive: bool option
        CompositionIds: string list option
    }

    type EventUpdateDto = {
        Name: string option
        IsActive: bool option
        /// replaces all compositions of the event
        CompositionIds: string list option
    }

    type CompositionListDto = {
        Compositions: ExistingCompositionDto array
        /// sorted by name, newest first
        Events: EventDto list
        Links: {|
            Events: string
            PrintConfigs: string
            PrintConfigRules: string
            InferPrintConfig: string
            TestPrintConfig: string
            Composition: string
            CompositionTemplate: string
            ExportToken: string
            VoiceDefinitions: string
            VoiceDefinitionGroups: string
        |}
    }

    type CompositionVoicesDto = {
        Title: string
        Voices: string list
    }

    type PrintConfigDto = {
        Key: string
        Name: string
        SortOrder: int
        ReorderPagesAsBooklet: bool
        CupsCommandLineArgs: string
        /// compositions using this print config, most voices first
        Compositions: CompositionVoicesDto list
        Links: {|
            Self: string
        |}
    }

    type NewPrintConfigDto = {
        Key: string
        Name: string
        SortOrder: int
        ReorderPagesAsBooklet: bool
        CupsCommandLineArgs: string
    }

    type PrintConfigUpdateDto = {
        Name: string option
        ReorderPagesAsBooklet: bool option
        CupsCommandLineArgs: string option
        SortOrder: int option
    }

    type PrintConfigDeleteDto = {
        ReplacementConfigId: string
    }

    type PrintConfigRuleDto = {
        MinPageCount: int
        MaxPageCount: int option
        /// either both or none of page width and height are set
        PageWidthMm: int option
        PageHeightMm: int option
        /// "portrait" or "landscape"
        Orientation: string option
        PrintConfig: string
    }

    type NewTag = {
        Key: string
        Value: string
    }

    type NewCompositionDto = {
        Title: string
        Tags: NewTag list
    }

    type CompositionUpdateDto = {
        Title: string option
        TagsToRemove: string list option
        TagsToAdd: NewTag list option
    }

    type CreateVoiceDto = {
        Names: string list
        File: byte[]
        PrintConfig: string
    }

    type ExistingVoiceDto = {
        Names: string list
        PrintConfig: string
        Links: {| Self: string; Sheet: string |}
    }

    type CompositionTemplateDto = {
        Title: string
        Tags: ExistingTag list
        Voices: ExistingVoiceDto array
    }

    type FullCompositionDto = {
        Title: string
        Tags: ExistingTag list
        Links: {| Self: string; Voices: string; Print: string |}
        Voices: ExistingVoiceDto array
    }

    type UpdateVoiceDto = {
        Names: string list option
        File: byte[] option
        PrintConfig: string option
    }

    type CreateVoiceDefinitionDto = {
        Name: string
        SortOrder: int
        GroupId: string option
        MemberCount: int
    }

    /// Wrapper that distinguishes "don't touch the group" (the whole field is missing)
    /// from "remove the voice definition from its group" (`Id` is `null`).
    type VoiceDefinitionGroupReferenceDto = {
        Id: string option
    }

    type UpdateVoiceDefinitionDto = {
        Name: string option
        SortOrder: int option
        Group: VoiceDefinitionGroupReferenceDto option
        MemberCount: int option
    }

    type VoiceDefinitionDeleteDto = {
        ReplacementVoiceDefinitionId: string option
    }

    type CreateVoiceDefinitionGroupDto = {
        Name: string
        SortOrder: int
    }

    type UpdateVoiceDefinitionGroupDto = {
        Name: string option
        SortOrder: int option
    }

    type VoicePrintSettingsDto = {
        Name: string
        Count: int
    }

    type VoicePrintResultDto = {
        VoiceName: string
        Result: string
    }
