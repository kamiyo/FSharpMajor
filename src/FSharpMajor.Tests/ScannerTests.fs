module FSharpMajor.Tests.ScannerTests

open Xunit
open FSharpMajor.Scanner
open FsUnitTyped.TopLevelOperators
open Xunit.Abstractions

type ScannerTest(output: ITestOutputHelper) =

    [<Fact>]
    member _.``MimeType from name``() =
        let path = "video.mp4"

        let mime = getMimeType path

        mime |> shouldEqual "video/mp4"

    [<Fact>]
    member _.``MimeType from just ext``() =
        let path = ".mp3"

        let mime = getMimeType path

        mime |> shouldEqual "audio/mpeg"

    [<Fact>]
    member _.``MimeType of flac``() =
        let path = "audio.flac"

        let mime = getMimeType path

        mime |> shouldEqual "audio/flac"

    [<Fact>]
    member _.``MimeType of aac``() =
        let path = "audio.aac"

        let mime = getMimeType path

        mime |> shouldEqual "audio/aac"

    [<Fact>]
    member _.``MimeType of m4a``() =
        let path = "audio.m4a"

        let mime = getMimeType path

        mime |> shouldEqual "audio/mp4"

    [<Fact>]
    member _.``MimeType of jpg``() =
        let path = "image.jpg"

        let mime = getMimeType path

        mime |> shouldEqual "image/jpeg"

    [<Fact>]
    member _.``MimeType of png``() =
        let path = "image.png"

        let mime = getMimeType path

        mime |> shouldEqual "image/png"

    member private _.output: ITestOutputHelper = output
