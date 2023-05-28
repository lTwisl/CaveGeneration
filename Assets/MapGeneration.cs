using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGeneration : MonoBehaviour
{
    public int width;
    public int height;
    [Range(0, 100)] public int percentFill;

    public bool useRandomSeed;
    public int seed;

    public int iterationSmooth;

    public int minSizeRoom;
    public int minSizeWall;

    private int[,] map;

    private void Start()
    {
        GenerationMap();
    }

    void GenerationMap()
    {
        map = new int[height, width];

        RandomFillMap();

        for (int i = 0; i < iterationSmooth; i++)
        {
            SmoothMap();
        }

        RemoveSmallItems();
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
            seed = Time.time.ToString().GetHashCode();
        Random.InitState(seed);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[y, x] = 1;
                    continue;
                }

                map[y, x] = Random.Range(0.0f, 1.0f) < percentFill / 100f ? 1 : 0;
            }
        }
    }

    void SmoothMap()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int countNeighbors = GetCountNeighbors(x, y);

                if (countNeighbors < 4)
                {
                    map[y, x] = 0;
                }
                else if (countNeighbors > 4)
                {
                    map[y, x] = 1;
                }
            }
        }
    }

    void RemoveSmallItems()
    {
        List<List<Cell>> regions = GetRegions(0);
        foreach (List<Cell> region in regions)
        {
            int sizeRegion = region.Count;

            if (sizeRegion > minSizeRoom)
                continue;

            foreach (Cell cell in region)
            {
                map[cell.y, cell.x] = 1;
            }
        }

        regions = GetRegions(1);
        foreach (List<Cell> region in regions)
        {
            int sizeRegion = region.Count;

            if (sizeRegion > minSizeWall)
                continue;

            foreach (Cell cell in region)
            {
                map[cell.y, cell.x] = 0;
            }
        }
    }

    int GetCountNeighbors(int x, int y)
    {
        int countNeighbors = 0;

        for (int neighborsY = y - 1; neighborsY <= y + 1; neighborsY++)
        {
            for (int neighborsX = x - 1; neighborsX <= x + 1; neighborsX++)
            {
                if (neighborsX == x && neighborsY == y)
                    continue;

                if (IsInMap(neighborsX, neighborsY))
                    countNeighbors += map[neighborsY, neighborsX];
                else
                    countNeighbors++;
            }
        }

        return countNeighbors;
    }

    bool IsInMap(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height; 
    }

    List<List<Cell>> GetRegions(int cellType)
    {
        List<List<Cell>> regions = new List<List<Cell>>();
        bool[,] mapConsideredСells = new bool[height, width]; // Карта рассмотренных ячеек

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mapConsideredСells[y, x]) // Проверянные ячйки проверять не надо
                    continue;

                if (map[y, x] != cellType) // Ячейки другого типа проверять не надо
                    continue;

                List<Cell> region = GetRegionCells(x, y);
                regions.Add(region);

                for (int i = 0; i < region.Count; i++)
                {
                    mapConsideredСells[region[i].y, region[i].x] = true;
                }
            }
        }

        return regions;
    }

    // Заливка
    List<Cell> GetRegionCells(int startX, int startY)
    {
        List<Cell> cells = new List<Cell>();
        bool[,] mapConsideredСells = new bool[height, width]; // Карта рассмотренных ячеек
        int cellType = map[startY, startX];

        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(new Cell(startX, startY));
        mapConsideredСells[startY, startX] = true;

        while (queue.Count > 0)
        {
            Cell cell = queue.Dequeue();
            cells.Add(cell);

            for (int neighborsY = cell.y - 1; neighborsY <= cell.y + 1; neighborsY++)
            {
                for (int neighborsX = cell.x - 1; neighborsX <= cell.x + 1; neighborsX++)
                {
                    if (neighborsX == cell.x && neighborsY == cell.y) // Сомого себя проверять не надо
                        continue;

                    if (!IsInMap(neighborsX, neighborsY)) // Ячейки, которые за картой, проверять не надо 
                        continue;

                    if (neighborsX != cell.x && neighborsY != cell.y) // Диагональные ячейки проверять не надо
                        continue;

                    if (mapConsideredСells[neighborsY, neighborsX]) // Проверянные ячйки проверять не надо
                        continue;

                    if (map[neighborsY, neighborsX] != cellType) // Ячейки другого типа проверять не надо
                        continue;

                    mapConsideredСells[neighborsY, neighborsX] = true;
                    queue.Enqueue(new Cell(neighborsX, neighborsY));
                }
            }
        }

        return cells;
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 30), "Generate"))
        {
            GenerationMap();
        }
    }

    private void OnDrawGizmos()
    {
        if (map == null)
            return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Gizmos.color = map[y, x] == 1 ? Color.black : Color.white;
                Vector3 pos = new Vector3(x - width / 2f, y - height / 2f, 0);
                Gizmos.DrawCube(pos, Vector3.one);

            }
        }
    }

    struct Cell
    {
        public int x;
        public int y;

        public Cell(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
