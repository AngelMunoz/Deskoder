namespace Deskoder

open Avalonia.FuncUI.Types

/// This is the main module of your application
/// here you handle all of your child pages as well as their
/// messages and their updates, useful to update multiple parts
/// of your application, Please refer to the `view` function
/// to see how to handle different kinds of "*child*" controls
module Shell =
    open Elmish
    open Avalonia
    open Avalonia.Controls
    open Avalonia.Input
    open Avalonia.FuncUI
    open Avalonia.FuncUI.Builder
    open Avalonia.FuncUI.Components.Hosts
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Elmish
    open LibVLCSharp.Shared


    type State =
        { libVlc: LibVLC
          player: MediaPlayer
          discoverer: RendererDiscoverer
          renderers: RendererItem list
          selectedRenderer: RendererItem option
          canStartCasting: bool
          isDiscovering: bool
          isPlaying: bool }

    type Msg =
        | RendererAdded of RendererItem
        | RendererDeleted of RendererItem
        | SelectRenderer of RendererItem option
        | SetCanStartPlaying of bool
        | SetIsPlaying of bool
        | SetIsDiscovering of bool
        | StartPlaying
        | StopPlaying

    module Subs =
        let rendererAdded (discoverer: RendererDiscoverer) =
            let sub dispatch =
                discoverer.ItemAdded.Subscribe(fun args -> dispatch (RendererAdded args.RendererItem)) |> ignore
            Cmd.ofSub sub

        let rendererDeleted (discoverer: RendererDiscoverer) =
            let sub dispatch =
                discoverer.ItemDeleted.Subscribe(fun args -> dispatch (RendererDeleted args.RendererItem)) |> ignore
            Cmd.ofSub sub

    let init (libVlc: LibVLC, mediaPlayer: MediaPlayer, discoverer: RendererDiscoverer) =
        { libVlc = libVlc
          player = mediaPlayer
          renderers = List.empty
          discoverer = discoverer
          selectedRenderer = None
          canStartCasting = false
          isDiscovering = false
          isPlaying = false }, Cmd.ofMsg (SetIsDiscovering true)

    let update (msg: Msg) (state: State): State * Cmd<_> =
        match msg with
        | RendererAdded renderer -> { state with renderers = renderer :: state.renderers }, Cmd.none
        | RendererDeleted renderer ->
            let renderers = state.renderers |> List.filter (fun r -> renderer.Name = r.Name)
            { state with renderers = renderers }, Cmd.none
        | SetCanStartPlaying canStart -> { state with canStartCasting = canStart }, Cmd.none
        | SetIsPlaying isPlaying -> { state with isPlaying = isPlaying }, Cmd.none
        | SetIsDiscovering isDiscovering ->
            match isDiscovering with
            | true -> state.discoverer.Start() |> ignore
            | false -> state.discoverer.Stop() |> ignore
            { state with isDiscovering = isDiscovering }, Cmd.none
        | SelectRenderer renderer ->
            let cmd =
                match renderer with
                | Some _ -> Cmd.ofMsg (SetCanStartPlaying true)
                | None -> Cmd.ofMsg (SetCanStartPlaying false)
            { state with selectedRenderer = renderer }, cmd
        | StartPlaying ->
            use media = new Media(state.libVlc, "screen://", FromType.FromLocation)
            media.AddOption(":screen-fps=30")
            media.AddOption(":sout=#chromecast{ip=192.168.0.6,mux=mp4}")
            media.AddOption(":sout-keep")
            match state.selectedRenderer with
            | Some renderer -> state.player.SetRenderer(renderer) |> ignore
            | None -> ()
            state.player.Play(media) |> ignore
            state, Cmd.ofMsg (SetIsPlaying true)
        | StopPlaying ->
            state.player.Stop()
            state, Cmd.ofMsg (SetIsPlaying false)

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ DockPanel.children
                [ StackPanel.create
                    [ StackPanel.children
                        [ for renderer in state.renderers do
                            Button.create
                                [ Button.content renderer.Name
                                  Button.onClick (fun _ -> dispatch (SelectRenderer(Some renderer))) ]
                          if state.canStartCasting then
                              Button.create
                                  [ Button.content ("Start Casting")
                                    Button.onClick (fun _ -> dispatch StartPlaying) ]
                          if state.isPlaying then
                              Button.create
                                  [ Button.content ("Stop Casting")
                                    Button.onClick (fun _ -> dispatch StopPlaying) ] ] ] ] ]

    /// This is the main window of your application
    /// you can do all sort of useful things here like setting heights and widths
    /// as well as attaching your dev tools that can be super useful when developing with
    /// Avalonia
    type MainWindow() as this =
        inherit HostWindow()
        do
            base.Title <- "Quickstart"
            base.Width <- 800.0
            base.Height <- 600.0
            base.MinWidth <- 800.0
            base.MinHeight <- 600.0

            let libvlc = new LibVLC()
            let mediaPlayer = new MediaPlayer(libvlc)
            let discoverer = new RendererDiscoverer(libvlc)


            //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
            //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

            this.AttachDevTools(KeyGesture(Key.F12))


            Elmish.Program.mkProgram init update view
            |> Program.withHost this
            |> Program.withSubscription (fun _ -> Subs.rendererAdded discoverer)
            |> Program.withSubscription (fun _ -> Subs.rendererDeleted discoverer)
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith (libvlc, mediaPlayer, discoverer)
