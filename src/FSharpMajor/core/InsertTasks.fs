module FSharpMajor.InsertTasks

open System
open System.Threading.Tasks

open Dapper.FSharp.PostgreSQL
open Dapper

open FSharpMajor.DatabaseTypes
open FSharpMajor.FsLibLog
open FSharpMajor.Database
open Npgsql

let private insertAndOrReturn<'T when 'T :> IQueryField> (conn: NpgsqlConnection) (entity: 'T) =
    task {
        let propName, propValue = entity.QueryField

        let sql =
            $"SELECT t.* FROM {typeof<'T>.Name} as t \
                        WHERE t.{propName} = @A"

        let! existing = conn.QueryAsync<'T>(sql, struct {| A = propValue |})

        match existing |> Seq.tryHead with
        | None ->
            return!
                insert {
                    into table<'T>
                    value entity
                }
                |> conn.InsertOutputAsync<'T, 'T>
        | Some _ -> return existing
    }

let private insertManyAndOrReturn<'T when 'T :> IQueryField and 'T: comparison>
    (conn: NpgsqlConnection)
    (entities: 'T list)
    =
    task {
        let h = List.head entities
        let fieldName = h.QueryField |> fst
        let searchList = entities |> List.map (fun e -> e.QueryField |> snd) |> Array.ofList

        let sql =
            $"SELECT t.* FROM {typeof<'T>.Name} as t \
                    WHERE t.{fieldName} = ANY @A"

        let! existing = conn.QueryAsync<'T>(sql, struct {| A = searchList |})

        let foundEntities = existing |> Set.ofSeq

        let toInsert =
            entities
            |> Set.ofList
            |> (fun a -> Set.difference a foundEntities)
            |> Set.toList

        match toInsert with
        | [] -> return existing
        | _ ->
            let! inserted =
                insert {
                    into table<'T>
                    values toInsert
                }
                |> conn.InsertOutputAsync<'T, 'T>

            return Seq.append inserted existing
    }

let insertTask (entities: #IComparable list) =
    match entities with
    | [] -> Task.FromResult Seq.empty
    | [ entity ] ->
        task {
            use! conn = npgsqlSource.OpenConnectionAsync()
            return! insertAndOrReturn conn entity
        }
    | _ ->
        task {
            use! conn = npgsqlSource.OpenConnectionAsync()
            return! insertManyAndOrReturn conn entities
        }

let private insertOneJoin<'T when 'T :> IJoinTable> (conn: NpgsqlConnection) (entity: 'T) =
    task {
        let field1, field2 = entity.QueryIdNames

        let sql =
            $"SELECT t.* FROM {typeof<'T>.Name} as t \
                        WHERE t.{field1} = @A AND t.{field2} = @B"

        let a, b = entity.QueryIdValues

        let! existing = conn.QueryAsync<'T>(sql, struct {| A = a; B = b |})

        match existing |> Seq.tryHead with
        | None ->
            return!
                insert {
                    into table<'T>
                    value entity
                }
                |> conn.InsertOutputAsync<'T, 'T>
        | Some _ -> return existing
    }

let private insertManyJoins<'T when 'T :> IJoinTable and 'T: comparison> (conn: NpgsqlConnection) (entities: 'T list) =
    task {
        let h = List.head entities
        let field1, field2 = h.QueryIdNames

        let query =
            $"SELECT t.* \
                  FROM UNNEST (@A, @B) AS params (a, b) \
                  INNER JOIN {typeof<'T>.Name} as t \
                    ON t.{field1} = params.a \
                    AND t.{field2} = params.b"

        let a, b =
            entities |> List.map (fun e -> e.QueryIdValues) |> Array.ofList |> Array.unzip

        let existingResult = conn.Query<'T>(query, struct {| A = a; B = b |})

        let found = existingResult |> Set.ofSeq

        let needed =
            entities |> Set.ofList |> (fun s -> Set.difference s found) |> Set.toList

        match needed with
        | [] -> return existingResult
        | _ ->
            let! inserted =
                insert {
                    into table<'T>
                    values needed
                }
                |> conn.InsertOutputAsync<'T, 'T>

            return Seq.append inserted existingResult
    }

let insertJoinTable (entities: 'a list) =
    match entities with
    | [] -> Task.FromResult Seq.empty
    | [ entity ] ->
        task {
            use! conn = npgsqlSource.OpenConnectionAsync()
            return! insertOneJoin conn entity
        }
    | h :: _ ->
        task {
            use! conn = npgsqlSource.OpenConnectionAsync()
            return! insertManyJoins conn entities
        }

let rec updateItemsLoop
    (updateList: directory_items list)
    (updated: directory_items list)
    (conn: Npgsql.NpgsqlConnection)
    =
    task {
        match updateList with
        | [] -> return updated
        | curr :: rest ->
            let! result =
                update {
                    for d in directoryItemsTable do
                        set curr
                        where (d.path = curr.path)
                        excludeColumn d.id
                }
                |> conn.UpdateOutputAsync<directory_items, directory_items>

            let newUpdated = (result |> Seq.head) :: updated
            return! updateItemsLoop rest newUpdated conn
    }

let insertOrUpdateDirectoryItem (directoryItems: directory_items list) =
    task {
        use! conn = npgsqlSource.OpenConnectionAsync()
        let logger = LogProvider.getLoggerByFunc ()

        match directoryItems with
        | [] -> return Seq.empty
        | [ directoryItem ] ->
            let! exists =
                select {
                    for di in directoryItemsTable do
                        where (di.path = directoryItem.path)
                }
                |> conn.SelectAsync<directory_items>

            match exists |> Seq.tryHead with
            | Some e when e <> directoryItem ->
                logger.info (Log.setMessage $"%A{directoryItem.path} to be updated")

                return!
                    update {
                        for di in directoryItemsTable do
                            set directoryItem
                            where (di.path = directoryItem.path)
                            excludeColumn di.id
                    }
                    |> conn.UpdateOutputAsync<directory_items, directory_items>
            | Some e -> return exists
            | None ->
                logger.info (Log.setMessage $"%A{directoryItem.path} to be inserted")

                return!
                    insert {
                        for di in directoryItemsTable do
                            value directoryItem
                            excludeColumn di.id
                    }
                    |> conn.InsertOutputAsync<directory_items, directory_items>

        | _ ->
            let paths = directoryItems |> List.map (fun di -> di.path)

            let! exists =
                select {
                    for di in directoryItemsTable do
                        where (isIn di.path paths)
                }
                |> conn.SelectAsync<directory_items>

            let existingPaths = exists |> Seq.map (fun di -> di.path) |> List.ofSeq

            let toInsert =
                directoryItems
                |> List.filter (fun di -> existingPaths |> List.contains di.path |> not)

            let! inserted =
                match toInsert with
                | [] -> Task.FromResult Seq.empty
                | _ ->
                    insert {
                        for di in directoryItemsTable do
                            values toInsert
                            excludeColumn di.id
                    }
                    |> conn.InsertOutputAsync<directory_items, directory_items>

            let noUpdate, toUpdate =
                exists
                |> List.ofSeq
                |> List.partition (fun ex -> directoryItems |> List.contains ex) // Hopefully .contains uses custom Equality

            let! updated =
                match toUpdate with
                | [] -> Task.FromResult Seq.empty
                | _ ->
                    task {
                        let! result = updateItemsLoop toUpdate [] conn
                        return result |> Seq.ofList
                    }

            return
                seq {
                    inserted
                    updated
                    Seq.ofList noUpdate
                }
                |> Seq.concat
    }
