using System;
using System.Linq;
using UnityEngine;

public struct Icon : IEquatable<Icon>
{
    public Sprite IconSprite { get; private set; }
    public int DecimalPosition { get; private set; }
    public int TableIndex { get; private set; }

    public Icon(Sprite iconSprite, int decimalPosition, int tableIndex)
    {
        IconSprite = iconSprite;
        DecimalPosition = decimalPosition;
        TableIndex = tableIndex;
    }

    public override bool Equals(object obj) => obj is Icon && Equals((Icon)obj);

    public override int GetHashCode() => 420 * DecimalPosition  + TableIndex % 4;

    public bool Equals(Icon other) => IconSprite == other.IconSprite && DecimalPosition == other.DecimalPosition && TableIndex == other.TableIndex;
}

public class SolveOrderGenerator
{
    public Icon[,,,] GeneratedSolveOrderGrid { get; private set; }
     
    public SolveOrderGenerator(Sprite[] icons)
    {
        GeneratedSolveOrderGrid = Enumerable.Range(0, 360).ToList().Shuffle().Take(256).OrderBy(x => x).Select((x, i) => new Icon(icons[x], i, x)).To4DArray();
    }
 }