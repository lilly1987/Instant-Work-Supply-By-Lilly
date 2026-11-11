# Instant Work Supply

For RimWorld 1.7. Reuses vanilla work selection, ingredient selection, hauling, stack-count, and reservation logic. When a pawn's current job has selected and reserved a haulable item, the pawn teleports to the vanilla path destination for pickup; vanilla `StartCarryThing` puts the selected stack into the pawn's hands. While carrying an item, the pawn teleports to supported job destinations, where vanilla placement toils deposit it.

Because vanilla selects the source items, its eligibility rules remain in effect. Forbidden items and items reserved by another pawn are not taken. A job that cannot find enough eligible material continues to fail or retry as it normally would. This mod does not add a separate map-wide item search or bypass vanilla reachability checks.

The mod affects standard goto-to-item and haul destination toils. A mod that implements its own pickup or delivery movement may require a separate compatibility patch.

Requires Harmony.

Run `Build.bat` to build the Release assembly into `1.7\Assemblies\InstantWorkSupply.dll`. Use `Build.bat /nopause` to suppress the final pause.
