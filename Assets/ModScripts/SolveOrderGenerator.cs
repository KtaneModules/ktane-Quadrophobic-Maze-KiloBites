using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Icon
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
}

public class SolveOrderGenerator
{
    public Icon[,,,] GeneratedSolveOrderGrid { get; private set; }
    public List<int> TableIndexes { get; private set; }
     
    public SolveOrderGenerator(Sprite[] icons)
    {
        TableIndexes = Enumerable.Range(0, icons.Length).ToList().Shuffle().Take(256).OrderBy(x => x).ToList();
        GeneratedSolveOrderGrid = TableIndexes.Select((x, i) => new Icon(icons[x], i, x)).To4DArray();
    }
 }