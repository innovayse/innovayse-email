# Handoff: Email Client (Innovayse Mail)

## Overview
A full interactive webmail client UI — sidebar with folders, message list, reading pane, and a compose modal — restyled to match Innovayse's dark, glassmorphic brand (same visual system as the auth redesign: deep navy background, cyan→violet gradient accent, drifting glow blobs, glowing particle field, Manrope typeface).

## About the Design Files
`reference.html` in this bundle is a **plain, framework-free HTML/CSS/JS reproduction** of the design — real CSS classes and a small vanilla JS state/render loop, no proprietary syntax. It's the file to point your CLI agent at for porting exact code (colors, spacing, radii, animation timings, interaction logic). It is a design reference, not production code — rebuild the same visual system and behavior using this project's real frontend stack, component structure, and actual mail data/API instead of the static JS array of sample emails here.

## Fidelity
**High-fidelity.** Colors, spacing, radii, and animation timings/easings in `reference.html` are final — implement pixel-close to these values.

## Layout & Screens

**Sidebar (224px, fixed):** brand mark + wordmark, profile card (avatar, name, email, settings icon), full-width gradient "Compose" button, folder nav (Inbox — with live unread-count badge, Drafts, Sent, Archive, Junk, Templates, Trash), storage usage bar at the bottom. Background: blurred glass panel with a **non-interactive drifting particle canvas** behind all content (small glowing dots in the two accent colors, connecting lines between nearby particles, continuous slow drift, no mouse interaction — see `#sidebar-particles` script block).

**Top bar (60px):** current date (left), icon buttons for calendar/contacts/settings/history (right), last one accent-filled.

**Message list + reading pane (main row):** two-pane layout.
- **List panel:** folder title, filter/refresh icons, search field, then a scrollable list of message rows (gradient avatar with initials, sender name, timestamp, subject, preview snippet, unread dot indicator). Selected row gets a tinted background + accent border.
- **Reading pane:** shows the open message (subject, sender avatar/name/email/time, reply/forward/archive/delete icon actions, body paragraphs) or an empty state ("Select a message to read") when nothing is open.

**Compose modal:** centered glass card overlay with its own ambient glow behind it. Header has a small gradient pencil badge + "New message" + close button. Icon-led rounded To/Subject fields (same input style as the login page). A grouped toolbar "pill" (bold/italic/underline/list/link/attach icons). A body textarea. Footer: discard icon, "Save draft" outline button, gradient "Send" button. Send/Save/Discard all show a brief toast confirmation ("Message sent", "Draft saved", "Draft discarded").

## Key Interaction: List/Reading-Pane Collapse (Gmail-style)
This is the most implementation-sensitive behavior — read carefully.

- **No email selected** (initial load, or right after switching folders): the reading pane is fully collapsed and the message list expands to fill the entire width of the main row, edge-to-edge — like Gmail's list view.
- **Selecting a message:** the list panel animates back down to its normal width (340px, clamped 260–380px in the original prototype, though the plain reference simplifies this to a straightforward 340px ↔ 100% width toggle) while the reading pane reveals the selected message and its "Back to inbox" link.
- **Switching folders always collapses the reading pane again** (selection resets to none), returning to the full-width list for that folder.
- A "Back to inbox" link at the top of the open message also collapses the pane back to full-width list view.
- **Animation implementation detail:** only the **list panel's `width`** is transitioned (`transition: width .32s cubic-bezier(.22,1,.36,1)`). The reading pane itself is always `flex: 1` with no width/flex-basis toggling — it fills whatever space remains and therefore resizes smoothly every animation frame as the sibling's width transitions. This was a deliberate fix after an earlier version tried to transition `flex-grow`/`min-width` on the reading pane directly, which does not interpolate reliably across browsers. **Do not reintroduce that pattern** — keep the reading pane's sizing passive.
- **Reactive glow:** a dedicated blurred glow blob sits behind the reading pane (`.reading-glow` / `#reading-glow`) and blooms brighter + scales up (`opacity 0.12→0.32`, `scale(0.8)→scale(1)`) when a message is open, dimming back down when the pane collapses — using the same `.38s cubic-bezier(.22,1,.36,1)` timing so it reads as part of the same motion as the width change.

## State Management
- `folder`: which nav item is active — drives which emails are filtered into the list.
- `selectedId`: id of the open email, or `null` — drives reading-pane visibility, list-panel width, and the reactive glow. Reset to `null` on every folder switch.
- `unread` per email — flips to `false` the moment that email is opened (drives the unread dot, bold sender/subject text, and the Inbox nav badge count, which only counts unread mail in Inbox).
- Compose modal open/closed — independent boolean, not tied to selection state.
- Toast message + auto-dismiss timer (~2.4s) — shown after Send/Save draft/Discard in the compose modal.

## Design Tokens
Same system as the auth redesign handoff (reuse those tokens if already implemented):
- Background: `#07080d`. Sidebar/panels: `rgba(14,16,26,0.55)` / `rgba(12,14,22,0.4)` with `backdrop-filter: blur(20–24px) saturate(160%)`.
- Accent: gradient `#29A3E8 → #8B5CF6`, `135deg`. Same 3 alternate curated palettes as the auth flow (teal→blue, violet→pink, cyan→teal) are supported as a theme swap.
- Font: Manrope, weights 400–800.
- Radii: 24px (compose modal), 20–22px (cards), 12–14px (buttons/inputs/rows), 10–11px (icon buttons, badges).
- Per-sender avatar colors are **data-driven**, not part of the fixed theme — each sample email carries its own two-color CSS gradient string (`gradient` field) so avatars have variety; initials are the first letters of the sender name.
- Background glows: 4 large blurred radial circles (`#29A3E8` / `#8B5CF6` at 9–22% opacity, 110–140px blur) positioned at the corners, each drifting via slow (18–24s) CSS keyframe loops — same technique as the auth page's background.

## Assets
All icons are hand-drawn inline SVGs (stroke-based, `stroke-width: 1.6–2.2`), no icon library. No raster images/photos.

## Files
- **`reference.html` — start here.** Complete, self-contained, framework-free HTML/CSS/JS port of this entire design (sidebar, list, reading pane, compose modal, collapse animation, reactive glow, particle canvas, toast). Open directly in any browser to interact with it. Read this file's `<style>` and `<script>` blocks for the literal values and logic, then re-implement the same structure/behavior in the app's real component architecture with real data.
