namespace MusiScore.Shared.DataTransfer

module Print =
    type ActiveCompositionDto = {
        Title: string
        ShowVoicesUrl: string
    }

    type CompositionDto = {
        Title: string
        IsActive: bool
        ShowVoicesUrl: string
    }

    type VoiceDto = {
        Name: string
        PrintUrl: string
    }

module Admin =
    type ExistingCompositionDto = {
        Title: string
        IsActive: bool
        UpdateUrl: string
        DeleteUrl: string
        GetVoicesUrl: string
        CreateVoiceUrl: string
    }

    type CompositionListDto = {
        Compositions: ExistingCompositionDto array
        GetPrintSettingsUrl: string
        CreateCompositionUrl: string
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
        UpdateUrl: string
        DeleteUrl: string
    }

    type UpdateVoiceDto = {
        Name: string option
        File: byte[] option
        PrintSetting: string option
    }
