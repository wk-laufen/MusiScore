namespace MusiScore.Client

type Deferred<'success, 'error> =
    | Loading
    | Loaded of 'success
    | LoadFailed of 'error

module Deferred =
    let map fn = function
        | Loaded v -> Loaded (fn v)
        | Loading
        | LoadFailed _ as v -> v
