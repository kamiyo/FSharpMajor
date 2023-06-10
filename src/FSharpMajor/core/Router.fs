module FSharpMajor.Router

open Giraffe

open FSharpMajor.API.Error
open FSharpMajor.API.System
open FSharpMajor.API.User

let apiRouter: HttpHandler =
    choose
        [ route "/ping.view" >=> pingHandler
          route "/getLicense.view" >=> licenseHandler
          route "/getUser.view" >=> userHandler ]

let rootRouter: HttpHandler =
    choose
        [ GET
          >=> setXmlType
          >=> requiresAuthentication (subsonicError ())
          >=> choose [ subRoute "/rest" apiRouter ]
          subsonicError () ]
