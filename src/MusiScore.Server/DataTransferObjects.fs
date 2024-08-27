namespace MusiScore.Shared.DataTransfer

module Print =
    type ActiveCompositionDto = {
        Title: string
        Voices: {| Name: string; PrintUrl: string |} list
    }

    type VoiceDto = {
        Name: string
        PrintUrl: string
    }

module Admin =
    type ExistingCompositionDto = {
        Title: string
        IsActive: bool
        Links: {|
            Self: string
            Voices: string
        |}
    }

    type CompositionListDto = {
        Compositions: ExistingCompositionDto array
        Links: {|
            PrintSettings: string
            TestPrintSetting: string
            Composition: string
            Export: string
        |}
    }

    type VoicePrintSettingDto = {
        Key: string
        Name: string
    }

    type NewCompositionDto = {
        Title: string
    }

    type CompositionUpdateDto = {
        Title: string option
        IsActive: bool option
    }

    type CreateVoiceDto = {
        Name: string
        File: byte[]
        PrintSetting: string
    }

    type ExistingVoiceDto = {
        Name: string
        File: byte[]
        PrintSetting: string
        Links: {| Self: string |}
    }

    type FullCompositionDto = {
        Title: string
        IsActive: bool
        Links: {| Self: string; Voice: string |}
        Voices: ExistingVoiceDto array
    }

    type UpdateVoiceDto = {
        Name: string option
        File: byte[] option
        PrintSetting: string option
    }
