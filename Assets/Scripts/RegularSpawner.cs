// Spawns one shape prefab repeatedly, with pacing that adapts to the current run and the shape's bias values.
using UnityEngine;

public class RegularSpawner : MonoBehaviour
{
    public GameObject Prefab;
    public float BaseSpawnInterval = 2f;
    public float MinimumSpawnInterval = 0.5f;
    public float SpawnInterval;

    private float nextSpawnTime;
    private FallingShapeBase shapePrefab;

    void Start()
    {
        // Cache the prefab's tuning once so spawn timing can react to the type of shape being produced.
        shapePrefab = Prefab != null ? Prefab.GetComponent<FallingShapeBase>() : null;
        nextSpawnTime = Time.time + Random.Range(0.4f, Mathf.Max(1f, BaseSpawnInterval));
    }

    void Update()
    {
        GameDirector director = GameDirector.Instance;

        if (director == null || !director.IsRunActive || Prefab == null || Camera.main == null)
        {
            return;
        }

        // Each spawner independently waits for its next timestamp instead of ticking down a separate timer.
        if (Time.time < nextSpawnTime)
        {
            return;
        }

        SpawnShape(director);
    }

    private void SpawnShape(GameDirector director)
    {
        // Shapes are spawned just above the visible playfield at a random horizontal lane.
        Vector3 max = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0f));
        float spawnPadding = 0.85f;
        float spawnX = Random.Range(-max.x + spawnPadding, max.x - spawnPadding);
        Vector2 spawnPosition = new Vector2(spawnX, max.y + 1.35f);

        Instantiate(Prefab, spawnPosition, Quaternion.identity);

        // The director supplies the global difficulty ramp, while each shape contributes its own spawn bias.
        float spawnBias = shapePrefab != null ? shapePrefab.SpawnBias : 1f;
        SpawnInterval = director.GetSpawnInterval(BaseSpawnInterval, MinimumSpawnInterval, spawnBias);
        nextSpawnTime = Time.time + (SpawnInterval * Random.Range(0.82f, 1.18f));
    }
}
