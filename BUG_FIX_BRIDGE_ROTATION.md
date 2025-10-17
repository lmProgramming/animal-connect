# Bug Fix: Bridge Continuous Rotation Error

## Problem

When a Bridge tile (or X-Intersection tile) is continuously rotated by clicking/tapping, an error is thrown after a few rotations.

## Root Cause

The `CreateRotateMove` method in `TileInputHandler.cs` was using a hardcoded modulo 4 to wrap rotation values:

```csharp
var newRotation = (currentTile.Value.Rotation + 1) % 4;
```

This works for tiles that have 4 rotation states (Curve, Intersection), but fails for tiles with fewer rotation states:

- **Bridge**: Only 2 rotations (0 and 1)
- **X-Intersection**: Only 1 rotation (0)
- **TwoCurves**: Only 2 rotations (0 and 1)

### Example of the Bug

For a Bridge tile at rotation 1:

1. First click: rotation 1 → (1 + 1) % 4 = 2 ❌ **INVALID** (Bridge only supports 0 and 1)
2. Error thrown in `MoveProcessor.ApplyRotation`:

   ```
   Rotation 2 is invalid for tile type Bridge (max: 2)
   ```

## Solution

Use the tile's actual maximum rotations from `GetMaxRotations()` instead of hardcoded 4:

```csharp
// Get max rotations for this tile type and wrap around correctly
int maxRotations = currentTile.Value.GetMaxRotations();
var newRotation = (currentTile.Value.Rotation + 1) % maxRotations;
```

## Files Changed

### 1. `/Assets/Scripts/Input/TileInputHandler.cs`

**Changed**: `CreateRotateMove()` method

- Added call to `currentTile.Value.GetMaxRotations()` to get the correct maximum rotations
- Changed modulo operation from `% 4` to `% maxRotations`

### 2. `/Assets/Tests/Core/Logic/MoveProcessorTests.cs`

**Added**: Two new test cases

- `ProcessMove_BridgeContinuousRotation_HandlesMaxRotationsCorrectly()`: Tests continuous rotation of Bridge tiles
- `ProcessMove_XIntersectionContinuousRotation_HandlesMaxRotationsCorrectly()`: Tests rotation of X-Intersection tiles

## Tile Rotation Limits Reference

| Tile Type      | Max Rotations | Valid Rotation Values |
|---------------|---------------|----------------------|
| Curve         | 4             | 0, 1, 2, 3          |
| TwoCurves     | 2             | 0, 1                |
| Intersection  | 4             | 0, 1, 2, 3          |
| XIntersection | 1             | 0                   |
| Bridge        | 2             | 0, 1                |

## Testing

The fix ensures that:

1. Bridge tiles correctly cycle between rotations 0 and 1
2. X-Intersection tiles remain at rotation 0 (no visual change)
3. TwoCurves tiles correctly cycle between rotations 0 and 1
4. All other tiles continue to work with 4 rotation states

## Verification

Run the new unit tests to verify the fix:

- `ProcessMove_BridgeContinuousRotation_HandlesMaxRotationsCorrectly`
- `ProcessMove_XIntersectionContinuousRotation_HandlesMaxRotationsCorrectly`

Or test manually in the game by continuously clicking on a Bridge or X-Intersection tile.
