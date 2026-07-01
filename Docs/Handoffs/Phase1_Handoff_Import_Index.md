# Lost Forest Phase 1 Handoff Import Index

## Purpose

This index tracks Phase 1 mechanic handoffs as they are brought into Phase 2.

Phase 2 should import proven rules, relationships, and tuning lessons from Phase 1, but should not import the board-game presentation unless it serves hidden world logic or debugging.

## Import Rule

For every Phase 1 handoff, answer:

- What did this system prove?
- What logic carries forward?
- What presentation should be discarded?
- What data does Phase 2 need?
- What debug view does Phase 2 need?
- What is the smallest playable Phase 2 version?

## Handoff Areas

| Area | Status | Phase 2 Destination |
| --- | --- | --- |
| Board/tile layout | Spec drafted | Tile layout and 3D chunk architecture |
| Rune search/return | Not imported | Rune and standing-stone loop |
| Stamina/chill/sprint | Not imported | Player stats and movement pressure |
| Elevation/movement cost | Not imported | Terrain and slope modifiers |
| Landmarks/navigation | Not imported | Landmark placement and navigation language |
| Pursuer logic | Not imported | Pursuer pressure state machine |
| Debug tools | Not imported | Debug overlay and tuning commands |

## Recommended First Import

Start with board/tile layout.

This is the key bridge between Phase 1 and Phase 2 because the Phase 2 world will still be built from tile IDs, but those IDs will map to 3D hex landscape chunks instead of visible board spaces.

Current spec:

- [Phase2_Board_Tile_Construction_Spec](../Architecture/Phase2_Board_Tile_Construction_Spec.md)

Imported Phase 1 handoff:

- [Phase1_Hex_And_Board_Construction_Handoff](Phase1_Hex_And_Board_Construction_Handoff.md)
