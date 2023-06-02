using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Spawner : MonoBehaviour
{
    [SerializeField] private MapGeneration mapGen;
    [SerializeField] private Player _playerPrefab;
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private Camera _camera;

    [SerializeField, Min(0)] private float _playerSpeed;

    void Start()
    {
        SpawnPlayer();
        SpawnEnemy();
    }

    private void SpawnPlayer()
    {
        int width = mapGen.Map.GetLength(1);
        int height = mapGen.Map.GetLength(0);

        List<Cell> emptyCellForSpawn = new List<Cell>();

        for (int y = height / 4; y < height - height / 4; y++)
        {
            for (int x = width / 4; x < width - width / 4; x++)
            {
                if (mapGen.Map[y, x] == 1 || mapGen.Map[y, x + 1] == 1 || mapGen.Map[y + 1, x] == 1 || mapGen.Map[y, x - 1] == 1 || mapGen.Map[y - 1, x] == 1)
                    continue;

                emptyCellForSpawn.Add(new Cell(x, y));
            } 
        }

        System.Random rnd = new System.Random(DateTime.Now.ToString().GetHashCode());
        int index = rnd.Next(0, emptyCellForSpawn.Count);
        Cell cell = emptyCellForSpawn[index];
        Vector3 Pos = new Vector3(cell.x - width / 2, cell.y - height / 2, 0);
        Player player = Instantiate(_playerPrefab, Pos, Quaternion.identity);
        player.Init(_playerSpeed);
        _camera.transform.parent = player.transform;
        _camera.transform.localPosition = new Vector3(0, 0, -10);
    }

    private void SpawnEnemy()
    {
        int width = mapGen.Map.GetLength(1);
        int height = mapGen.Map.GetLength(0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mapGen.Map[y, x] == 1 || mapGen.Map[y, x + 1] == 1 || mapGen.Map[y + 1, x] == 1 || mapGen.Map[y, x - 1] == 1 || mapGen.Map[y - 1, x] == 1)
                    continue;

                System.Random rnd = new System.Random((DateTime.Now.ToString() + x.ToString() + y.ToString()).GetHashCode());
                if (rnd.Next(0, 100) < 5)
                {
                    Vector3 Pos = new Vector3(x - width / 2, y - height / 2, 0);
                    Instantiate(_enemyPrefab, Pos, Quaternion.identity);
                }
            }
        }
    }
}
