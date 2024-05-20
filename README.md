
# Editor Tools

Various editor tools and editor windows.

# Installing

Head to my [VCC Listing](https://jansharp.github.io/vrc/vcclisting.xhtml) and follow the instructions there.

# Tools

<!-- cSpell:ignore occluders, occludees -->

- UI Color Changer to update colors of selectable UIs
  - Uses the `Normal Color` of the selected UI components, like buttons, to update all the other colors like highlighted, pressed and so on
  - All currently selected GameObjects in the hierarchy are affected, if they have a Selectable UI component
  - Yes, Selectable is a base class derived by several other components
- OcclusionVisibilityWindow to help visualize which objects are occluders and occludees (and static batchers)
- BulkReplaceWindow to quickly replace many objects with another prefab
- Select Particle Systems based on their culling mode
- Save Pending Asset Changes is a tiny editor script to write any asset file changes unity has cached in memory to the drive
- Create Parent menu item next to Create Empty, creating an empty game object which becomes the parent of all selected objects
- Selection Stage editor window to effectively have a second much more manually controlled list of selected objects
- Log Component Class names to quickly get the component names to be able to search for all objects with that component in the scene
