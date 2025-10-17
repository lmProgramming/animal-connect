using UnityEngine;

/// <summary>
/// Visual representation of an entity (animal).
/// TODO: Update to work with new Core system.
/// Currently disabled - entities are managed in QuestData.
/// </summary>
public class EntityVisual : MonoBehaviour
{
    [SerializeField] private string entityName;
    [SerializeField] private int entityId;

    // TODO: Integrate with new PathNetworkState
    // Old Entity/PathPoint system has been removed
    
    private void Awake()
    {
        // Disabled until integrated with new system
        Debug.LogWarning("EntityVisual: Old system disabled. Needs integration with Core.Models.QuestData");
    }
}//