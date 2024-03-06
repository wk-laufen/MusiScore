namespace MusiScore.Server

open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Filters
open System

type FaultInjectionFilter(?probability: double) =
    let probability = defaultArg probability 0.1

    interface IActionFilter with
        member _.OnActionExecuting (context: ActionExecutingContext) =
            if Random.Shared.NextDouble() < probability then context.Result <- new StatusCodeResult(503)

        member _.OnActionExecuted(context: ActionExecutedContext) = ()
