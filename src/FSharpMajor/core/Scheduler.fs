module FSharpMajor.Scheduler

open Quartz

open FSharpMajor.Scanner

type UpdateJob() =
    interface IJob with
        member _.Execute(_ctx: IJobExecutionContext) =
            task {
                try
                    return! scanForUpdates ()
                with exn ->
                    raise (JobExecutionException(exn, false))
            }
