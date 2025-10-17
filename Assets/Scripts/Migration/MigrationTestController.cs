using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Migration
{
    /// <summary>
    /// Controller for testing and demonstrating the migration between old and new systems.
    /// Provides UI controls and monitoring for the migration process.
    /// </summary>
    public class MigrationTestController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SystemAdapter _systemAdapter;
        [SerializeField] private MigrationValidator _migrationValidator;
        
        [Header("UI (Optional)")]
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _statsText;
        [SerializeField] private Button _switchSystemButton;
        [SerializeField] private Button _validateButton;
        [SerializeField] private Button _resetStatsButton;
        
        [Header("Auto-Test Settings")]
        [SerializeField] private bool _runAutoTests = false;
        [SerializeField] private int _autoTestMoves = 10;
        [SerializeField] private float _autoTestDelay = 1f;
        
        private float _nextAutoTestTime;
        private int _autoTestsCompleted = 0;
        
        private void Start()
        {
            SetupUI();
            
            if (_systemAdapter != null)
            {
                _systemAdapter.Initialize();
                _systemAdapter.OnGameInitialized += OnGameInitialized;
                _systemAdapter.OnMoveCompleted += OnMoveCompleted;
                _systemAdapter.OnGameWon += OnGameWon;
                _systemAdapter.OnValidationFailed += OnValidationFailed;
            }
            
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            if (_systemAdapter != null)
            {
                _systemAdapter.OnGameInitialized -= OnGameInitialized;
                _systemAdapter.OnMoveCompleted -= OnMoveCompleted;
                _systemAdapter.OnGameWon -= OnGameWon;
                _systemAdapter.OnValidationFailed -= OnValidationFailed;
            }
        }
        
        private void Update()
        {
            if (_runAutoTests && _autoTestsCompleted < _autoTestMoves)
            {
                if (Time.time >= _nextAutoTestTime)
                {
                    PerformRandomMove();
                    _nextAutoTestTime = Time.time + _autoTestDelay;
                    _autoTestsCompleted++;
                }
            }
            
            // Keyboard shortcuts for testing
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PerformRandomMove();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                ToggleSystem();
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                ManualValidate();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                ResetStats();
            }
        }
        
        private void SetupUI()
        {
            if (_switchSystemButton != null)
            {
                _switchSystemButton.onClick.AddListener(ToggleSystem);
            }
            
            if (_validateButton != null)
            {
                _validateButton.onClick.AddListener(ManualValidate);
            }
            
            if (_resetStatsButton != null)
            {
                _resetStatsButton.onClick.AddListener(ResetStats);
            }
        }
        
        private void UpdateUI()
        {
            if (_statusText != null && _systemAdapter != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Active System: {(_systemAdapter.UseNewSystem ? "NEW" : "OLD")}");
                sb.AppendLine($"Game State: {_systemAdapter.GetStateDescription()}");
                sb.AppendLine($"Paths Valid: {_systemAdapter.IsValid()}");
                sb.AppendLine($"Game Won: {_systemAdapter.IsGameWon()}");
                
                _statusText.text = sb.ToString();
            }
            
            if (_statsText != null && _migrationValidator != null)
            {
                _statsText.text = _migrationValidator.GetValidationStats();
            }
            
            if (_switchSystemButton != null && _systemAdapter != null)
            {
                var buttonText = _switchSystemButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = _systemAdapter.UseNewSystem ? "Switch to Old" : "Switch to New";
                }
            }
        }
        
        // Event Handlers
        
        private void OnGameInitialized()
        {
            Debug.Log("[MigrationTestController] Game initialized");
            UpdateUI();
        }
        
        private void OnMoveCompleted()
        {
            Debug.Log("[MigrationTestController] Move completed");
            UpdateUI();
        }
        
        private void OnGameWon()
        {
            Debug.Log("[MigrationTestController] Game won!");
            UpdateUI();
            
            if (_runAutoTests)
            {
                _runAutoTests = false;
                Debug.Log($"[MigrationTestController] Auto-test completed after {_autoTestsCompleted} moves");
            }
        }
        
        private void OnValidationFailed()
        {
            Debug.LogError("[MigrationTestController] Validation failed!");
            UpdateUI();
            
            if (_runAutoTests)
            {
                _runAutoTests = false;
                Debug.LogError($"[MigrationTestController] Auto-test FAILED after {_autoTestsCompleted} moves");
            }
        }
        
        // Public Methods (for UI buttons)
        
        public void ToggleSystem()
        {
            if (_systemAdapter != null)
            {
                _systemAdapter.UseNewSystem = !_systemAdapter.UseNewSystem;
                UpdateUI();
            }
        }
        
        public void ManualValidate()
        {
            if (_migrationValidator != null)
            {
                _migrationValidator.ManualValidation();
                UpdateUI();
            }
        }
        
        public void ResetStats()
        {
            if (_migrationValidator != null)
            {
                _migrationValidator.ResetStats();
                UpdateUI();
            }
        }
        
        public void StartAutoTest()
        {
            _runAutoTests = true;
            _autoTestsCompleted = 0;
            _nextAutoTestTime = Time.time + _autoTestDelay;
            Debug.Log($"[MigrationTestController] Starting auto-test with {_autoTestMoves} moves");
        }
        
        public void StopAutoTest()
        {
            _runAutoTests = false;
            Debug.Log($"[MigrationTestController] Auto-test stopped after {_autoTestsCompleted} moves");
        }
        
        public void PerformRandomMove()
        {
            if (_systemAdapter == null) return;
            
            // Random action: rotate a random tile
            int randomSlot = Random.Range(0, 9);
            
            Debug.Log($"[MigrationTestController] Performing random rotation on slot {randomSlot}");
            _systemAdapter.RotateTile(randomSlot);
        }
        
        // Context menu items for testing in editor
        
        [ContextMenu("Toggle System")]
        private void ContextToggleSystem()
        {
            ToggleSystem();
        }
        
        [ContextMenu("Perform Random Move")]
        private void ContextRandomMove()
        {
            PerformRandomMove();
        }
        
        [ContextMenu("Manual Validate")]
        private void ContextManualValidate()
        {
            ManualValidate();
        }
        
        [ContextMenu("Reset Stats")]
        private void ContextResetStats()
        {
            ResetStats();
        }
        
        [ContextMenu("Start Auto Test")]
        private void ContextStartAutoTest()
        {
            StartAutoTest();
        }
        
        [ContextMenu("Stop Auto Test")]
        private void ContextStopAutoTest()
        {
            StopAutoTest();
        }
        
        [ContextMenu("Print Full Status")]
        private void ContextPrintStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== MIGRATION STATUS ===");
            
            if (_systemAdapter != null)
            {
                sb.AppendLine(_systemAdapter.GetStateDescription());
            }
            
            if (_migrationValidator != null)
            {
                sb.AppendLine(_migrationValidator.GetValidationStats());
            }
            
            sb.AppendLine($"Auto-test running: {_runAutoTests}");
            sb.AppendLine($"Auto-test moves completed: {_autoTestsCompleted}/{_autoTestMoves}");
            
            Debug.Log(sb.ToString());
        }
    }
}
