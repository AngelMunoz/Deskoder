namespace Deskoder

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Dialogs
open Avalonia.FuncUI
open LibVLCSharp.Shared

/// This is your application you can ose the initialize method to load styles
/// or handle Life Cycle events of your application
type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
        this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml"
        this.Styles.Load "avares://Deskoder/Styles.xaml"

    override this.OnFrameworkInitializationCompleted() =
        Core.Initialize()
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- Shell.MainWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main (args: string []) =
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .UseManagedSystemDialogs<AppBuilder>()
            .StartWithClassicDesktopLifetime(args)
