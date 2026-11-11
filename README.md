# Instant Work Supply

For RimWorld 1.7. A pawn skips travel to a haulable item reserved by its current job, and vanilla carry toils put that item into its hands. Pawns teleport to the end of each subsequent vanilla path, so carried ingredients move instantly to work targets and vanilla job toils still place them. Other pawn movement also teleports.

Items are selected, reserved, picked up, and placed by vanilla job logic. Forbidden items and items reserved by another pawn are not taken. Jobs retain vanilla candidate-selection and reachability rules; this mod does not add a separate map-wide item search. Movement that does not use `Pawn_PathFollower` is not affected. Teleporting all pawn movement can bypass normal travel time and movement-related interactions.

Requires Harmony.

Run `Build.bat` to build the Release assembly into `1.7\Assemblies\InstantWorkSupply.dll`. Use `Build.bat /nopause` to suppress the final pause.
