module FSharpMajor.API.Scanning

open Giraffe
open Microsoft.AspNetCore.Http

open FSharpMajor.Scanner

let updateScanHandler: HttpHandler =
    fun (_next: HttpFunc) (ctx: HttpContext) ->
        let _scanTask = scanForUpdates ()
        text "Update scan started" earlyReturn ctx
