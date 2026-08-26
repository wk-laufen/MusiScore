namespace MusiScore.Server

open System
open System.Collections.Concurrent
open System.Security.Cryptography

/// Short-lived tokens that let the browser download a file by navigating to its URL, which it can't do
/// with the `Authorization` header. A token stays valid for a few minutes so that a failed download can
/// be retried, and it only ever grants access to the endpoint it was created for.
type DownloadTokenStore() =
    let lifetime = TimeSpan.FromMinutes 5.
    let expiryByToken = ConcurrentDictionary<string, DateTimeOffset>()

    let removeExpiredTokens () =
        for entry in expiryByToken do
            if entry.Value <= DateTimeOffset.UtcNow then
                expiryByToken.TryRemove(entry) |> ignore

    member _.Create() =
        removeExpiredTokens ()
        let token = RandomNumberGenerator.GetBytes 32 |> Convert.ToHexString
        expiryByToken.[token] <- DateTimeOffset.UtcNow.Add(lifetime)
        token

    member _.IsValid(token: string) =
        not (String.IsNullOrEmpty token) &&
            match expiryByToken.TryGetValue token with
            | true, expiry -> expiry > DateTimeOffset.UtcNow
            | _ -> false
