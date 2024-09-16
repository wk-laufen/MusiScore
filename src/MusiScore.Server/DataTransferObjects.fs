namespace MusiScore.Shared.DataTransfer

module Print =
    type ActiveCompositionDto = {
        Title: string
        Composer: string option
        Arranger: string option
        Voices: {| Name: string; PrintUrl: string |} list
    }

    type VoiceDto = {
        Name: string
        PrintUrl: string
    }

module Admin =
    type ExistingCompositionDto = {
        Title: string
        Composer: string option
        Arranger: string option
        IsActive: bool
        Links: {|
            Self: string
            Voices: string
        |}
    }

    type CompositionListDto = {
        Compositions: ExistingCompositionDto array
        Links: {|
            PrintConfigs: string
            InferPrintConfig: string
            TestPrintConfig: string
            Composition: string
            Export: string
        |}
    }

    type PrintConfigDto = {
        Key: string
        Name: string
        SortOrder: int
        ReorderPagesAsBooklet: bool
        CupsCommandLineArgs: string
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
    }

    type NewCompositionDto = {
        Title: string
        Composer: string option
        Arranger: string option
        IsActive: bool option
    }

    type CompositionUpdateDto = {
        Title: string option
        UpdateComposer: bool option
        Composer: string option
        UpdateArranger: bool option
        Arranger: string option
        IsActive: bool option
    }

    type CreateVoiceDto = {
        Name: string
        File: byte[]
        PrintConfig: string
    }

    type ExistingVoiceDto = {
        Name: string
        File: byte[]
        PrintConfig: string
        Links: {| Self: string |}
    }

    type FullCompositionDto = {
        Title: string
        Composer: string option
        Arranger: string option
        IsActive: bool
        Links: {| Self: string; Voice: string |}
        Voices: ExistingVoiceDto array
    }

    type UpdateVoiceDto = {
        Name: string option
        File: byte[] option
        PrintConfig: string option
    }
