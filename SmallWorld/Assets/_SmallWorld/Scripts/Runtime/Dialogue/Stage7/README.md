# Stage 7 dialogue runtime interface

The assembly is engine-independent. UI and scene integration should create a `DialogueDefinition`, keep a shared `DialogueState`, and construct a `DialogueSession` only after `definition.CanShowInMenu(state)` succeeds.

- Read `DialogueSession.Current` to render `SpeakerName`, `Text`, and filtered `Choices`.
- Call `Advance()`, `SelectChoice(id)`, `Tick(deltaSeconds)`, or `Skip()` from input/UI code.
- Subscribe to `FrameChanged` and `Completed`; note that the initial frame is already available immediately after construction.
- Store relationships, player state, flags, and menu gates as integer keys in `DialogueState`. Missing keys evaluate as zero.
- `DialogueEffect` adds by default; pass `replace: true` for flags or absolute state.
- Conditional nodes use `FallbackNodeId`. Empty next/fallback IDs finish the dialogue. `Skip()` stops at choices, never chooses for the player, and throws on a repeated node or excessive single-call traversal. `Tick()` advances at most one node per call, including automatic cycles.
- `History` contains displayed lines and explicit player choices in sequence.

Definitions validate duplicate IDs and broken links at construction time. Content import or ScriptableObject adapters can be added by another task without changing the runtime API.
