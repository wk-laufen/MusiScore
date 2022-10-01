module MusiScore.Server.Test.PDF

open Expecto
open MusiScore.Server

[<Tests>]
let tests =
    testList "getBookletPageOrder" [
        testCase "works for 4 pages" <| fun _ ->
            let expected = [Some 4; Some 1; Some 2; Some 3]
            let actual = PDF.getBookletPageOrder 4
            Expect.equal actual expected "Page order should match"
        testCase "works for 8 pages" <| fun _ ->
            let expected = [Some 8; Some 1; Some 2; Some 7; Some 6; Some 3; Some 4; Some 5]
            let actual = PDF.getBookletPageOrder 8
            Expect.equal actual expected "Page order should match"
        testCase "works for 6 pages" <| fun _ ->
            let expected = [None; Some 1; Some 2; None; Some 6; Some 3; Some 4; Some 5]
            let actual = PDF.getBookletPageOrder 6
            Expect.equal actual expected "Page order should match"
    ]
