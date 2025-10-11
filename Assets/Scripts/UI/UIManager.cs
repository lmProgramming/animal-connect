using Quest;
using UnityEngine;

namespace UI
{
    public sealed class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        public QuestVisualizer questVisualizer;

        private void Awake()
        {
            Instance = this;
        }
    }
}