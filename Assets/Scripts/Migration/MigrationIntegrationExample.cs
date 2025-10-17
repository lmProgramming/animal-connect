using AnimalConnect.Managers;
using Grid;
using Migration;
using UnityEngine;

/// <summary>
///     Example of how to integrate the migration system into your game.
///     This shows the minimal setup needed to enable parallel system validation.
/// </summary>
public class MigrationIntegrationExample : MonoBehaviour
{
    [Header("Required References - Assign in Inspector")]
    [SerializeField] private MyGrid oldGrid;

    [SerializeField] private GameManager oldGameManager;
    [SerializeField] private GameStateManager newStateManager;

    [Header("Settings")]
    [SerializeField] private bool useNewSystemByDefault;

    [SerializeField] private bool enableValidation = true;
    private SystemAdapter adapter;

    [Header("Migration Components - Created Automatically")]
    private MigrationValidator validator;

    private void Awake()
    {
        SetupMigrationSystem();
    }

    private void Start()
    {
        InitializeSystems();
    }

    /// <summary>
    ///     Step 1: Setup migration components
    /// </summary>
    private void SetupMigrationSystem()
    {
        // Create validator
        validator = gameObject.AddComponent<MigrationValidator>();

        // Create adapter
        adapter = gameObject.AddComponent<SystemAdapter>();

        // Configure components through reflection (since fields are private/serialized)
        // In practice, you'd assign these in the Inspector
        Debug.Log("[Migration] Migration system components created");
        Debug.Log("[Migration] Please assign references in Inspector:");
        Debug.Log("  - MigrationValidator: oldGrid, oldGameManager, newStateManager");
        Debug.Log("  - SystemAdapter: same references + migrationValidator");
    }

    /// <summary>
    ///     Step 2: Initialize both systems
    /// </summary>
    private void InitializeSystems()
    {
        if (adapter != null)
        {
            // Set which system to use
            adapter.UseNewSystem = useNewSystemByDefault;

            // Initialize (will initialize the active system)
            adapter.Initialize();

            Debug.Log($"[Migration] Systems initialized. Using {(useNewSystemByDefault ? "NEW" : "OLD")} system.");
        }
    }

    /// <summary>
    ///     Step 3: Use adapter for all game actions
    /// </summary>
    public void OnTileRotated(int slotIndex)
    {
        if (adapter != null) adapter.RotateTile(slotIndex);
    }

    public void OnTilesSwapped(int slot1, int slot2)
    {
        if (adapter != null) adapter.SwapTiles(slot1, slot2);
    }

    /// <summary>
    ///     Example: Switch systems at runtime
    /// </summary>
    [ContextMenu("Switch to New System")]
    public void SwitchToNewSystem()
    {
        if (adapter != null)
        {
            adapter.SwitchToNewSystem();
            Debug.Log("[Migration] Switched to NEW system");
        }
    }

    [ContextMenu("Switch to Old System")]
    public void SwitchToOldSystem()
    {
        if (adapter != null)
        {
            adapter.SwitchToOldSystem();
            Debug.Log("[Migration] Switched to OLD system");
        }
    }

    /// <summary>
    ///     Example: Check validation stats
    /// </summary>
    [ContextMenu("Print Validation Stats")]
    public void PrintValidationStats()
    {
        if (validator != null) Debug.Log(validator.GetValidationStats());
    }
}