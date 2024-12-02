using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public float difficulty;

    private void Start()
    {
        difficulty = Random.value * Random.value;

        GenerateQuest();
    }

    void GenerateQuest()
    {        
        Quest quest = QuestGenerator.GenerateQuest(difficulty);

        GameManager.Instance.SetupQuest(quest);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            FindObjectOfType<QuestVisualizer>().DeleteVisualization();

            GenerateQuest();
        }
    }
}
