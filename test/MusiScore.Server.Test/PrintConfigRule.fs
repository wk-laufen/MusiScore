module MusiScore.Server.Test.PrintConfigRule

open Expecto
open MusiScore.Server

// in points
let private a4Portrait = (595.276, 841.89)
let private a4Landscape = (841.89, 595.276)
let private a3Portrait = (841.89, 1190.55)

let private a4 = { WidthMm = 210; HeightMm = 297 }
let private a3 = { WidthMm = 297; HeightMm = 420 }

let private anyRule printConfigKey =
    { MinPageCount = 1; MaxPageCount = None; PageSize = None; Orientation = None; PrintConfigKey = printConfigKey }

[<Tests>]
let tests =
    testList "PrintConfigRule" [
        testCase "matches page size with tolerance" <| fun _ ->
            Expect.isTrue (PrintConfigRule.matchesPageSize a4 a4Portrait) "A4 portrait"
            Expect.isTrue (PrintConfigRule.matchesPageSize a4 a4Landscape) "A4 landscape"
            Expect.isTrue (PrintConfigRule.matchesPageSize { WidthMm = 297; HeightMm = 210 } a4Portrait) "page size is independent of orientation"
            Expect.isTrue (PrintConfigRule.matchesPageSize a4 (600., 835.)) "A4 with slight deviation"
            Expect.isFalse (PrintConfigRule.matchesPageSize a4 (612., 792.)) "Letter is not A4"
            Expect.isFalse (PrintConfigRule.matchesPageSize a4 a3Portrait) "A3 is not A4"

        testCase "rule without conditions matches everything" <| fun _ ->
            Expect.isTrue (PrintConfigRule.matches [ a4Portrait; a3Portrait ] (anyRule "x")) "should match"

        testCase "page count range is inclusive" <| fun _ ->
            let rule = { anyRule "x" with MinPageCount = 2; MaxPageCount = Some 3 }
            Expect.isFalse (PrintConfigRule.matches (List.replicate 1 a4Portrait) rule) "1 page"
            Expect.isTrue (PrintConfigRule.matches (List.replicate 2 a4Portrait) rule) "2 pages"
            Expect.isTrue (PrintConfigRule.matches (List.replicate 3 a4Portrait) rule) "3 pages"
            Expect.isFalse (PrintConfigRule.matches (List.replicate 4 a4Portrait) rule) "4 pages"

        testCase "page size and orientation must match on every page" <| fun _ ->
            let rule = { anyRule "x" with PageSize = Some a4; Orientation = Some Portrait }
            Expect.isTrue (PrintConfigRule.matches [ a4Portrait; a4Portrait ] rule) "all A4 portrait"
            Expect.isFalse (PrintConfigRule.matches [ a4Portrait; a4Landscape ] rule) "mixed orientation"
            Expect.isFalse (PrintConfigRule.matches [ a4Portrait; a3Portrait ] rule) "mixed page size"

        testCase "first matching rule wins" <| fun _ ->
            let rules = [
                { anyRule "single" with MaxPageCount = Some 1 }
                { anyRule "landscape" with Orientation = Some Landscape }
                anyRule "fallback"
            ]
            Expect.equal (PrintConfigRule.tryFindPrintConfigKey [ a4Landscape ] rules) (Some "single") "single landscape page"
            Expect.equal (PrintConfigRule.tryFindPrintConfigKey [ a4Landscape; a4Landscape ] rules) (Some "landscape") "two landscape pages"
            Expect.equal (PrintConfigRule.tryFindPrintConfigKey [ a4Portrait; a4Portrait ] rules) (Some "fallback") "two portrait pages"

        testCase "no rule matches" <| fun _ ->
            let rules = [ { anyRule "x" with PageSize = Some a3 } ]
            Expect.equal (PrintConfigRule.tryFindPrintConfigKey [ a4Portrait ] rules) None "no match"
    ]
