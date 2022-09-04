[<AutoOpen>]
module MusiScore.Client.Update

open Elmish

let update httpClient js message model =
    match message, model with
    | SetPage Print, (_, pageModel) ->
        let subModel, subCommand = Print.Model.init
        (Print, PrintModel subModel), Cmd.map PrintMessage subCommand
    | SetPage Admin, (_, pageModel) ->
        let subModel, subCommand = Admin.Model.init
        (Admin, AdminModel subModel), Cmd.map AdminMessage subCommand
    | PrintMessage subMessage, (Print, PrintModel subModel) ->
        let (subModel', subMessage') = Print.Update.update httpClient subMessage subModel
        (Print, PrintModel subModel'), Cmd.map PrintMessage subMessage'
    | PrintMessage _, _ -> model, Cmd.none
    | AdminMessage subMessage, (Admin, AdminModel subModel) ->
        let (subModel', subMessage') = Admin.Update.update httpClient js subMessage subModel
        (Admin, AdminModel subModel'), Cmd.map AdminMessage subMessage'
    | AdminMessage _, _ -> model, Cmd.none
