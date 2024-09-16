namespace MusiScore.Server

open Tomlyn

module Toml =
    type TomlVoice (name: string, printConfig: string) =
        member val Name = name with get, set
        member val PrintConfig = printConfig with get, set

    type TomlComposition (title: string, composer: string, arranger: string, isActive: bool, voices: TomlVoice array) =
        member val Title = title with get, set
        member val Composer = composer with get, set
        member val Arranger = arranger with get, set
        member val IsActive = isActive with get, set
        member val Voices = voices with get, set

    type TomlMetadata (composition: TomlComposition) =
        member val Composition = composition with get, set

    let getCompositionMetadata (composition: Composition) (voices: FullVoice list) =
        let model =
            TomlMetadata(
                TomlComposition(
                    composition.Title,
                    composition.Composer |> Option.toObj,
                    composition.Arranger |> Option.toObj,
                    composition.IsActive,
                    voices
                    |> List.map (fun v ->
                        TomlVoice(v.Name, v.PrintConfig)
                    )
                    |> Array.ofList
                )
            )
        Toml.FromModel(model)
