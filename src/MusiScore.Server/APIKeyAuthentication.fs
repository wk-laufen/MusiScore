namespace MusiScore.Server

open Microsoft.AspNetCore.Authentication
open System.Threading.Tasks
open System.Security.Claims

type APIKeyAuthenticationSchemeOptions() =
    inherit AuthenticationSchemeOptions()

    member val APIKeys : (string * string list) list = [] with get, set

type APIKeyAuthenticationHandler(options, logger, encoder) =
    inherit AuthenticationHandler<APIKeyAuthenticationSchemeOptions>(options, logger, encoder)

    override this.HandleAuthenticateAsync () : Task<AuthenticateResult> =
        let authHeader = this.Request.Headers.Authorization.ToString()
        let claims =
            this.Options.APIKeys
            |> List.filter (fst >> fun apiKey -> authHeader = $"APIKey {apiKey}")
            |> List.collect snd
            |> List.map (fun role -> Claim(ClaimTypes.Role, role))
        match claims with
        | [] -> Task.FromResult(AuthenticateResult.NoResult())
        | claims -> Task.FromResult(AuthenticateResult.Success(AuthenticationTicket(ClaimsPrincipal(ClaimsIdentity(claims, "APIKey")), "APIKey")))