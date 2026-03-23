using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using static UnityEngine.Random;
using static UnityEngine.Debug;

public class QuadrophobicMazeScript : MonoBehaviour 
{

	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	
	public KMSelectable[] Buttons;

	public Sprite[] Icons;
    public SpriteRenderer[] IconViewers;

    public Material[] ButtonMats;

	static int moduleIdCounter = 1, qmMazeIdCounter = 1;
	int moduleId, qmMazeId;
	private bool moduleSolved, isActivated;

    private bool canShowIcons = true;
    private bool canReset;

	private SolveOrderGenerator iconGridGenerator;

	private QMazeTools generator;
	private string[,,,] maze;
	private Icon[,,,] iconGrid;

	private int[] initialPosition, currentPosition;
	private Icon currentPositionIcon;
	private List<Icon> keys, resetKeysForReference;

    private static readonly Dictionary<QMButton, char> buttonToWallLetter = new Dictionary<QMButton, char>
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

	private Coroutine holding, moduleAnimator;

	void Awake()
    {
		moduleId = moduleIdCounter++;
        qmMazeId = qmMazeIdCounter++;
        
        Module.OnActivate += Activate;

        foreach (KMSelectable button in Buttons)
        {
            button.OnInteract += () => { ButtonPress(button); return false; };
            button.OnInteractEnded += () => { ButtonRelease(button); };
        }
    }

	private int uotIncrease = 0;

	private float DetermineUOT() => Enumerable.Range(0, 30 + (10 * uotIncrease)).Sum(_ => Range(0.8f, 1.2f));

	
	void Start()
    {
	    generator = new QMazeTools();
	    iconGridGenerator = new SolveOrderGenerator(Icons);

        iconGrid = iconGridGenerator.GeneratedSolveOrderGrid;

	    maze = generator.GeneratedMaze;
	    initialPosition = generator.InitialPosition;
	    currentPosition = initialPosition.ToArray();

	    keys = QMExtensions.GenerateGoals(Bomb.GetSerialNumber(), iconGrid);
	    resetKeysForReference = keys.ToList();
	    
	    UpdatePosition();
        
        Log($"[Quadrophobic Maze #{moduleId}] The initial position is at [{currentPosition.Join(",")}] ({currentPositionIcon.DecimalPosition})");
        Log($"[Quadrophobic Maze #{moduleId}] ----------------------------------------------------");
        Log($"[Quadrophobic Maze #{moduleId}] Goals are found in {keys.Select(x => $"[{QMExtensions.GetCoords(x, iconGrid).Join(",")}] ({x.DecimalPosition}) (Table Index: {x.TableIndex})").Join(", ")}");
        Log($"[Quadrophobic Maze #{moduleId}] ----------------------------------------------------");
        Log($"[Quadrophobic Maze #{moduleId}] < Generated Walls of the Maze >");
        foreach (var log in generator.LoggedMaze)
	        Log($"[Quadrophobic Maze #{moduleId}] {log}");
    }

	void OnDestroy()
	{
		qmMazeIdCounter = 1;
	}

    void Activate()
    {
        isActivated = true;
        
        if (qmMazeId == 1)
            Audio.PlaySoundAtTransform("Startup", transform);

        foreach (var iconViewer in IconViewers)
            iconViewer.GetComponentInParent<MeshRenderer>().material = ButtonMats[1];
        
        DisplayIcons();
    }

	void ButtonPress(KMSelectable button)
	{
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
		button.AddInteractionPunch(0.4f);

		if (moduleSolved || !isActivated || moduleAnimator != null)
			return;
		
		var buttonIx = (QMButton)Array.IndexOf(Buttons, button);

        switch (buttonIx)
        {
            case QMButton.Dollar:
                if (holding != null)
                {
                    StopCoroutine(holding);
                    holding = null;
                }
                
                holding = StartCoroutine(Holding());

                break;
            default:
                if (!maze[currentPosition[0], currentPosition[1], currentPosition[2], currentPosition[3]].Contains(buttonToWallLetter[buttonIx]))
                {
                    switch (buttonIx)
                    {
                        case QMButton.Up:
                        case QMButton.Down:
                            currentPosition[2] = (buttonIx == QMButton.Up ? currentPosition[2] - 1 + 4 : currentPosition[2] + 1) % 4;
                            break;
                        case QMButton.Left:
                        case QMButton.Right:
                            currentPosition[3] = (buttonIx == QMButton.Left ? currentPosition[3] - 1 + 4 : currentPosition[3] + 1) % 4;
                            break;
                        case QMButton.Top:
                        case QMButton.Bottom:
                            currentPosition[1] = (buttonIx == QMButton.Top ? currentPosition[1] - 1 + 4 : currentPosition[1] + 1) % 4;
                            break;
                        case QMButton.Ana:
                        case QMButton.Kata:
                            currentPosition[0] = (buttonIx == QMButton.Ana ? currentPosition[0] - 1 + 4 : currentPosition[0] + 1) % 4;
                            break;
                    }
                    
                    UpdatePosition();
                    
                    Log($"[Quadrophobic Maze #{moduleId}] You went {buttonIx} and now went to ({currentPosition.Join(",")}) ({currentPositionIcon.DecimalPosition})");
                    
                    DisplayIcons();
                }
                else
                {
	                Log($"[Quadrophobic Maze #{moduleId}] You went {buttonIx}, but there's a wall in that direction. Strike!");
	                moduleAnimator = StartCoroutine(StrikeAnimation());
                }
                break;
        }

	}
	
	void UpdatePosition() => currentPositionIcon = iconGrid[currentPosition[0], currentPosition[1], currentPosition[2], currentPosition[3]];

    IEnumerator Holding()
    {
        var duration = 1f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canReset = true;
        holding = null;
    }

    IEnumerator StrikeAnimation()
    {
	    var duration = 1f;
	    var elapsed = 0f;

	    foreach (var iconViewer in IconViewers)
		    iconViewer.enabled = false;

	    var baseColor = ButtonMats[1].color;

	    Audio.PlaySoundAtTransform("Strike", transform);
	    Module.HandleStrike();
	    
	    while (elapsed < duration)
	    {
		    Array.ForEach(IconViewers, x => x.GetComponentInParent<MeshRenderer>().material.color = Color.Lerp(Color.red, baseColor, elapsed));
		    yield return null;
		    elapsed += Time.deltaTime;
	    }
	    
	    Array.ForEach(IconViewers, x => x.GetComponentInParent<MeshRenderer>().material.color = baseColor);

	    canShowIcons = true;
	    
	    DisplayIcons();

	    moduleAnimator = null;
    }

	void ButtonRelease(KMSelectable button)
	{
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, button.transform);
		button.AddInteractionPunch(0.4f);

		if (moduleSolved || !isActivated || moduleAnimator != null || (QMButton)Array.IndexOf(Buttons, button) != QMButton.Dollar)
			return;

		if (holding != null)
		{
			StopCoroutine(holding);
			holding = null;
		}

        if (canReset)
        {
            canReset = false;
            // TODO: Create a coroutine for resetting.
        }
        else
        {
            if (keys.First().Equals(currentPositionIcon))
            {
                keys.RemoveAt(0);

                if (canShowIcons)
                    canShowIcons = false;

                if (keys.Count == 0)
                {
                    // Todo: Create solve animation.
                }
            }
            else
            {
                Log($"[Quadrophobic Maze #{moduleId}] The current position's decimal ({currentPositionIcon}) doesn't match the key's goal, or isn't in the correct order. Strike!");
                moduleAnimator = StartCoroutine(StrikeAnimation());
            }
            
        }
		
	}

    void DisplayIcons()
    {
        if (!canShowIcons)
        {
            foreach (var iconViewer in IconViewers)
                iconViewer.enabled = false;

            return;
        }
        
        var dirs = new[] { QMButton.Top, QMButton.Up, QMButton.Ana, QMButton.Left, QMButton.Right, QMButton.Kata, QMButton.Down, QMButton.Bottom };
        
        for (int i = 0; i < 8; i++)
        {
            Icon? iconToDisplay;
            IconViewers[i].enabled = !QMExtensions.IsWallPresent(currentPosition, iconGrid, maze, dirs[i], out iconToDisplay);

            if (iconToDisplay == null)
                continue;

            IconViewers[i].sprite = iconToDisplay?.IconSprite;
        }
    }
	

	// Twitch Plays

	private struct QueueInfo
	{
		public Icon CurrentPosition { get; private set; }
		public int[] CurrentPositionIndex { get; private set; }
		public Icon? ParentPosition { get; private set; }
		public QMButton? Direction { get; private set; }

		public QueueInfo(Icon currentPosition, int[] currentPositionIndex, Icon? parentPosition = null, QMButton? direction = null)
		{
			CurrentPosition = currentPosition;
			CurrentPositionIndex = currentPositionIndex;
			ParentPosition = parentPosition;
			Direction = direction;
		}
	}

	private List<List<QMButton>> ObtainPaths()
	{
		var paths = new List<List<QMButton>>();

		var lastKnownPosition = currentPosition.ToArray();
		var goals = keys.ToList();

		var directions = buttonToWallLetter.Keys.ToArray();

		foreach (var goal in goals)
		{
			var q = new Queue<QueueInfo>();
			var visited = new Dictionary<Icon, QueueInfo>();
			
			q.Enqueue(new QueueInfo(iconGrid[lastKnownPosition[0], lastKnownPosition[1], lastKnownPosition[2], lastKnownPosition[3]], lastKnownPosition));

			while (q.Count > 0)
			{
				var qi = q.Dequeue();

				if (visited.ContainsKey(qi.CurrentPosition))
					continue;
				
				visited[qi.CurrentPosition] = qi;

				if (qi.CurrentPosition.Equals(goal))
				{
					lastKnownPosition = qi.CurrentPositionIndex.ToArray();
					goto goalfound;
				}
				
				foreach (var direction in directions)
					if (!maze[qi.CurrentPositionIndex[0], qi.CurrentPositionIndex[1], qi.CurrentPositionIndex[2], qi.CurrentPositionIndex[3]].Contains(buttonToWallLetter[direction]))
					{
						var modifiedIndex = qi.CurrentPositionIndex.ToArray();
						
						switch (direction)
						{
							case QMButton.Up:
							case QMButton.Down:
								modifiedIndex[2] = (direction == QMButton.Up ? modifiedIndex[2] - 1 + 4 : modifiedIndex[2] + 1) % 4;
								break;
							case QMButton.Left:
							case QMButton.Right:
								modifiedIndex[3] = (direction == QMButton.Left ? modifiedIndex[3] - 1 + 4 : modifiedIndex[3] + 1) % 4;
								break;
							case QMButton.Top:
							case QMButton.Bottom:
								modifiedIndex[1] = (direction == QMButton.Top ? modifiedIndex[1] - 1 + 4 : modifiedIndex[1] + 1) % 4;
								break;
							case QMButton.Ana:
							case QMButton.Kata:
								modifiedIndex[0] = (direction ==  QMButton.Ana ? modifiedIndex[0] - 1 + 4 : modifiedIndex[0] + 1) % 4;
								break;
						}
						
						var newIcon = iconGrid[modifiedIndex[0], modifiedIndex[1], modifiedIndex[2], modifiedIndex[3]];
						
						q.Enqueue(new QueueInfo(newIcon, modifiedIndex, qi.CurrentPosition, direction));
					}
			}

			throw new InvalidOperationException($"Goal {goals.FindIndex(goal.Equals) + 1} cannot be reached and is therefore unsolvable!");
			
			goalfound:
			var r = goal;
			var path = new List<QMButton>();

			while (true)
			{
				var nr = visited[r];

				if (nr.ParentPosition == null)
					break;
				
				path.Add(nr.Direction.Value);
				
				r = nr.ParentPosition.Value;
			}
			
			paths.Add(path);
		}

		return paths;
	}
	

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} something";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand(string command)
    {
		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		yield return null;
    }

	IEnumerator TwitchHandleForcedSolve()
    {
		yield return null;
    }


}