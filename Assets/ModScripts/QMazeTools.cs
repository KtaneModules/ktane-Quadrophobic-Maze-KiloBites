using System.Collections.Generic;
using System.Linq;
using static UnityEngine.Random;

public class QMazeTools
{
    private static readonly int mazeRow = 4, mazeCol = 4, mazeFloor = 4, mazeTimeline = 4;

    public readonly string[,,,] GeneratedMaze;
    public readonly int[] InitialPosition;
    private string[,,,] connectionsGenerated = new string[4, 4, 4, 4];
    
    public readonly List<string> LoggedMaze = new List<string>();
    
    public QMazeTools()
    {
        InitialPosition = Enumerable.Range(0, 4).Select(_ => Range(0, 4)).ToArray();
        GeneratedMaze = Enumerable.Repeat("UDLRTBAK", 256).To4DArray();
        GenerateMazeOriginShift();
    }

    void GenerateMazeOriginShift()
    {
        for (int w = 0; w < mazeTimeline; w++)
            for (int z = 0; z < mazeFloor; z++)
                for (int x = 0; x < mazeCol; x++)
                    for (int y = 0; y < mazeRow; y++)
                    {
                        //U = Up, D = Down, L = Left, R = Right, T = Top, B = Bottom, A = Ana, K = Kata

                        if (y < mazeRow - 1)
                            connectionsGenerated[w, z, x, y] = "R";
                        else if (x < mazeCol - 1)
                            connectionsGenerated[w, z, x, y] = "D";
                        else if (z < mazeFloor - 1)
                            connectionsGenerated[w, z, x, y] = "B";
                        else if (w < mazeTimeline - 1)
                            connectionsGenerated[w, z, x, y] = "K";
                        else
                            connectionsGenerated[w, z, x, y] = "X";
                    }
        int currentRow = 3, currentCol = 3, currentFloor = 3, currentTimeline = 3;

        do
        {
            for (int i = 0; i < 10; i++)
            {
                var movement = Range(0, 8);
                GeneratedMaze[currentTimeline, currentFloor, currentCol, currentRow] = "URDLTBAK";

                switch (movement)
                {
                    case 0:
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "U";
                        currentCol = (currentCol - 1 + 4) % 4;
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "X";
                        break;
                    case 1:
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "D";
                        currentCol = (currentCol + 1) % 4;
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "X";
                        break;
                    case 2:
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "L";
                        currentRow = (currentRow - 1 + 4) % 4;
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "X";
                        break;
                    case 3:
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "R";
                        currentRow = (currentRow + 1) % 4;
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "X";
                        break;
                    case 4:
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "T";
                        currentFloor = (currentFloor - 1 + 4) % 4;
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "X";
                        break;
                    case 5:
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "B";
                        currentFloor = (currentFloor + 1) % 4;
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "X";
                        break;
                    case 6:
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "A";
                        currentTimeline = (currentTimeline - 1 + 4) % 4;
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "X";
                        break;
                    case 7:
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "K";
                        currentTimeline = (currentTimeline + 1) % 4;
                        connectionsGenerated[currentTimeline, currentFloor, currentCol, currentRow] = "X";
                        break;
                }
            }
            
            for (int w = 0; w < 4; w++)
                for (int z = 0; z < 4; z++)
                    for (int x = 0; x < 4; x++)
                        for (int y = 0; y < 4; y++)
                            switch (connectionsGenerated[w, z, x, y])
                            {
                                case "U":
                                    GeneratedMaze[w, z, x, y] = GeneratedMaze[w, z, x, y].Replace("U", "");
                                    GeneratedMaze[w, z, (x - 1 + 4) % 4, y] = GeneratedMaze[w, z, (x - 1 + 4) % 4, y].Replace("D", "");
                                    break;
                                case "D":
                                    GeneratedMaze[w, z, x, y] = GeneratedMaze[w, z, x, y].Replace("D", "");
                                    GeneratedMaze[w, z, (x + 1) % 4, y] = GeneratedMaze[w, z, (x + 1) % 4, y].Replace("U", "");
                                    break;
                                case "L":
                                    GeneratedMaze[w, z, x, y] = GeneratedMaze[w, z, x, y].Replace("L", "");
                                    GeneratedMaze[w, z, x, (y - 1 + 4) % 4] = GeneratedMaze[w, z, x, (y - 1 + 4) % 4].Replace("R", "");
                                    break;
                                case "R":
                                    GeneratedMaze[w, z, x, y] = GeneratedMaze[w, z, x, y].Replace("R", "");
                                    GeneratedMaze[w, z, x, (y + 1) % 4] = GeneratedMaze[w, z, x, (y + 1) % 4].Replace("L", "");
                                    break;
                                case "T":
                                    GeneratedMaze[w, z, x, y] = GeneratedMaze[w, z, x, y].Replace("T", "");
                                    GeneratedMaze[w, (z - 1 + 4) % 4, x, y] = GeneratedMaze[w, (z - 1 + 4) % 4, x, y].Replace("B", "");
                                    break;
                                case "B":
                                    GeneratedMaze[w, z, x, y] = GeneratedMaze[w, z, x, y].Replace("B", "");
                                    GeneratedMaze[w, (z + 1) % 4, x, y] = GeneratedMaze[w, (z + 1) % 4, x, y].Replace("T", "");
                                    break;
                                case "A":
                                    GeneratedMaze[w, z, x, y] = GeneratedMaze[w, z, x, y].Replace("A", "");
                                    GeneratedMaze[(w - 1 + 4) % 4, z, x, y] = GeneratedMaze[(w - 1 + 4) % 4, z, x, y].Replace("K", "");
                                    break;
                                case "K":
                                    GeneratedMaze[w, z, x, y] = GeneratedMaze[w, z, x, y].Replace("K", "");
                                    GeneratedMaze[(w + 1) % 4, z, x, y] = GeneratedMaze[(w + 1) % 4, z, x, y].Replace("A", "");
                                    break;
                            }
        }
        while ("UDLR".Any(GeneratedMaze[InitialPosition[0], InitialPosition[1], InitialPosition[2], InitialPosition[3]].Contains));

        for (int w = 0; w < 4; w++)
        {
            LoggedMaze.Add($"W {w}:");

            for (int z = 0; z < 4; z++)
            {
                LoggedMaze.Add($"Z {z}:");

                for (int x = 0; x < 4; x++)
                {
                    var mazeShit = string.Empty;
                    for (int y = 0; y < 4; y++)
                        mazeShit += $"[{GeneratedMaze[w, z, x, y]}]";
                    
                    LoggedMaze.Add(mazeShit);
                }
            }
            LoggedMaze.Add("----------------------------------------------------");
        }
    }
}