using System;
using System.Collections.Generic;
using System.Linq;

public static class QMExtensions
{
    public static T[,,,] To4DArray<T>(this IEnumerable<T> source)
    {
        const int verifyCount = 256;

        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (source.Count() != verifyCount)
            throw new Exception($"The collection's count ({source.Count()}) is not equal to 256!");

        var result = new T[4, 4, 4, 4];

        var convert = source.ToArray();

        var index = 0;

        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                for (int k = 0; k < 4; k++)
                    for (int l = 0; l < 4; l++)
                    {
                        result[i, j, k, l] = convert[index];
                        index++;
                    }

        return result;
    }

    private static T[] Flat4DArray<T>(this T[,,,] source) => source.Cast<T>().ToArray();

    public static bool IsWallPresent(int[] position, Icon[,,,] iconGrid, string[,,,] maze, QMButton dir, out Icon displayedAdjacentIcon)
    {
        displayedAdjacentIcon = null;

        if (dir == QMButton.Dollar)
            throw new Exception($"Direction {dir} is not present!");

        var dirToWallLetter = new Dictionary<QMButton, char>
        {
            { QMButton.Up, 'U' },
            { QMButton.Down, 'D' },
            { QMButton.Left, 'L' },
            { QMButton.Right, 'R' },
            { QMButton.Top, 'T' },
            { QMButton.Bottom, 'B' },
            { QMButton.Ana, 'A' },
            { QMButton.Kata, 'K' }
        };

        if (maze[position[0], position[1], position[2], position[3]].Contains(dirToWallLetter[dir]))
            return true;

        switch (dir)
        {
            case QMButton.Up:
                displayedAdjacentIcon = iconGrid[position[0], position[1], (position[2] - 1 + 4) % 4, position[3]];
                break;
            case QMButton.Down:
                displayedAdjacentIcon =  iconGrid[position[0], position[1], (position[2] + 1) % 4, position[3]];
                break;
            case QMButton.Left:
                displayedAdjacentIcon = iconGrid[position[0], position[1], position[2], (position[3] - 1 + 4) % 4];
                break;
            case QMButton.Right:
                displayedAdjacentIcon = iconGrid[position[0], position[1], position[2], (position[3] + 1) % 4];
                break;
            case QMButton.Top:
                displayedAdjacentIcon = iconGrid[position[0], (position[1] - 1 + 4) % 4, position[2], position[3]];
                break;
            case QMButton.Bottom:
                displayedAdjacentIcon = iconGrid[position[0], (position[1] + 1) % 4, position[2], position[3]];
                break;
            case QMButton.Ana:
                displayedAdjacentIcon = iconGrid[(position[0] - 1 + 4) % 4, position[1], position[2], position[3]];
                break;
            case QMButton.Kata:
                displayedAdjacentIcon = iconGrid[(position[0] + 1) % 4, position[1], position[2], position[3]];
                break;
        }

        return false;
    }

    private static string ConvertBase36ToBinary(string base36Value)
    {
        var result = 0;

        const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        foreach (var c in base36Value)
        {
            var charIx = base36.IndexOf(c);

            result = result * 36 + charIx;
        }

        var current = string.Empty;

        while (result != 0)
        {
            current = base36[result % 2] + current;
            result /= 2;
        }

        return current.PadLeft(32, '0');
    }

    private static int ConvertTernaryToDecimal(int input)
    {
        var total = 0;
        var numberLength = input.ToString().Length;

        for (int i = 0; i < numberLength; i++)
            total += (int)Math.Pow(3, numberLength - (i + 1)) * int.Parse(input.ToString()[i].ToString());

        return total;
    }

    public static List<int> GenerateGoals(string sn, List<int> tableIndexes, Icon[,,,] grid)
    {
        var goals = new List<int>();

        var convertedSn = ConvertBase36ToBinary(sn);

        var splitStr = new[] { convertedSn.Substring(0, 16), convertedSn.Substring(16) }.Select(x => x.Select(y => y - '0').ToArray()).ToArray();

        var addedTernary = new int[16];

        for (int i = 0; i < 16; i++)
            addedTernary[i] = splitStr[0][i] + splitStr[1][i];

        var ternaryGroups = addedTernary.Select((x, i) => new { Index = i, Value = x }).GroupBy(x => x.Index / 4).Select(x => x.Select(v => v.Value).Join("")).Select(int.Parse).ToArray();
        
        var convertedGroups = ternaryGroups.Select(ConvertTernaryToDecimal).ToArray();

        var flattenGrid = grid.Flat4DArray();

        foreach (var index in convertedGroups)
        {
            var modifiedIndex = index;

            while (true)
            {
                if (tableIndexes.Contains(modifiedIndex))
                {
                    if (goals.Contains(flattenGrid.First(x => x.TableIndex == modifiedIndex).DecimalPosition))
                        continue;

                    break;
                }

                modifiedIndex = (modifiedIndex + 1) % 360;
            }
            
            goals.Add(flattenGrid.First(x => x.TableIndex == modifiedIndex).DecimalPosition);
        }

        return goals;
    }
}