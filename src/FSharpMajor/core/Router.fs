module FSharpMajor.Router

open Giraffe

open FSharpMajor.API.System
open FSharpMajor.Authorization

let apiRouter: HttpHandler =
    choose
        [ route "/ping.view" >=> pingHandler
          route "/getLicense.view" >=> licenseHandler ]

let rootRouter: HttpHandler =
    choose
        [ GET
          >=> setXmlType
          >=> requiresAuthentication subsonicError
          >=> choose [ subRoute "/rest" apiRouter ]
          RequestErrors.NOT_FOUND "Not Found" ]
