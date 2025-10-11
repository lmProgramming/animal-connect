using UnityEngine;

public class FallingIconsManager : MonoBehaviour
{
    [SerializeField] private GameObject[] icons;

    [SerializeField] private Vector2 screenDimensions;

    // Start is called before the first frame update
    private void Start()
    {
        foreach (var icon in icons)
            icon.transform.position = new Vector3(Random.Range(-screenDimensions.x, screenDimensions.x),
                Random.Range(-screenDimensions.y, screenDimensions.y));
    }

    // Update is called once per frame
    private void Update()
    {
        foreach (var icon in icons)
        {
            Vector2 position = icon.transform.position;
            if (position.x > screenDimensions.x || position.x < -screenDimensions.x ||
                position.y > screenDimensions.y || position.y < -screenDimensions.y)
                icon.transform.position = new Vector3(Random.Range(-screenDimensions.x, screenDimensions.x),
                    screenDimensions.y);
        }
    }
}