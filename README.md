# Morrowind Menus & Trading

This RimWorld 1.6 mod project is now focused entirely on two systems:

- a Morrowind inspired pawn inventory UI
- large personal colonist inventories that behave more like stockpiles and trader networks

## Current feature goals in this source tree

- darker black and gold Morrowind style inventory windows
- large inventory icons and an equipped-items strip aligned on the left
- automatic pickup of nearby haulables into colonist inventories
- category trader roles with auto defaults from the pawn's best skills
- pawn to pawn sharing of food, weapons, medicine, and build resources
- top left resource counter support for items stored in colonist inventories

## Build

Use the included GitHub Actions workflow to compile the DLL and package the mod for RimWorld 1.6.


Latest update: stronger pawn-first, role-favored personal inventory filling with stricter allowed-category checks and broader storage scanning.


Version metadata note: this package advertises support for RimWorld 1.6 and 1.7, while still loading the same compiled code from the 1.6 folder for both branches.
