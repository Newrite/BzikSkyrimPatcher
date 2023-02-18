namespace Bzikovich

[<RequireQualifiedAccess>]
type ConsoleNotify =
  | Continue
  | Cancel

[<RequireQualifiedAccess>]
type Step =
  | ChooseGameVersion
  | ChoosePatcher

[<RequireQualifiedAccess>]
type GameVersion =
  | Skyrim
  | Enderal
  | None

  member self.Api() =
    match self with
    | GameVersion.Skyrim -> Patcher.ApiGameRelease.Skyrim
    | GameVersion.Enderal -> Patcher.ApiGameRelease.Enderal
    | GameVersion.None -> Patcher.ApiGameRelease.Skyrim

[<RequireQualifiedAccess>]
type Inputs =
  | Exit
  | GameEnderal
  | GameSkyrim
  | PatchGetAllArmor
  | PatchGetAllWeapon
  | PatchGetAllSpellStaffs
  | PatchBzikWeapon
  | PatchGetEnchantmentsAndSpellsWithReduceCosts
  | None

[<RequireQualifiedAccess>]
module Commands =
  [<Literal>]
  let Exit = "exit"

  [<Literal>]
  let Skyrim = "skyrim"

  [<Literal>]
  let Enderal = "enderal"

  [<Literal>]
  let PatchGetAllArmor = "armor_get"

  [<Literal>]
  let PatchGetAllWeapon = "weapon_get"

  [<Literal>]
  let PatchGetAllSpellStaffs = "spell_staffs_get"

  [<Literal>]
  let PatchBzikWeapon = "weapon_patch"

  [<Literal>]
  let PatchGetEnchantmentsAndSpellsWithReduceCosts = "ench_spell_cost_get"

module Patcher =
  let mutable step = Step.ChooseGameVersion
  let mutable gameVersion = GameVersion.None

  let mutable extraInfoString = ""

  let templateMenu currentStep =
    if currentStep = Step.ChooseGameVersion then
      $"
  First choose game version:
  {Commands.Enderal} or {Commands.Skyrim}?
  {extraInfoString}
  enter {Commands.Exit} to exit.
      "
    else
      $"
  Enter patcher:
  {Commands.PatchGetAllArmor}
  {Commands.PatchGetAllWeapon}
  {Commands.PatchBzikWeapon}
  {Commands.PatchGetAllSpellStaffs}
  {Commands.PatchGetEnchantmentsAndSpellsWithReduceCosts}
  {extraInfoString}
  enter {Commands.Exit} to exit.
      "

  let inputHandler (input: string) =
    match input.ToLower() with
    | Commands.Exit -> Inputs.Exit
    | Commands.Enderal -> Inputs.GameEnderal
    | Commands.Skyrim -> Inputs.GameSkyrim
    | Commands.PatchGetAllArmor -> Inputs.PatchGetAllArmor
    | Commands.PatchGetAllWeapon -> Inputs.PatchGetAllWeapon
    | Commands.PatchGetAllSpellStaffs -> Inputs.PatchGetAllSpellStaffs
    | Commands.PatchBzikWeapon -> Inputs.PatchBzikWeapon
    | Commands.PatchGetEnchantmentsAndSpellsWithReduceCosts -> Inputs.PatchGetEnchantmentsAndSpellsWithReduceCosts
    | _ -> Inputs.None

  let matchInput input =
    match inputHandler input with
    | Inputs.GameEnderal ->
      gameVersion <- GameVersion.Enderal
      step <- Step.ChoosePatcher
      ConsoleNotify.Continue
    | Inputs.GameSkyrim ->
      gameVersion <- GameVersion.Skyrim
      step <- Step.ChoosePatcher
      ConsoleNotify.Continue
    | Inputs.PatchGetAllArmor ->
      extraInfoString <- Patcher.Patch(gameVersion.Api(), Patcher.ApiPatcherMod.PatchGetAllArmor)
      ConsoleNotify.Continue
    | Inputs.PatchBzikWeapon ->
      extraInfoString <- Patcher.Patch(gameVersion.Api(), Patcher.ApiPatcherMod.PatchBzikWeapon)
      ConsoleNotify.Continue
    | Inputs.PatchGetAllSpellStaffs ->
      extraInfoString <- Patcher.Patch(gameVersion.Api(), Patcher.ApiPatcherMod.PatchGetAllSpellStaffs)
      ConsoleNotify.Continue
    | Inputs.PatchGetAllWeapon ->
      extraInfoString <- Patcher.Patch(gameVersion.Api(), Patcher.ApiPatcherMod.PatchGetAllWeapon)
      ConsoleNotify.Continue
    | Inputs.PatchGetEnchantmentsAndSpellsWithReduceCosts ->
      extraInfoString <-
        Patcher.Patch(gameVersion.Api(), Patcher.ApiPatcherMod.PatchGetEnchantmentsAndSpellsWithReduceCosts)

      ConsoleNotify.Continue
    | Inputs.Exit ->
      System.Console.Clear()
      printfn "Bye bye"
      ConsoleNotify.Cancel
    | Inputs.None ->
      extraInfoString <- $"Unhandled input {input}, please, try enter again"
      ConsoleNotify.Continue

  [<EntryPoint>]
  let rec mainLoop args =
    System.Console.Clear()
    printfn "%s" <| templateMenu step
    extraInfoString <- ""
    let input = System.Console.ReadLine()

    match matchInput input with
    | ConsoleNotify.Continue -> mainLoop args
    | ConsoleNotify.Cancel -> 0
