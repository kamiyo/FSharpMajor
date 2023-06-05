module Router

open Giraffe

open API.System

let apiRouter : HttpHandler =
    choose [
        route "/ping.view" >=> pingHandler
        route "/getLicense.view" >=> licenseHandler
    ]

let rootRouter : HttpHandler =
    choose [
        GET >=> choose [
            subRoute "/rest" (warbler (fun _ -> apiRouter))
        ]
        RequestErrors.NOT_FOUND "Not Found"
    ]
