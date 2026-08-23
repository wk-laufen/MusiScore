namespace MusiScore.Shared.DataTransfer

module Print =
    type ExistingTag = {
        Key: string
        Title: string
        Settings: {|
            OverviewDisplayFormat: {| Order: int; Format: string |} option
        |}
        Value: string option
    }

    type ActiveCompositionDto = {
        Title: string
        Tags: ExistingTag list
        Voices: {| Name: string; GlobalSortOrder: int; PrintUrl: string |} list
    }

    type VoiceDto = {
        Name: string
        PrintUrl: string
    }

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
        Title: string
        Tags: ExistingTag list
        IsActive: bool
        Links: {|
            Self: string
            Voices: string
            Print: string
        |}
    }

    type CompositionListDto = {
        Compositions: ExistingCompositionDto array
        Links: {|
            PrintConfigs: string
            InferPrintConfig: string
            TestPrintConfig: string
            Composition: string
            CompositionTemplate: string
            Export: string
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

    type NewTag = {
        Key: string
        Value: string
    }

    type NewCompositionDto = {
        Title: string
        Tags: NewTag list
        IsActive: bool option
    }

    type CompositionUpdateDto = {
        Title: string option
        TagsToRemove: string list option
        TagsToAdd: NewTag list option
        IsActive: bool option
    }

    type CreateVoiceDto = {
        Name: string
        File: byte[]
        PrintConfig: string
    }

    type ExistingVoiceDto = {
        Name: string
        PrintConfig: string
        Links: {| Self: string; Sheet: string |}
    }

    type CompositionTemplateDto = {
        Title: string
        Tags: ExistingTag list
        IsActive: bool
        Voices: ExistingVoiceDto array
    }

    type FullCompositionDto = {
        Title: string
        Tags: ExistingTag list
        IsActive: bool
        Links: {| Self: string; Voices: string; Print: string |}
        Voices: ExistingVoiceDto array
    }

    type UpdateVoiceDto = {
        Name: string option
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
