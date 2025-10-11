using UnityEngine;

namespace Quest
{
    public class QuestManager : MonoBehaviour
    {
        public float difficulty;

        private void Start()
        {
            difficulty = Random.value * Random.value;

            GenerateQuest();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                FindAnyObjectByType<QuestVisualizer>().DeleteVisualization();

                GenerateQuest();
            }
        }

        private void GenerateQuest()
        {
            var quest = QuestGenerator.GenerateQuest(difficulty);

            GameManager.Instance.SetupQuest(quest);
        }
    }
}