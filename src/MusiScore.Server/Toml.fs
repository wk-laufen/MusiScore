namespace MusiScore.Server

open Tomlyn

module Toml =
    type TomlTag (key: string, value: string) =
        member val Key = key with get, set
        member val Value = value with get, set

    type TomlVoice (name: string, printConfig: string) =
        member val Name = name with get, set
        member val PrintConfig = printConfig with get, set

    type TomlComposition (title: string, tags: TomlTag array, isActive: bool, voices: TomlVoice array) =
        member val Title = title with get, set
        member val Tags = tags with get, set
        member val IsActive = isActive with get, set
        member val Voices = voices with get, set

    type TomlMetadata (composition: TomlComposition) =
        member val Composition = composition with get, set

    let getCompositionMetadata (composition: Composition) (voices: FullVoice list) =
        let model =
            TomlMetadata(
                TomlComposition(
                    composition.Title,
                    composition.Tags |> List.choose (fun v -> match v.Value with | Some value -> Some (TomlTag(v.Key, value)) | None -> None) |> Array.ofList,
                    composition.IsActive,
                    voices
                    |> List.map (fun v ->
                        TomlVoice(v.Name, v.PrintConfig)
                    )
                    |> Array.ofList
                )
            )
        Toml.FromModel(model)
