
# Changelog

## [1.3.7] - 2025-07-05

### Added

- Add basic enum support to PropertySearchWindow ([`8dfb04b`](https://github.com/JanSharp/VRCEditorTools/commit/8dfb04bcd41b6d3ef7fae8bfa6a09e17e78b46d5))
- Add Gradient support to PropertySearchWindow ([`93baf48`](https://github.com/JanSharp/VRCEditorTools/commit/93baf48a790821497362db7e5deaf1a10811415c))
- Add Hash128 support to PropertySearchWindow, new with unity 2022 ([`df2c883`](https://github.com/JanSharp/VRCEditorTools/commit/df2c883db3b0129eef63c5cf8559fb466606f00a))

## [1.3.6] - 2025-05-28

### Changed

- Update readme ([`7bfad9b`](https://github.com/JanSharp/VRCEditorTools/commit/7bfad9b9973a0c74c602791664bcfd2539845bd2))

### Fixed

- Fix move prev selection not ignoring prefab assets ([`4172f41`](https://github.com/JanSharp/VRCEditorTools/commit/4172f414ef43eebf4ecab62f25796f94bfad1007))

## [1.3.5] - 2025-05-27

### Added

- Add Move Prev Selection Into Here context menu item ([`1000c18`](https://github.com/JanSharp/VRCEditorTools/commit/1000c18c2cea3e0d00421759001b90cb99b60152))

## [1.3.4] - 2025-05-25

### Added

- Add WIP tool to find materials using given texture ([`37dfe87`](https://github.com/JanSharp/VRCEditorTools/commit/37dfe87e9e5c8c6342e2516266689b186fdeff74))
- Make WIP tool searches open results stage even for 0 results ([`330377d`](https://github.com/JanSharp/VRCEditorTools/commit/330377d4c6c68540479835dc00872808e732c23d))

### Fixed

- Fix selection stage sometimes moving scene view on asset ping ([`a4b2a6c`](https://github.com/JanSharp/VRCEditorTools/commit/a4b2a6cdb5b788305deab28c87fcffa3675fac4e))

## [1.3.3] - 2025-05-24

### Changed

- Retain selected objects in SelectionStage on sort or removal ([`4e8a31d`](https://github.com/JanSharp/VRCEditorTools/commit/4e8a31d70d522457521a69a24c7bf0a80227017a))
- Make SelectionStage refresh on renames ([`d74ae83`](https://github.com/JanSharp/VRCEditorTools/commit/d74ae8329c1282995fb31a2d910b851b4ecee855))
- Move overwrite buttons all the way to the top ([`e7a8faa`](https://github.com/JanSharp/VRCEditorTools/commit/e7a8faaca0e2d2c6c8fc0960efaf27a123607685))

### Added

- Add WIP tools editor window with a prefab instance search ([`c4c79af`](https://github.com/JanSharp/VRCEditorTools/commit/c4c79affea7c42f35e69a37f63e0007696a82910))
- Add support for non game objects in SelectionStage ([`b70fb7b`](https://github.com/JanSharp/VRCEditorTools/commit/b70fb7b254f553a73e5807fd86d44c8908e161a0))
- Add options for how submit should behave in stage ([`16fc030`](https://github.com/JanSharp/VRCEditorTools/commit/16fc03053c92cca1a3d28be32f1d166f124fc515))
- Add hierarchy/asset path sort to SelectionStage ([`9f9b845`](https://github.com/JanSharp/VRCEditorTools/commit/9f9b84546b8cc66639f79e26176f38ff42e21af6), [`8b31548`](https://github.com/JanSharp/VRCEditorTools/commit/8b3154864bd55bb5786b56f7c1289fe141ccde95))
- Add hierarchy/asset path tooltip in SelectionStage ([`86519e8`](https://github.com/JanSharp/VRCEditorTools/commit/86519e85402ba9efc7e5829158b6e4c1bbd6dbd0), [`c89efca`](https://github.com/JanSharp/VRCEditorTools/commit/c89efca82c0d6e97055d6b6a8d8719ef301e2634))
- Add tooltips to most buttons in SelectionStage ([`ba35dfb`](https://github.com/JanSharp/VRCEditorTools/commit/ba35dfb827d91eb9b331862eb77daf669b6a1589))

### Removed

- Remove Create Parent because Unity 2022 already has it ([`3ac7039`](https://github.com/JanSharp/VRCEditorTools/commit/3ac7039500f06657baacb727e4eef6f228ad0ffb))

### Fixed

- Fix Selection Stage buttons being moved off screen ([`7f61400`](https://github.com/JanSharp/VRCEditorTools/commit/7f614001ef8dc362621c1a9d3a089023ef75b060))

## [1.3.2] - 2025-04-26

### Fixed

- Fix compiler error in selection stage window ([`96f17b7`](https://github.com/JanSharp/VRCEditorTools/commit/96f17b7c59a749fc69316646029cd41acf27f529))

## [1.3.1] - 2024-07-27

### Fixed

- Fix static flags helper window ignoring inactive children ([`6682280`](https://github.com/JanSharp/VRCEditorTools/commit/66822805b7c58b12e2cc4e1f4af656f1f03078fb))

## [1.3.0] - 2024-07-12

_Make sure to close the Occlusion Visibility Window before updating, otherwise Unity may throw a fit._

### Added

- Add Static Flags Helper Window to help with all flags, not just occlusion and batching ([`962660a`](https://github.com/JanSharp/VRCEditorTools/commit/962660a4ff2fe81de95814e36cfc9f6a90424394))

### Removed

- **Breaking:** Remove Occlusion Visibility Window ([`962660a`](https://github.com/JanSharp/VRCEditorTools/commit/962660a4ff2fe81de95814e36cfc9f6a90424394))

### Fixed

- Remove Runtime.meta file from vpm packages ([`c1e3466`](https://github.com/JanSharp/VRCEditorTools/commit/c1e3466cb3c31b4a3992363d47e61f425245d03a))

## [1.2.0] - 2024-05-24

### Changed

- Readd check for immutable prefabs in Create Parent ([`9ef7813`](https://github.com/JanSharp/VRCEditorTools/commit/9ef781361bbb71659b5bdf67695c1edd7a25c35c))
- Update count in Bulk Replace on selection change ([`4d619cc`](https://github.com/JanSharp/VRCEditorTools/commit/4d619ccb76e5fb233793fac8dac2b516ceea32d0))
- Add foldout to bulk replace window to hide bloat ([`a71cdd1`](https://github.com/JanSharp/VRCEditorTools/commit/a71cdd1521febe0b65cf0b9170c779bda9782da4))

### Added

- Add References Window to find incoming references ([`c264594`](https://github.com/JanSharp/VRCEditorTools/commit/c26459486823d54fe5122bfc5eadaee9d905c056))
- Add Keep Original Name checkbox to Bulk Replace ([`44bebb7`](https://github.com/JanSharp/VRCEditorTools/commit/44bebb73c9c3dc941bc273029f03b9c01325408f))

### Fixed

- **Breaking:** Fix inactive children getting ignored in PropertySearchWindow, BulkLODGroups and SelectParticleSystems ([`9634ef8`](https://github.com/JanSharp/VRCEditorTools/commit/9634ef8ddc0a92e1c73336af343f64cd0141f7a7))

## [1.1.2] - 2024-05-22

### Changed

- Remove Create Parent prefab restrictions entirely ([`a61bbff`](https://github.com/JanSharp/VRCEditorTools/commit/a61bbff9740b2846d531412dadc07b6f7fd152aa))

## [1.1.1] - 2024-05-22

### Changed

- Loosen restriction on when Create Parent is usable, only children of immutable prefabs get rejected ([`cb51dd7`](https://github.com/JanSharp/VRCEditorTools/commit/cb51dd7d2b5b6a436553d102be2df16bedf81329))

## [1.1.0] - 2024-05-21

_Make sure to close The Bulk Replace Window before updating, due to a class name change._

### Changed

- **Breaking:** Rename window classes to have the Window postfix ([`4018fa5`](https://github.com/JanSharp/VRCEditorTools/commit/4018fa55bf10d12b2fa6d74d8345cb09e5e60470))
- **Breaking:** Rename Save Pending Asset Changes to Save Cached Unity Asset Changes for clarity ([`05a0920`](https://github.com/JanSharp/VRCEditorTools/commit/05a092024f72549d6c4ee96879884c5196ba6833))
- Move all tools under Tools/JanSharp and rearrange them ([`3c01b56`](https://github.com/JanSharp/VRCEditorTools/commit/3c01b566b96fda508e702154daa0f3cafabfabf3), [`accc270`](https://github.com/JanSharp/VRCEditorTools/commit/accc27060b69f451320eee64af4f98384da77768), [`a2441b2`](https://github.com/JanSharp/VRCEditorTools/commit/a2441b2ec47825f78351dd011f2d684e5adc8128))

### Added

- Add Selection Stage editor window ([`e7d1d4c`](https://github.com/JanSharp/VRCEditorTools/commit/e7d1d4c385f76a24f84c8f5b778031f07dbbf0d5), [`a73b30f`](https://github.com/JanSharp/VRCEditorTools/commit/a73b30f679b05cbff222621a57f789847c585984), [`0857508`](https://github.com/JanSharp/VRCEditorTools/commit/08575083da6a08f1a8d34467c3f4eec01eade29d), [`a09ac61`](https://github.com/JanSharp/VRCEditorTools/commit/a09ac6101013f1c17eb0eeb2ffc80168d681a895), [`2a8cb46`](https://github.com/JanSharp/VRCEditorTools/commit/2a8cb466312515f6542780357aeb5cc0ecfd3bc9), [`2a253c7`](https://github.com/JanSharp/VRCEditorTools/commit/2a253c767f50b72736158b76e242ca97699a9230), [`cca7d3e`](https://github.com/JanSharp/VRCEditorTools/commit/cca7d3e62f77ea795354a5d0c674046c9089bd70), [`3d9090d`](https://github.com/JanSharp/VRCEditorTools/commit/3d9090df66fa797a4874545ab10c9a1ad76928f0))
- Add Property Search window to find components where a given property has some value ([`262bbb7`](https://github.com/JanSharp/VRCEditorTools/commit/262bbb7f52760c2c884123a9862638aa8bf7f796), [`6332a6e`](https://github.com/JanSharp/VRCEditorTools/commit/6332a6e1c584391e63d6f350796e17bbcfc50cde), [`760beb7`](https://github.com/JanSharp/VRCEditorTools/commit/760beb79e2bd6714a884d2f8d75ab4b100ded276), [`cca7d3e`](https://github.com/JanSharp/VRCEditorTools/commit/cca7d3e62f77ea795354a5d0c674046c9089bd70))
- Add generation of basic culling LOD Groups and context menu items to copy paste cull percentages on multiple LOD Groups at once ([`7514075`](https://github.com/JanSharp/VRCEditorTools/commit/75140753b7d4ec06f03fc03e9ed8290eb157e5d6))
- Add menu items to show/hide all/selected game objects and one to invert visibility ([`1e54005`](https://github.com/JanSharp/VRCEditorTools/commit/1e5400589b2831c6b5145fc48f6477cd17e6820d), [`a2441b2`](https://github.com/JanSharp/VRCEditorTools/commit/a2441b2ec47825f78351dd011f2d684e5adc8128))
- Add Print Selected Count menu item ([`4daef4a`](https://github.com/JanSharp/VRCEditorTools/commit/4daef4aa80db918a7cb882067a4d4f0e140c2fca))
- Add Print Component Class Names menu item ([`da6428d`](https://github.com/JanSharp/VRCEditorTools/commit/da6428d7d0a7733c484e4d250e922ed5287b2b0a), [`bf84889`](https://github.com/JanSharp/VRCEditorTools/commit/bf84889a683353e43f762d615a7884329b47af39), [`17ec4f6`](https://github.com/JanSharp/VRCEditorTools/commit/17ec4f63e529a637462e50704384612c801638a0))

## [1.0.5] - 2024-05-17

### Changed

- Improve readme line for Save Pending Changes ([`38faa0c`](https://github.com/JanSharp/VRCEditorTools/commit/38faa0c7d7a6e536fa62ee108de4fb53e100ce5b))

### Added

- Add Create Parent menu item next to Create Empty ([`7b90b07`](https://github.com/JanSharp/VRCEditorTools/commit/7b90b070ad1f6d0f8e6d7d0f73af182b4160537f))

## [1.0.4] - 2024-05-13

### Added

- Add editor script to save pending asset changes to the drive ([`368b962`](https://github.com/JanSharp/VRCEditorTools/commit/368b962cb237c536a2d3c7ac3d5d2d2cc57bd663))

## [1.0.3] - 2024-04-24

### Changed

- Write selected particle system count to log ([`d1abc5e`](https://github.com/JanSharp/VRCEditorTools/commit/d1abc5ecd910a9167c0d81fe51bf7e081d6ba61b))

## [1.0.2] - 2024-04-24

### Added

- Add tool to select Particle Systems based on their culling mode ([`088553e`](https://github.com/JanSharp/VRCEditorTools/commit/088553e348a4c41bba635c96bf1e251d30295cc5))

### Fixed

- Fix first 2 commit hashes in changelog ([`f959eaa`](https://github.com/JanSharp/VRCEditorTools/commit/f959eaa5e92d01658bf20f312c1ba16b5a04a883))

## [1.0.1] - 2024-03-16

### Fixed

- Fix keep children only keeping half of them ([`a78f27d`](https://github.com/JanSharp/VRCEditorTools/commit/a78f27d89c4ac0621303e0371cd379c6a068db65))

## [1.0.0] - 2024-03-16

_This package got split off of [com.jansharp.common v0.2.1](https://github.com/JanSharp/VRCJanSharpCommon/blob/v0.2.1/CHANGELOG.md), see there for prior history._

### Added

- Add bulk replace tool ([`5513871`](https://github.com/JanSharp/VRCEditorTools/commit/55138716cbe527f956ae90b1a8b5a17ae1a21cef))
- Add UI Color Changer and Occlusion Visibility Window by splitting com.jansharp.common ([`2d7f2f5`](https://github.com/JanSharp/VRCEditorTools/commit/2d7f2f5c36f5f492514b5540125de2d31882b1fd))

[1.3.7]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.3.7
[1.3.6]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.3.6
[1.3.5]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.3.5
[1.3.4]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.3.4
[1.3.3]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.3.3
[1.3.2]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.3.2
[1.3.1]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.3.1
[1.3.0]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.3.0
[1.2.0]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.2.0
[1.1.2]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.1.2
[1.1.1]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.1.1
[1.1.0]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.1.0
[1.0.5]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.0.5
[1.0.4]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.0.4
[1.0.3]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.0.3
[1.0.2]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.0.2
[1.0.1]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.0.1
[1.0.0]: https://github.com/JanSharp/VRCEditorTools/releases/tag/v1.0.0
