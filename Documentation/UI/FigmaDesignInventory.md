# CrashSimulator Figma Design Inventory

Audit date: 2026-10-26  
Primary reference: [Published CrashSimulator Figma Make website](https://row-rise-81586427.figma.site/)

## Access status

- **VERIFIED (browser rendered):** published website loaded in Chrome and its Main Menu accessibility tree and rendered screenshot were inspected at 1920×1080.
- **STATIC (browser DOM/CSS inspection):** element bounds, computed typography/colors, background image URL, and status labels were read from the rendered page.
- The Figma desktop/MCP design API is not required for this pass; Main Menu observations come from the public rendered page.
- The screenshot includes a Figma Make publishing badge at lower right. It is platform chrome and is not part of the game UI.
- Only Main Menu was inspected. Other screens are explicitly out of scope until its Unity visual gate passes.

## Main Menu observed design

Reference image: browser capture of `https://row-rise-81586427.figma.site/` at 1920×1080 (captured in the active browser session; no local PNG export was saved).  
Background: full-bleed 1920×1280 Unsplash photograph cropped with cover behavior; dark automotive workshop/interior scene with car seat and bright rear window.  
Image URL: `https://images.unsplash.com/photo-1783207938137-286e6df9fecf?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1920`

| Element | Observed position / size at 1920×1080 | Typography / color | State / interaction |
|---|---:|---|---|
| Status eyebrow | x64 y64 | Inter Bold 11px, #8E9AB5, tracking 1.8px; `CRASH TEST SYSTEMS ONLINE` | Static status |
| Sound button | x1750 y48, 48×48 | Dark translucent button #0C1019, #2A3550 border; 20px white speaker SVG | Click was attempted; no visible state change observed |
| Settings button | x1808 y48, 48×48 | Same button style; 20px white gear SVG | Settings action button |
| `CRASH` | x200 y211, 430×72 | Rajdhani Bold 94px, #F2F5FA, tracking 2px | Static title |
| `SIMULATOR` | x200 y283, 430×36 | Rajdhani Medium 47px, #B9C4D9, tracking 12px | Static title |
| Tagline | x200 y351, 430×29 | Rajdhani Bold 19px, #CBD3E0, tracking 2px; `CRASH.` uses #FF7A1A | Static title |
| Free Drive | x226 y425, 404×61 | Rajdhani Medium 26px #F2F5FA; subtitle Inter Medium 12px #8E9AB5 | Initially selected with a 3px blue vertical indicator; subtitle `Drive freely. Test the limits.` |
| Crash Lab | x226 y496, 404×61 | Rajdhani Medium 26px #ABB5C7 | Navigation action |
| Garage | x226 y567, 404×61 | Rajdhani Medium 26px #ABB5C7 | Navigation action |
| Settings | x226 y638, 404×61 | Rajdhani Medium 26px #ABB5C7 | Navigation action |
| Right-side scrim | full screen | Left-to-right black gradient: #090A0E opaque to rgba(10,10,12,.92) at 28%, .38 at 56%, .22 at right; bottom black gradient starts at 55% | Image legibility overlay |
| Footer rule | near bottom, x64 to x1856 | 1px #2A3550 line | Static divider |
| Build label | x64 y1035 | Inter Bold 10px #738099, tracking 1.7px | `BUILD 0.1.0` |
| Facility marker | x1695 y966 | Rajdhani Medium 32px #2E8BFF, tracking 1.4px | `01` |
| Facility title | x1735 y972, 121×30 | Inter 10px #C8D0DF, tracking 1.4px | `VEHICLE DYNAMICS / SANDBOX` |
| Facility footer | x1742 y1035 | Inter Bold 10px #738099, tracking 1.7px | `TEST FACILITY 01` |

Observed layout: minimal left navigation and title over the photograph, no panels/cards, compact sound/settings controls at the upper right, small facility/build identifiers at the bottom. The screenshot's right side remains image-led. No vehicle model render is present in the observed reference.

## Static asset notes

- The exact background image used by the published page was retrieved from the page's computed CSS image URL and copied to `Assets/_CrashSimulator/UI/Images/MainMenuGarageBackground.jpg` for the Unity implementation.
- **STATIC:** source dimensions are 1920×1280; local JPEG is about 224 KB. Unity import is configured as a compressed Sprite with 2048px max size and mipmaps.
- Source provenance is an Unsplash CDN image ID. The general [Unsplash license](https://unsplash.com/license) is available, but creator/model releases and license provenance for this exact CDN image are **UNVERIFIED**. The public site itself provides no creator credit in the observed view.
- The two visible icons were observed as inline SVGs in the web page. Unity implementation uses small procedural uGUI glyphs; precise SVG path equivalence is **NOT TESTED**.

## Not inspected

Free Drive HUD, Crash Lab, Garage, Damage Report, Settings and any other screen were not inspected in this pass. No properties for those screens are inferred here.
