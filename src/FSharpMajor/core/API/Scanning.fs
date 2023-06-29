module FSharpMajor.API.Scanning

open Giraffe
open Microsoft.AspNetCore.Http

open FSharpMajor.Scanner

let updateScanHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let scanTask = scanForUpdates ()
        text "Update scan started" earlyReturn ctx
