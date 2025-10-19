using DebugTools;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    ///     UI toggle button for the path point debug visualizer.
    ///     Add this to a UI Button to toggle debug visualization during gameplay.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PathDebugToggleButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PathPointDebugVisualizer debugVisualizer;
        
        [Header("Button Text (Optional)")]
        [SerializeField] private Text buttonText;
        [SerializeField] private string onText = "Hide Path Debug";
        [SerializeField] private string offText = "Show Path Debug";
        
        private Button _button;
        
        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnButtonClicked);
            
            // Try to find debugVisualizer if not assigned
            if (debugVisualizer == null)
            {
                debugVisualizer = FindObjectOfType<PathPointDebugVisualizer>();
            }
            
            UpdateButtonText();
        }
        
        private void OnButtonClicked()
        {
            if (debugVisualizer != null)
            {
                debugVisualizer.ToggleDebug();
                UpdateButtonText();
            }
            else
            {
                Debug.LogWarning("PathDebugToggleButton: PathPointDebugVisualizer not found!");
            }
        }
        
        private void UpdateButtonText()
        {
            if (buttonText == null) return;
            
            // This requires the debugVisualizer to have a public getter for showDebug
            // For now, we'll just toggle the text
            buttonText.text = buttonText.text == onText ? offText : onText;
        }
    }
}
