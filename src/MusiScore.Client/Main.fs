module MusiScore.Client.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client

/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/counter">] Counter
    | [<EndPoint "/data">] Data

/// The Elmish application's model.
type Model =
    {
        page: Page
        counter: int
        books: Book[] option
        error: string option
        username: string
        password: string
        signedInAs: option<string>
        signInFailed: bool
    }

and Book =
    {
        title: string
        author: string
        publishDate: DateTime
        isbn: string
    }

let initModel =
    {
        page = Home
        counter = 0
        books = None
        error = None
        username = ""
        password = ""
        signedInAs = None
        signInFailed = false
    }

/// Remote service definition.
type BookService =
    {
        /// Get the list of all books in the collection.
        getBooks: unit -> Async<Book[]>

        /// Add a book in the collection.
        addBook: Book -> Async<unit>

        /// Remove a book from the collection, identified by its ISBN.
        removeBookByIsbn: string -> Async<unit>

        /// Sign into the application.
        signIn : string * string -> Async<option<string>>

        /// Get the user's name, or None if they are not authenticated.
        getUsername : unit -> Async<string>

        /// Sign out from the application.
        signOut : unit -> Async<unit>
    }

    interface IRemoteService with
        member this.BasePath = "/books"

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | Increment
    | Decrement
    | SetCounter of int
    | GetBooks
    | GotBooks of Book[]
    | SetUsername of string
    | SetPassword of string
    | GetSignedInAs
    | RecvSignedInAs of option<string>
    | SendSignIn
    | RecvSignIn of option<string>
    | SendSignOut
    | RecvSignOut
    | Error of exn
    | ClearError

let update remote message model =
    let onSignIn = function
        | Some _ -> Cmd.ofMsg GetBooks
        | None -> Cmd.none
    match message with
    | SetPage page ->
        { model with page = page }, Cmd.none

    | Increment ->
        { model with counter = model.counter + 1 }, Cmd.none
    | Decrement ->
        { model with counter = model.counter - 1 }, Cmd.none
    | SetCounter value ->
        { model with counter = value }, Cmd.none

    | GetBooks ->
        let cmd = Cmd.OfAsync.either remote.getBooks () GotBooks Error
        { model with books = None }, cmd
    | GotBooks books ->
        { model with books = Some books }, Cmd.none

    | SetUsername s ->
        { model with username = s }, Cmd.none
    | SetPassword s ->
        { model with password = s }, Cmd.none
    | GetSignedInAs ->
        model, Cmd.OfAuthorized.either remote.getUsername () RecvSignedInAs Error
    | RecvSignedInAs username ->
        { model with signedInAs = username }, onSignIn username
    | SendSignIn ->
        model, Cmd.OfAsync.either remote.signIn (model.username, model.password) RecvSignIn Error
    | RecvSignIn username ->
        { model with signedInAs = username; signInFailed = Option.isNone username }, onSignIn username
    | SendSignOut ->
        model, Cmd.OfAsync.either remote.signOut () (fun () -> RecvSignOut) Error
    | RecvSignOut ->
        { model with signedInAs = None; signInFailed = false }, Cmd.none

    | Error RemoteUnauthorizedException ->
        { model with error = Some "You have been logged out."; signedInAs = None }, Cmd.none
    | Error exn ->
        { model with error = Some exn.Message }, Cmd.none
    | ClearError ->
        { model with error = None }, Cmd.none

/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

let homePage model dispatch =
    div {
        attr.``class`` "content"
        h1 { attr.``class`` "title"; "Welcome to Bolero!" }
        p { "This application demonstrates Bolero's major features." }
        ul {
            li {
                "The entire application is driven by "
                a {
                    attr.target "_blank"
                    attr.href "https://fsbolero.github.io/docs/Elmish"
                    "Elmish"
                }
                "."
            }
            li {
                "The menu on the left switches pages based on "
                a {
                    attr.target "_blank"
                    attr.href "https://fsbolero.github.io/docs/Routing"
                    "routes"
                }
                "."
            }
            li {
                "The "
                a { router.HRef Counter; "Counter" }
                " page demonstrates event handlers and data binding in "
                a {
                    attr.target "_blank"
                    attr.href "https://fsbolero.github.io/docs/Templating"
                    "HTML templates"
                }
                "."
            }
            li {
                "The "
                a { router.HRef Data; "Download data" }
                " page demonstrates the use of "
                a {
                    attr.target "_blank"
                    attr.href "https://fsbolero.github.io/docs/Remoting"
                    "remote functions"
                }
                "."
            }
            p { "Enjoy writing awesome apps!" }
        }
    }

let counterPage model dispatch =
    concat {
        h1 { attr.``class`` "title"; "A simple counter" }
        p {
            button {
                on.click (fun _ -> dispatch Decrement)
                attr.``class`` "button"
                "-"
            }
            input {
                attr.``type`` "number"
                attr.id "counter"
                attr.``class`` "input"
                bind.input.int model.counter (fun v -> dispatch (SetCounter v))
            }
            button {
                on.click (fun _ -> dispatch Increment)
                attr.``class`` "button"
                "+"
            }
        }
    }

let dataPage model (username: string) dispatch =
    concat {
        h1 {
            attr.``class`` "title"
            "Download data "
            button {
                attr.``class`` "button"
                on.click (fun _ -> dispatch GetBooks)
                "Reload"
            }
        }
        p {
            $"Signed in as {username}. "
            button {
                attr.``class`` "button"
                on.click (fun _ -> dispatch SendSignOut)
                "Sign out"
            }
        }
        table {
            attr.``class`` "table is-fullwidth"
            thead {
                tr {
                    th { "Title" }
                    th { "Author" }
                    th { "Published" }
                    th { "ISBN" }
                }
            }
            tbody {
                cond model.books <| function
                | None ->
                    tr {
                        td { attr.colspan 4; "Downloading book list..." }
                    }
                | Some books ->
                    forEach books <| fun book ->
                        tr {
                            td { book.title }
                            td { book.author }
                            td { book.publishDate.ToString("yyyy-MM-dd") }
                            td { book.isbn }
                        }
            }
        }
    }

let errorNotification errorText closeCallback =
    div {
        attr.``class`` "notification is-warning"
        cond closeCallback <| function
        | None -> empty()
        | Some closeCallback -> button { attr.``class`` "delete"; on.click closeCallback }
        text errorText
    }

let field (content: Node) = div { attr.``class`` "field"; content }
let control (content: Node) = div { attr.``class`` "control"; content }

let inputField (fieldLabel: string) (inputAttrs: Attr) =
    field (concat {
        label { attr.``class`` "label"; fieldLabel }
        control (input { attr.``class`` "input"; inputAttrs })
    })

let signInPage model dispatch =
    concat {
        h1 { attr.``class`` "title"; "Sign in" }
        form {
            on.submit (fun _ -> dispatch SendSignIn)
            inputField "Username" (
                bind.input.string model.username (fun s -> dispatch (SetUsername s))
            )
            inputField "Password" (attrs {
                attr.``type`` "password"
                bind.input.string model.password (fun s -> dispatch (SetPassword s))
            })
            field (
                control (
                    input { attr.``type`` "submit"; attr.value "Sign in" }
                )
            )
            cond model.signInFailed <| function
            | false -> empty()
            | true -> errorNotification "Sign in failed. Use any username and the password \"password\"." None
        }
    }

let menuItem (model: Model) (page: Page) (itemText: string) =
    li {
        a {
            attr.``class`` (if model.page = page then "is-active" else "")
            router.HRef page
            itemText
        }
    }

let view model dispatch =
    div {
        attr.``class`` "columns"
        aside {
            attr.``class`` "column sidebar is-narrow"
            section {
                attr.``class`` "section"
                nav {
                    attr.``class`` "menu"
                    ul {
                        attr.``class`` "menu-list"
                        menuItem model Home "Home"
                        menuItem model Counter "Counter"
                        menuItem model Data "Download data"
                    }
                }
            }
        }
        div {
            attr.``class`` "column"
            section {
                attr.``class`` "section"
                cond model.page <| function
                | Home -> homePage model dispatch
                | Counter -> counterPage model dispatch
                | Data ->
                    cond model.signedInAs <| function
                    | Some username -> dataPage model username dispatch
                    | None -> signInPage model dispatch
                div {
                    attr.id "notification-area"
                    cond model.error <| function
                    | None -> empty()
                    | Some err -> errorNotification err (Some (fun _ -> dispatch ClearError))
                }
            }
        }
    }

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let bookService = this.Remote<BookService>()
        let update = update bookService
        Program.mkProgram (fun _ -> initModel, Cmd.ofMsg GetSignedInAs) update view
        |> Program.withRouter router
