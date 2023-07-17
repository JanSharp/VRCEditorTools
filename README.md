
# JanSharp Common

Common files and scripts are, well, commonly used across multiple other scripts. It's like the core.

# Installing

Head to my [VCC Listing](https://jansharp.github.io/vrc/vcclisting.xhtml) and follow the instructions there.

# Features

## Editor

- OnBuildUtil, allowing multiple registrations and a defined order
  - In an editor script, mark a class with the [InitializeOnLoad](https://docs.unity3d.com/ScriptReference/InitializeOnLoadAttribute.html) attribute
  - In its static constructor call `OnBuildUtil.RegisterType<T>(...)` passing in a function with type T as a parameter and returning a boolean
  - The registered callbacks will run on every component of the registered type T in the current scene when entering play mode and when VRChat builds the project
  - If your callback returns false it indicates failure and prevents VRChat from publishing. Entering play mode does not get aborted however
    - You should always `Debug.LogError` the reason for the abort
  - Since the RegisterType method takes an optional order argument, it is supported to register the same type twice but with different order
    - The order means that all lowest order registrations for all types are handled first, then the next order and so on
- EditorUtil to help working with SerializedObjects and SerializedProperties and other editor only utils
- UI Color Changer to update colors of selectable UIs
  - Uses the `Normal Color` of the selected UI components, like buttons, to update all the other colors like highlighted, pressed and so on
  - All currently selected GameObjects in the hierarchy are affected, if they have a Selectable UI component
  - Yes, Selectable is a base class derived by several other components

## Runtime

- UpdateManager to register and deregister any behaviour for a CustomUpdate function at runtime (runs on Update)
  - Udon behaviours used with this require a `CustomUpdate` event (public method) and an int variable `customUpdateInternalIndex`
  - Registering an already registered behaviour is supported, it does nothing, same goes for deregistering an already deregistered behaviour
- UpdateManager prefab
- LocalEventOnInteract to send a custom event to any Udon behaviour locally
- LocalToggleOnInteract to toggle a single GameObject locally

# Ideas

Honestly I'm not sure. It feels like this is missing a lot of stuff, but then I'm not sure what really belongs in here. But I suppose the editor scripting this provides is already decent. But still, I'm not marking it as `1.0.0` yet.
