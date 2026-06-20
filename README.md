# Titration Endpoint Color System

A reusable Unity component and prefab for simulating an acid-into-NaOH titration. It tracks the acid volume delivered by a burette, detects the equivalence point, changes a phenolphthalein-style indicator from pink to colorless, locks the final burette volume, and exposes the result for NaOH standardization.

## Included assets

- `Assets/Prefabs/TitrationEndpointDetector.prefab` — ready-to-use detector prefab.
- `Assets/Scripts/Titration/TitrationEndpointDetector.cs` — endpoint calculation and color-control component.

## Behavior

When standard acid is added to NaOH containing phenolphthalein:

1. The solution starts pink while NaOH is in excess.
2. The burette reports the total acid volume after every drop.
3. The detector compares acid equivalents delivered with the initial NaOH equivalents.
4. On the first reading at or beyond equivalence, it changes the liquid to colorless.
5. The endpoint flag and final acid volume are locked. Later drops cannot reverse the color or change the recorded result.

There is no animation in this module; the two indicator colors are swapped immediately.

## Requirements

- Unity 6 or another Unity version that supports the C# syntax used by the script.
- A flask liquid GameObject with a `Renderer` and a material exposing `_BaseColor` (URP) or `_Color`.
- A separate burette system that reports cumulative acid volume in millilitres.

## Installation

Copy the repository's `Assets` folder into the root of your Unity project. Unity will import the script and prefab automatically.

Alternatively, copy these two folders while preserving their `.meta` files:

```text
Assets/Prefabs
Assets/Scripts/Titration
```

## Unity setup

1. Drag `TitrationEndpointDetector.prefab` into the scene.
2. Select the prefab instance.
3. Assign the flask liquid's `Renderer` to **Liquid Renderer**.
4. Enter the standard acid concentration in mol/L.
5. Enter the initial NaOH volume in mL.
6. Enter the nominal NaOH concentration used by the simulation to determine equivalence.
7. Keep both equivalent factors at `1` for a monoprotic acid such as HCl reacting with NaOH.
8. Adjust **Base Excess Pink** and **Endpoint Colorless** if a different visual style is required.
9. Connect the optional **On Endpoint Reached** UnityEvent if another scene object should react immediately.

The prefab defaults are 0.1 mol/L acid, 10.0 mL NaOH, 0.1 mol/L nominal NaOH, and a 1:1 reaction. These values produce an endpoint at 10.0 mL of acid.

If the color's alpha value should make the liquid transparent, configure the liquid material's surface type/blending to support transparency. The component changes the color but does not change shader rendering settings.

## Burette integration

Call `CheckEndpoint` after each drop using the **total delivered acid volume**, not the size of the latest drop:

```csharp
using UnityEngine;

public class Burette : MonoBehaviour
{
    [SerializeField] private TitrationEndpointDetector endpointDetector;

    private float totalAcidVolumeMl;

    private void ReportDispensedDrop(float dropVolumeMl)
    {
        totalAcidVolumeMl += dropVolumeMl;

        if (endpointDetector.CheckEndpoint(totalAcidVolumeMl))
        {
            StopDispensing();
        }
    }

    private void StopDispensing()
    {
        // Disable the burette valve/drop generation here.
    }
}
```

The Burette can also poll `EndpointReached()` or the `IsEndpointReached` property after reporting a drop. For event-driven integration, subscribe to `EndpointReachedEvent` or configure the serialized UnityEvent in the Inspector.

## Public API

| Member | Purpose |
| --- | --- |
| `CheckEndpoint(float totalAcidVolumeAddedMl)` | Updates reaction progress and returns `true` at and after the endpoint. |
| `EndpointReached()` | Burette-friendly polling method. |
| `IsEndpointReached` | Read-only endpoint property. |
| `GetFinalAcidVolume()` | Returns the locked endpoint volume, or `0` before the endpoint. |
| `CalculateStandardizedNaohConcentration()` | Calculates NaOH concentration from the locked endpoint result. |
| `ResetTitration()` | Clears the result and restores the pink starting state. |
| `AcidVolumeAddedMl` | Most recent cumulative acid volume accepted before locking. |
| `FinalAcidVolumeMl` | Read-only locked endpoint volume. |
| `EndpointReachedEvent` | C# event fired once when the endpoint is first reached. |

## Chemistry used

The detector works in reactive equivalents, allowing simple stoichiometry factors:

```text
acid equivalents = acid concentration x acid volume (L) x acid equivalent factor
base equivalents = nominal NaOH concentration x initial NaOH volume (L) x base equivalent factor
endpoint reached when acid equivalents >= base equivalents
```

After the endpoint, the standardized NaOH concentration is:

```text
NaOH concentration =
    acid concentration x final acid volume x acid equivalent factor
    --------------------------------------------------------------
    initial NaOH volume x base equivalent factor
```

For HCl + NaOH, both equivalent factors are `1`.

## Verification example

The supplied defaults were checked with a 1:1 reaction:

- At 9.9 mL acid: endpoint is `false`.
- At 10.0 mL acid: endpoint becomes `true`.
- The locked final volume is 10.0 mL.
- The calculated NaOH concentration is 0.100 mol/L.
- Reporting 10.1 mL afterward leaves the locked volume at 10.0 mL.

## Simulation notes

- The endpoint calculation uses the configured nominal NaOH concentration. In a real standardization experiment, the chemical indicator responds to pH rather than knowing the unknown concentration; this component uses the nominal value to model that response.
- The color transition is simplified to occur at equivalence. Real phenolphthalein changes over a pH range.
- Because burette input arrives in discrete drops, the locked volume is the first reported total that crosses equivalence and may include a small overshoot.
- This repository does not include drop generation, liquid-level animation, or a complete burette model.
