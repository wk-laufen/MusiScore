namespace MusiScore.Client

open Bolero
open Elmish

type Page =
    | [<EndPoint "/">] Print
    | [<EndPoint "/admin">] Admin

type PageModel =
    | PrintModel of Print.Model
    | AdminModel of Admin.Model

type Model = Page * PageModel

type Message =
    | SetPage of Page
    | PrintMessage of Print.Message
    | AdminMessage of Admin.Message

module Model =
    let init =
        let model, cmd = Print.Model.init
        (Print, PrintModel model), Cmd.map PrintMessage cmd
