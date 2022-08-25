[<AutoOpen>]
module MusiScore.Client.View

open Bolero
open Bolero.Html

let view (router: Router<_, _, _>) model dispatch =
    match model with
    | (Print, PrintModel subModel) -> Print.View.view subModel (PrintMessage >> dispatch)
    | (Print, _) -> empty ()
    | (Admin, AdminModel subModel) -> Admin.View.view subModel (AdminMessage >> dispatch)
    | (Admin, _) -> empty ()
