using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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

        ProcessingMap();
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
            seed = Time.time.ToString().GetHashCode();
        System.Random rnd = new System.Random(seed);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[y, x] = 1;
                    continue;
                }
                
                map[y, x] = rnd.Next(0, 100) < percentFill ? 1 : 0;
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

    void ProcessingMap()
    {
        List<List<Cell>> regions = GetRegions(1);
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

        List<Room> survivingRooms = new List<Room>();
        regions = GetRegions(0);
        foreach (List<Cell> region in regions)
        {
            int sizeRegion = region.Count;

            if (sizeRegion > minSizeRoom)
            {
                survivingRooms.Add(new Room(region, map));
                continue;
            }

            foreach (Cell cell in region)
            {
                map[cell.y, cell.x] = 1;
            }
        }

        survivingRooms.Sort();
        survivingRooms[0].isAccessibleFromMainRoom = true;
        survivingRooms[0].isMainRoom = true;

        ConnectClosestRooms(survivingRooms);
    }

    void ConnectClosestRooms(List<Room> rooms)
    {
        Room mainRoom = rooms[0];
        foreach (Room room in rooms)
        {
            if (room.isMainRoom)
                continue;

            room.distToMain = room.GetDistToRoom(mainRoom);
        }

        List<Room> sorted = rooms.OrderBy(r => r.distToMain).ToList();

        foreach (Room roomA in sorted)
        {
            if (roomA.isMainRoom)
                continue;

            float bestDist = float.MaxValue;
            Cell bestCellA = new Cell();
            Cell bestCellB = new Cell();
            Room bestRoomA = new Room();
            Room bestRoomB = new Room();

            foreach (Room roomB in rooms)
            {
                if (roomA == roomB)
                    continue;

                if (!roomB.isAccessibleFromMainRoom)
                    continue;

                float distBeteeenRooms = roomA.GetDistToRoom(roomB, out Cell tempA, out Cell tempB);

                if (distBeteeenRooms < bestDist)
                {
                    bestDist = distBeteeenRooms;
                    bestCellA = tempA;
                    bestCellB = tempB;
                    bestRoomA = roomA;
                    bestRoomB = roomB;
                }
            }

            CreatePassage(bestRoomA, bestRoomB, bestCellA, bestCellB);
        }
    }

    void CreatePassage(Room roomA, Room roomB, Cell cellA, Cell cellB)
    {
        roomA.ConnectRoom(roomB);

        int dx = cellB.x - cellA.x;
        int dy = cellB.y - cellA.y;

        if (Math.Abs(dx) > Math.Abs(dy))
        {
            int delta = System.Math.Sign(dx);
            int x = cellA.x - delta;
            
            while (x != cellB.x)
            {
                x += delta;
                int y = (int)((float)dy / dx * x - (float)dy / dx * cellA.x + cellA.y);

                for (int y1 = y - 1; y1 <= y + 1; y1++)
                {
                    for (int x1 = x - 1; x1 <= x + 1; x1++)
                    {
                        if (!IsInMap(x1, y1))
                            continue;

                        map[y1, x1] = 0;
                    }
                }
            }
        }
        else
        {
            int delta = System.Math.Sign(dy);
            int y = cellA.y - delta;

            while (y != cellB.y)
            {
                y += delta;
                int x = (int)((float)dx / dy * y - (float)dx / dy * cellA.y + cellA.x);

                for (int y1 = y-1; y1 <= y+1; y1++)
                {
                    for (int x1 = x - 1; x1 <= x + 1; x1++)
                    {
                        if (!IsInMap(x1, y1))
                            continue;

                        map[y1, x1] = 0;
                    }
                }
            }
        }
    }

    Vector3 CellToWorldPoint(Cell cell)
    {
        return new Vector3(cell.x - width / 2, cell.y - height / 2, -5);
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

        /*List<List<Cell>> regions = GetRegions(0);
        foreach (List<Cell> region in regions)
        {
            Room room = new Room(region, map);
            //Debug.Log(room.edgeCells.Count);
            for (int i = 0; i < room.edgeCells.Count; i++)
            {
                int x = room.edgeCells[i].x;
                int y = room.edgeCells[i].y;
                Gizmos.color = Color.blue;
                Vector3 pos = new Vector3(x - width / 2f, y - height / 2f, 0);
                Gizmos.DrawCube(pos, Vector3.one);
            }
        }

        Vector3 pos1 = CellToWorldPoint(new Cell(0, 0));
        Gizmos.DrawSphere(pos1, 0.5f);

        Vector3 pos2 = CellToWorldPoint(new Cell(1, 0));
        Gizmos.DrawSphere(pos2, 0.5f);*/
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

    class Room : System.IComparable<Room>
    {
        public List<Cell> cells;
        public List<Cell> edgeCells;
        public int sizeRoom;

        public List<Room> connectedRooms;

        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public float distToMain;

        public Room() { }

        public Room(List<Cell> cells, int[,] map)
        {
            this.cells = cells;
            sizeRoom = cells.Count;
            connectedRooms = new List<Room>();

            edgeCells = new List<Cell>();
            foreach (Cell cell in cells)
            {
                bool founed = false;
                for (int neighborsY = cell.y - 1; neighborsY <= cell.y + 1; neighborsY++)
                {
                    for (int neighborsX = cell.x - 1; neighborsX <= cell.x + 1; neighborsX++)
                    {
                        if (neighborsX == cell.x && neighborsY == cell.y) // Сомого себя проверять не надо
                            continue;

                        if (neighborsX != cell.x && neighborsY != cell.y) // Диагональные ячейки проверять не надо
                            continue;

                        if (map[neighborsY, neighborsX] == 0)
                            continue;

                        edgeCells.Add(cell);
                        founed = true;
                    }
                    if (founed)
                        break;
                }
            }
        }

        
        public void SetAccessibleFromMainRoom()
        {
            if (isAccessibleFromMainRoom)
                return;

            isAccessibleFromMainRoom = true;
            foreach (Room room in connectedRooms)
            {
                room.SetAccessibleFromMainRoom();
            }
        }

        public void ConnectRoom(Room otherRoom)
        {
            if (isAccessibleFromMainRoom)
            {
                otherRoom.SetAccessibleFromMainRoom();
            }
            else if (otherRoom.isAccessibleFromMainRoom)
            {
                SetAccessibleFromMainRoom();
            }

            connectedRooms.Add(otherRoom);
            otherRoom.connectedRooms.Add(this);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }

        public float GetDistToRoom(Room other)
        {
            float bestDist = float.MaxValue;
            foreach (Cell cellA in edgeCells)
            {
                foreach (Cell cellB in other.edgeCells)
                {
                    float distBeteeenRooms = Mathf.Pow(cellA.x - cellB.x, 2) + Mathf.Pow(cellA.y - cellB.y, 2);

                    if (distBeteeenRooms < bestDist)
                    {
                        bestDist = distBeteeenRooms;
                    }
                }
            }

            return bestDist;
        }

        public float GetDistToRoom(Room other, out Cell bestCellA, out Cell bestCellB)
        {
            float bestDist = float.MaxValue;
            bestCellA = new Cell();
            bestCellB = new Cell();
            foreach (Cell cellA in edgeCells)
            {
                foreach (Cell cellB in other.edgeCells)
                {
                    float distBeteeenRooms = Mathf.Pow(cellA.x - cellB.x, 2) + Mathf.Pow(cellA.y - cellB.y, 2);

                    if (distBeteeenRooms < bestDist)
                    {
                        bestDist = distBeteeenRooms;
                        bestCellA = cellA;
                        bestCellB = cellB;
                    }
                }
            }

            return bestDist;
        }

        public int CompareTo(Room other)
        {
            return sizeRoom.CompareTo(other.sizeRoom);
        }
    }
}
