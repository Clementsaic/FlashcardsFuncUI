namespace CounterApp

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Controls.Primitives
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Microsoft.FSharp.Core

type Card =
    { Face: string
      Back: string
      FaceUp: bool
      Id: int }

module Main =

    let view () =
        Component(fun ctx ->
            let state =
                ctx.useState
                    [ { Face = "bruh"
                        Back = "unchungus"
                        FaceUp = true
                        Id = 0 }
                      { Face = "1"
                        Back = "2"
                        FaceUp = true
                        Id = 1 }
                      { Face = "a"
                        Back = "b"
                        FaceUp = true
                        Id = 2 } ]

            let emptyCard =
                { Face = ""
                  Back = ""
                  FaceUp = true
                  Id = -1 }

            let flipCard (cards: Card List, id: int) =
                List.foldBack
                    (fun card cardsUpdated ->
                        { card with
                            FaceUp = (if card.Id = id then not card.FaceUp else card.FaceUp) }
                        :: cardsUpdated)
                    cards
                    []

            let setAllCards (cards: Card List, faceUp: bool) =
                List.foldBack (fun card cardsUpdated -> { card with FaceUp = faceUp } :: cardsUpdated) cards []

            let deleteCard (cards: Card List, id: int) =
                List.foldBack
                    (fun card cardsUpdated ->
                        if card.Id = id then
                            cardsUpdated
                        else
                            { card with
                                Id = card.Id - if card.Id > id then 1 else 0 }
                            :: cardsUpdated)
                    cards
                    []

            let cardButtons (cards: Card List) : Types.IView List =
                (List.foldBack
                    (fun card views ->
                        Button.create
                            [ Button.width 500
                              Button.height 300
                              Button.margin 20
                              Button.verticalContentAlignment VerticalAlignment.Center
                              Button.horizontalContentAlignment HorizontalAlignment.Center
                              Button.content (if card.FaceUp then card.Face else card.Back)
                              Button.onClick (fun _ -> state.Set(flipCard (state.Current, card.Id)))
                              Button.onContextRequested (fun args ->
                                  state.Set(deleteCard (state.Current, card.Id))
                                  args.Handled <- true) ]
                        :: views)
                    cards
                    [])
                @ [ Button.create
                        [ Button.width 500
                          Button.height 300
                          Button.margin 20
                          Button.verticalContentAlignment VerticalAlignment.Center
                          Button.horizontalContentAlignment HorizontalAlignment.Center
                          Button.content "+"
                          Button.onClick (fun _ ->
                              state.Set(
                                  state.Current
                                  @ [ { emptyCard with
                                          Id = List.length state.Current } ]
                              )) ] ]

            let stateString (cards: Card List) =
                List.foldBack (fun card ids -> $"{card.Id}.{if card.FaceUp then 0 else 1}" :: ids) cards []
                |> String.concat "-"

            // printfn "%s" (stateString state.Current)

            let cardView (cards: Card List) =
                Component.create (
                    stateString cards,
                    fun ctx -> WrapPanel.create [ WrapPanel.children (cardButtons state.Current) ]
                )

            DockPanel.create
                [ DockPanel.children
                      [ Menu.create
                            [ Menu.dock Dock.Top
                              Menu.viewItems
                                  [ MenuItem.create [ MenuItem.header "Load" ]
                                    MenuItem.create [ MenuItem.header "Save" ]
                                    MenuItem.create
                                        [ MenuItem.header "FaceUp"
                                          MenuItem.onClick (fun _ -> state.Set(setAllCards (state.Current, true))) ]
                                    MenuItem.create
                                        [ MenuItem.header "FaceDown"
                                          MenuItem.onClick (fun _ -> state.Set(setAllCards (state.Current, false))) ]
                                    MenuItem.create
                                        [ MenuItem.header "Shuffle"
                                          MenuItem.onClick (fun _ -> state.Set(List.randomShuffle state.Current)) ] ] ]
                        ContentControl.create [ ContentControl.content (cardView state.Current) ] ] ]

        // UniformGrid.create [
        //     UniformGrid.rows (fun _ -> (int ctx.control.Width % 400))
        //     UniformGrid.columns 4
        //     UniformGrid.name "chungus"
        //     UniformGrid.children [
        //         TextBox.create [  ]
        //         TextBox.create [  ]
        //         TextBox.create [  ]
        //         TextBox.create [  ]
        //         TextBox.create [  ]
        //         TextBox.create [  ]
        //         TextBox.create [  ]
        //     ]
        // ]
        // bruh
        // Grid.create [
        //     Grid.rowDefinitions "Auto, Auto, Auto"
        //     Grid.columnDefinitions "Auto, Auto, Auto"
        //     Grid.children [
        //         Button.create [
        //             Button.row 0
        //             Button.column 0
        //             Button.onClick (fun _ -> state.Set(state.Current + 1))
        //             Button.content "bruh"
        //         ]
        //         Button.create [
        //             Button.row 0
        //             Button.column 1
        //             Button.onClick (fun _ -> state.Set(state.Current + 1))
        //             Button.content "bruv"
        //         ]
        //         Button.create [
        //             Button.row 1
        //             Button.column 0
        //             Button.onClick (fun _ -> state.Set(state.Current + 1))
        //             Button.content "brugg"
        //         ]
        //     ]
        // ]
        // bruv
        // DockPanel.create [
        //     DockPanel.children [
        //         Button.create [
        //             Button.dock Dock.Bottom
        //             Button.onClick (fun _ -> state.Set(state.Current - 1))
        //             Button.content "-"
        //             Button.horizontalAlignment HorizontalAlignment.Stretch
        //             Button.horizontalContentAlignment HorizontalAlignment.Center
        //         ]
        //         Button.create [
        //             Button.dock Dock.Bottom
        //             Button.onClick (fun _ -> state.Set(state.Current + 1))
        //             Button.content "+"
        //             Button.horizontalAlignment HorizontalAlignment.Stretch
        //             Button.horizontalContentAlignment HorizontalAlignment.Center
        //         ]
        //         TextBlock.create [
        //             TextBlock.dock Dock.Top
        //             TextBlock.fontSize 48.0
        //             TextBlock.verticalAlignment VerticalAlignment.Center
        //             TextBlock.horizontalAlignment HorizontalAlignment.Center
        //             TextBlock.text (string state.Current)
        //         ]
        //     ]
        // ]
        )

type MainWindow() =
    inherit HostWindow()

    do
        base.Title <- "FuncCards"
        base.Content <- Main.view ()

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime -> desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main (args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
