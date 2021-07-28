using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ProceduralMapGenerator : MapGenerator
{
    [SerializeField]
    private GameObject Player;
    [SerializeField]
    private Transform ChunkParent;
    [SerializeField]
    TileMap map;

    private Vector2Int chunkSpawnPlayerPos = new Vector2Int(0, 0);
    private Vector2Int chunkDisablePlayerPos = new Vector2Int(0, 0);
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    // Start is called before the first frame update
    void Start()
    {
        SpawnInitialChunks();
    }

    // Update is called once per frame
    private void Update()
    {
        SpawnNewChunks();
        DisableOldChunks();
    }

    private void SpawnNewChunks()
    {
        int xPlayerChunkPos = Mathf.RoundToInt(Player.transform.position.x / 12);
        int yPlayerChunkPos = Mathf.RoundToInt(Player.transform.position.z / 12);

        Vector2Int diff = new Vector2Int(Mathf.Abs(xPlayerChunkPos - chunkSpawnPlayerPos.x), Mathf.Abs(yPlayerChunkPos - chunkSpawnPlayerPos.y));

        if (xPlayerChunkPos > chunkSpawnPlayerPos.x)
        {
            SpawnXChunks(diff.x, chunkSpawnPlayerPos.x + Settings.HalfViewDistanceCeil, yPlayerChunkPos);
            chunkSpawnPlayerPos.x = xPlayerChunkPos;
        }
        else if (xPlayerChunkPos < chunkSpawnPlayerPos.x)
        {
            SpawnXChunks(diff.x, chunkSpawnPlayerPos.x - Settings.HalfViewDistanceCeil - diff.x + 1, yPlayerChunkPos);
            chunkSpawnPlayerPos.x = xPlayerChunkPos;
        }

        if (yPlayerChunkPos > chunkSpawnPlayerPos.y)
        {
            SpawnYChunks(diff.y, chunkSpawnPlayerPos.y + Settings.HalfViewDistanceCeil, xPlayerChunkPos);
            chunkSpawnPlayerPos.y = yPlayerChunkPos;
        }
        else if (yPlayerChunkPos < chunkSpawnPlayerPos.y)
        {
            SpawnYChunks(diff.y, chunkSpawnPlayerPos.y - Settings.HalfViewDistanceCeil - diff.y + 1, xPlayerChunkPos);
            chunkSpawnPlayerPos.y = yPlayerChunkPos;
        }
    }

    private void DisableOldChunks()
    {
        int xPlayerChunkPos = Mathf.RoundToInt(Player.transform.position.x / 12);
        int yPlayerChunkPos = Mathf.RoundToInt(Player.transform.position.z / 12);

        Vector2Int diff = new Vector2Int(Mathf.Abs(xPlayerChunkPos - chunkDisablePlayerPos.x), Mathf.Abs(yPlayerChunkPos - chunkDisablePlayerPos.y));

        if (xPlayerChunkPos > chunkDisablePlayerPos.x)
        {
            for (int i = 0; i < diff.x; i++)
            {
                DisableChunksInColumn(chunkDisablePlayerPos.x - Settings.HalfViewDistanceFloor + i, yPlayerChunkPos, diff.y);
            }

            chunkDisablePlayerPos.x = xPlayerChunkPos;
        }
        else if (xPlayerChunkPos < chunkDisablePlayerPos.x)
        {
            for (int i = 0; i < diff.x; i++)
            {
                DisableChunksInColumn(chunkDisablePlayerPos.x + Settings.HalfViewDistanceFloor - i, yPlayerChunkPos, diff.y);
            }
            chunkDisablePlayerPos.x = xPlayerChunkPos;
        }

        if (yPlayerChunkPos > chunkDisablePlayerPos.y)
        {
            for (int i = 0; i < diff.y; i++)
            {
                DisableChunksInRow(chunkDisablePlayerPos.y - Settings.HalfViewDistanceFloor + i, xPlayerChunkPos, diff.x);
            }
            chunkDisablePlayerPos.y = yPlayerChunkPos;
        }
        else if (yPlayerChunkPos < chunkDisablePlayerPos.y)
        {
            for (int i = 0; i < diff.y; i++)
            {
                DisableChunksInRow(chunkDisablePlayerPos.y + Settings.HalfViewDistanceFloor - i, xPlayerChunkPos, diff.x);
            }
            chunkDisablePlayerPos.y = yPlayerChunkPos;
        }
    }

    private void SpawnInitialChunks()
    {
        int floor   = Mathf.FloorToInt(Settings.ViewDistance / 2.0f);
        int ceiling = Mathf.CeilToInt(Settings.ViewDistance / 2.0f);

        for (int i = -floor; i < ceiling; i++)
        {
            for (int j = -floor; j < ceiling; j++)
            {
                Chunk chunk = SpawnChunk(new Vector2Int(j, i));
                chunks.Add(new Vector2Int(j, i), chunk);
            }
        }
    }

    private void SpawnXChunks(int columns, int startChunkXPos, int middleChunkYPos)
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < Settings.ViewDistance; y++)
            {
                Vector2Int chunkPos = new Vector2Int(startChunkXPos + x, middleChunkYPos - Settings.HalfViewDistanceFloor + y);
                EnableOrSpawnChunk(chunkPos);
            }
        }
    }

    private void SpawnYChunks(int rows, int startChunkYPos, int middleChunkXPos)
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < Settings.ViewDistance; x++)
            {
                Vector2Int chunkPos = new Vector2Int(middleChunkXPos - Settings.HalfViewDistanceFloor + x, startChunkYPos + y);
                EnableOrSpawnChunk(chunkPos);
            }
        }
    }

    private void DisableChunksInColumn(int x, int middleYPos, int additionalDist = 0)
    {
        for (int i = 0; i < Settings.ViewDistance + (additionalDist * 2); i++)
        {
            int y = middleYPos + i - Settings.HalfViewDistanceFloor - additionalDist;
            Vector2Int pos = new Vector2Int(x, y);
            TryDisableChunk(pos);
        }
    }

    private void DisableChunksInRow(int y, int middleXPos, int additionalDist = 0)
    {
        for (int i = 0; i < Settings.ViewDistance + (additionalDist * 2); i++)
        {
            int x = middleXPos + i - Settings.HalfViewDistanceFloor - additionalDist;
            Vector2Int pos = new Vector2Int(x, y);
            TryDisableChunk(pos);
        }
    }

    private bool TryDisableChunk(Vector2Int position)
    {
        if (chunks.ContainsKey(position))
        {
            chunks[position].gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    private void EnableOrSpawnChunk(Vector2Int position)
    {
        if (chunks.ContainsKey(position))
        {
            //Enable the pre-existing chunk
            chunks[position].gameObject.SetActive(true);
        }
        else
        {
            //Spawn a new chunk and add it to the dictionary
            Chunk chunk = SpawnChunk(position);
            chunks.Add(position, chunk);
            
        }
    }

    private Chunk SpawnChunk(Vector2Int position)
    {
        GameObject chunkObj = new GameObject("Chunk (" + position.x + ", " + position.y + ")");
        chunkObj.transform.SetParent(ChunkParent);
        chunkObj.transform.position = new Vector3(position.x * Settings.ChunkSize, 0, position.y * Settings.ChunkSize);
        Chunk chunk = chunkObj.AddComponent<Chunk>();
        chunk.SetMapGenerator(this);
        chunk.Generate();
        return chunk;
    }
}

[CustomEditor(typeof(ProceduralMapGenerator))]
public class ProceduralMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Get Tiles"))
        {
            ((ProceduralMapGenerator)serializedObject.targetObject).GetTiles();
        }

        base.OnInspectorGUI();
    }
}
