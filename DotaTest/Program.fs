module DotaGSI

open System
open System.Reflection
open Dota2GSI
open Dota2GSI.Nodes
open Dota2GSI.EventMessages

[<Literal>]
let port = 8001

[<Literal>]
let cnfg_name = "Questera.Games"

let prnt (element: Object, value: string) =
    printfn $"{DateTimeOffset.UtcNow.ToString()} - {nameof element}: {value}"

    element.GetType().GetProperties()
    |> Array.filter (fun (prop: PropertyInfo) -> prop.CanRead)
    |> Array.iter (fun (prop: PropertyInfo) ->
        let value = prop.GetValue(element)
        printfn $"{prop.Name}: {value}")

let crt_cfg (listener: GameStateListener) =
    listener.GenerateGSIConfigFile(cnfg_name)
    |> fun success ->
        printfn
            "%s"
            (if success then
                 "Configuration file generated successfully."
             else
                 "Failed to generate the configuration file.")

        listener

let build_listener (p: int) =
    new GameStateListener(p)
    |> crt_cfg
    |> fun (listener: GameStateListener) ->
        listener.add_TimeOfDayChanged (fun (data: TimeOfDayChanged) -> prnt(data, $"{nameof data.IsDaytime}: {data.IsDaytime}"))
        listener.add_AbilityAdded (fun (data: AbilityAdded) -> prnt(data, $"{nameof data.Player}: {data.Player.Details.Name} - {data.Value.Name}"))
        listener.add_AbilityUpdated (fun (data: AbilityUpdated) -> prnt(data, $"{nameof data.Player}: {data.New.Name} {data.Previous.Level} {data.New.Level}"))
        listener.add_HeroTookDamage (fun (data: HeroTookDamage) -> prnt(data, $"{nameof data.Player}: {data.New} {data.Previous}"))
        listener.add_PlayerKillsChanged (fun (data: PlayerKillsChanged) -> prnt(data, $"{nameof data.Player}: {data.New} {data.Previous}"))

        listener.add_GameStateChanged (fun (data: GameStateChanged) ->
            if data.New <> data.Previous then
                prnt(data, $"Game State Changed: {data.New}"))

        listener

[<EntryPoint>]
let main argv =
    let listener = build_listener port
    let res = listener.Start()
    match res with
    | true -> printf "Success Starting"
    | false -> printf "Failed Starting"
    while listener.Running do
        if listener.CurrentGameState.Map.GameState = DOTA_GameState.DOTA_GAMERULES_STATE_POST_GAME then
            listener.Stop()
    0 // Return an integer exit code