# Task for Codex: ingredient boxes on the pantry shelves

Unity 6 (6000.3.26f1), URP 17, new Input System. Game: *Nutrition Kitchen*, first-person co-op cooking sim.
All gameplay code is in `Assets/Scripts/Minigames/` (namespace `NutritionKitchen.Minigames`); ingredient data in
`Assets/Scripts/Crafting/` + `Assets/Data/Ingredients/*.asset`. Everything is spawned at runtime by
`MinigameBootstrap.cs` (finds restaurant objects by name: `Dry_Storage_Shelf_1`, `Dry_Storage_Shelf_2`, etc.).

## What the player wants

Replace the current pantry interaction (look at the shelf, press number keys 1-7) with **one cardboard box per
ingredient** on the two dry-storage shelves (`Dry_Storage_Shelf_1` and `_2`):

- 7 ingredients (from `Pantry.stock`), one box each, spread over both shelves (shelf tops are at about
  `bounds.min.y + 1.08` and `+ 1.53`; shelves are 2.4 m wide, 0.55 m deep, box ~0.5 x 0.34 x 0.4 m fits).
  Rest of the shelves stays empty.
- On the **front** of each box: an **icon of the ingredient**, made at runtime by photographing its 3D model
  (`PropFactory.IngredientModel(ing, parent, pos, size)`) with a temporary camera into a RenderTexture,
  then **name + count** under it (e.g. `Chicken Meat` / `x2`, `OUT OF STOCK` at 0, box looks faded at 0).
- **Aim the crosshair at a box and press E** (empty hands) to take one unit -> `hands.Hold(FoodItem.Create(ing))`.
  Holding an unused raw clean one of the *same* ingredient, E on its box puts it back (+1).
- Keep budget/ordering (`Pantry.Order`, `Money`, `Count`, `OnTheWay`) exactly as it is: the freezer screen
  (`FreezerScreen.cs`) and drone delivery (`DroneDelivery.cs`) depend on it.

## How the existing code works (reuse it)

- `KitchenInteractable` (abstract): `Prompt(PlayerHands)` + `HandleInput(PlayerHands, Keyboard)`; add a trigger
  with `AddTrigger(center, size)`. `StationInteractor` raycasts from the camera and calls these on whatever is aimed at.
  A box should be its own `KitchenInteractable`.
- Builders face **local -Z** toward the player (back wall is `+fwd`, where `fwd = Flat(shelf.transform.forward)`).
- `PropFactory.Prim(...)` makes primitives with cached URP Lit materials; `PropFactory.SetColor` per-object tint;
  `PropFactory.Text(...)` makes world-space TextMesh text that depth-tests (shader `Assets/Resources/Text3D.shader`).
  Don't parent text under non-uniformly scaled objects (it squashes).
- Dev keys (editor only, `StationInteractor.DevJump`): F1-F9 jump to stations/pantry/bin/pass sorted by name,
  F10 freezer screen, F11 drop pad. If you add new `KitchenInteractable`s (the boxes), **exclude them from that list**
  like `DroppedFood`/`DeliveryBox`, and keep `Pantry` in it so F4 still jumps to the pantry.

## Gotchas already found

- **URP ignores `Camera.Render()`** -> icons come out black. Use
  `RenderPipeline.SubmitRenderRequest(cam, new RenderPipeline.StandardRequest { destination = rt })`
  (`using UnityEngine.Rendering;`), with a camera whose `cullingMask` is an unused layer (e.g. 29) holding only the model.
- Icon quad material: `Universal Render Pipeline/Unlit`, set `_BaseMap` to the RenderTexture.
- Unity only notices script changes when its window regains focus: press **Ctrl+R** before Play.
  Don't let Unity recompile *during* Play mode (runtime-built objects lose their data -> NullReferenceExceptions).
- `Assets/Scripts/Props/DumpsterBuilder.cs` lines 622 and 630 currently don't compile (float passed to an `int`
  parameter of `DrawRect`); Unity blocks all scripts until that's fixed.

## Done when

Both shelves show the 7 labelled boxes with real (not black) icons, aim + E takes/puts back, counts update,
ordering at the freezer screen + drone delivery still work, and the project compiles with no errors.
