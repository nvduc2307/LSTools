# InstallRebarBeamV2 - Optimization and Refactoring Report

## Scope

This iteration keeps the installed-rebar behavior and the atomic transaction introduced by the port-parity work, while reducing repeated Revit API access, repeated preview rendering, and large mixed-responsibility source files.

Runtime behavior still needs to be validated inside Revit with representative single-span and multi-span beam models. The compile matrix below verifies API compatibility only; it is not a substitute for model-level testing.

## Implemented architecture

```text
InstallRebarBeamV2/
|- Application/
|  |- Commands/                 transaction and post-processing pipeline
|  |- RebarExecutionContext     document, axes, host and bar-type cache
|  |- RebarExecutionMetrics     per-stage timing
|  `- RebarInstallationResult   typed output for every rebar group
|- Domain/Plans/
|  `- MainBarCreationPlan       Revit-write input for main bars
|- Geometry/
|  |- MainBars/                 main-bar planning and geometry
|  `- Auxiliary/                side-bar and dantory geometry
|- Revit/Writers/
|  |- MainBarRebarWriter
|  `- Stirrups/                 four stirrup writer groups
|- UI/Preview/
|  |- PreviewRefreshCoordinator debounce and partial invalidation
|  |- InstallRebarBeamPreviewViewModel
|  `- Canvas/                   main, auxiliary, stirrup and primitive renderers
`- Support/Legacy/              compatibility helpers retained from RimT
```

## Performance changes

### Revit creation pipeline

- A single `RebarExecutionContext` is created per run.
- One temporary host is reused for all created bar groups instead of creating a host for each group.
- Rebar types are indexed once by name and resolved through a strict dictionary lookup.
- Main bars now follow `Planner -> MainBarCreationPlan -> Writer`; geometry calculation and Revit element creation are timed separately.
- The created rebar collection is materialized once for assembly creation, metadata lookup, and rehosting.
- The real target host id is resolved once and carried in the installation result.

### Geometry and opening processing

- Main-bar point controls are cached per beam member for each level/group calculation.
- Beam and sub-beam lookups use id dictionaries instead of repeated linear searches.
- The maximum main-bar quantity handles empty and one-bar layouts without invalid spacing math.
- Opening processing reuses the cached `BoxElement`, prefilters candidate stirrups, groups source stirrups once per beam, caches corrected lengths and schema payloads, and deduplicates deletes by element id.

### Parameters and metadata

- Required shared-parameter bindings and existing shared parameters are scanned once per ensure operation instead of once per parameter.
- Rebar metadata is written against the already-created `Rebar` objects, avoiding document lookups by unique id.
- Repeated type-parameter and `BeamRebarInfo` construction blocks are consolidated into shared helpers.

### Preview

- Quantity changes are debounced by 100 ms.
- Main-bar and side-bar previews are invalidated independently.
- Multiple rapid changes are coalesced into one UI-thread redraw.
- Initial view loading remains synchronous so the dialog never opens with a deliberately delayed first preview.

## Structural changes

- `InstallRebarBeamInModelService.cs`: approximately 1,646 lines before refactoring; now an orchestration and auxiliary-writer file, with main/stirrup writers split out.
- `DrawRebarBeamInCanvasService.cs`: 2,831 lines before refactoring; now 248 orchestration lines plus four responsibility-based partial renderer files.
- `InstallRebarBeamV2ViewModel.cs`: 828 lines before refactoring; now 256 lines, with transaction commands and preview wiring in separate partial files.
- `SubInstallRebarBeamInModelService.cs`: approximately 1,311 lines before refactoring; now 594 lines, with main and auxiliary geometry in dedicated files.

Partial classes are used only as a compatibility seam: XAML bindings, generated relay commands, dependency-injection registrations, and public service contracts remain stable while the physical files are separated.

## Instrumentation

The command writes a single `Debug` timing summary after post-processing. It contains stages such as:

- `main.top.1.plan` / `main.top.1.write`
- `side`, `dantory`
- `stirrup.main` and the three secondary-stirrup groups
- `metadata.type`, `metadata.schema`
- `assembly.create`, `assembly.metadata`
- `rehost`, `opening`, `segments`

These timings are intentionally collected inside the same transaction as production execution. They can be compared across the same model/configuration before deciding where another optimization is justified.

## Verification completed

| Target | Result | Warnings | Errors |
|---|---:|---:|---:|
| Debug R24 | Passed | 202 | 0 |
| Debug R25 | Passed | 250 | 0 |
| Debug R26 | Passed | 249 | 0 |

The warnings are existing project/package and nullable warnings; this change does not introduce a new warning in the R26 comparison build.

## Required Revit runtime regression

Before release, run the following cases in each supported Revit version used by the team:

1. One physical beam member, all six main-bar groups enabled.
2. Multi-span beam chain with different start/mid/end quantities.
3. Quantity equal to zero and quantity equal to one for every main-bar level.
4. Side bars and dantory bars enabled and disabled independently.
5. All four stirrup groups, including vertical and horizontal secondary stirrups.
6. No opening, one circular opening, and multiple openings near adjacent spans.
7. Successful rehost: no temporary DirectShape remains after commit.
8. Forced failure: transaction rolls back all rebars, assembly, metadata, and the temporary host.
9. Rapid UI quantity edits: final preview matches the last value and does not lag behind selection changes.
10. Compare rebar count, curves, bar type, host id, assembly membership, shared parameters, and schema payload against the original RimT command.

## Deliberately deferred

- No speculative multithreading was added; Revit document and element APIs must remain on the Revit API thread.
- Stirrup geometry was physically separated from the orchestration service, but a full immutable creation-plan model for every stirrup subtype is deferred until the runtime regression fixtures are available.
- Existing canvas drawing algorithms were reorganized and debounced, not mathematically rewritten. Their visual output should first be snapshotted in Revit before deeper renderer changes.
