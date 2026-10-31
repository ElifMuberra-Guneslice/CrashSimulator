# Figma UI assets

The Figma Make file specifies Rajdhani for display and Inter for UI, with Turkish Latin Extended glyph coverage. The corresponding font files are in `Fonts/` under their SIL Open Font License texts. `Assets/Scripts/Editor/FigmaUiDesignSystemImporter.cs` binds these assets to the existing uGUI screens and applies the Figma palette.

No React/Vite source is embedded in Unity. The existing screens remain native uGUI objects connected to the vehicle and simulation components.
