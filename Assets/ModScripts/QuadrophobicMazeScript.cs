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
    private List<int> keys, keyReferenceForReset;

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

    private int DecimalPosition(int[] coords)
    {
        var dec = 0;

        for (int i = 0; i < coords.Length; i++)
            dec += coords[i] * (int)Math.Pow(4, 4 - i);

        return dec;
    }

	
	void Start()
    {
	    generator = new QMazeTools();
	    iconGridGenerator = new SolveOrderGenerator(Icons);

        iconGrid = iconGridGenerator.GeneratedSolveOrderGrid;

	    maze = generator.GeneratedMaze;
	    initialPosition = generator.InitialPosition;
	    currentPosition = initialPosition.ToArray();
        generator.GenerateKeys(Bomb.GetSerialNumber(), iconGridGenerator.TableIndexes, iconGrid, out keys);
        keyReferenceForReset = keys.ToList();
        
        Log($"[Quadrophobic Maze #{moduleId}] The initial position is at {DecimalPosition(currentPosition)} ({currentPosition.Join(",")})");
        
        
    }

    void OnDestroy() => qmMazeIdCounter = 1;

    void Activate()
    {
        isActivated = true;
        
        if (qmMazeIdCounter == 1)
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
                }
                DisplayIcons();
                break;
        }

	}

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
            if (keys.First() == iconGrid[currentPosition[0], currentPosition[1], currentPosition[2], currentPosition[3]].DecimalPosition)
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
                // Todo: Make a strike animation.
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
            Icon iconToDisplay;
            IconViewers[i].enabled = !QMExtensions.IsWallPresent(currentPosition, iconGrid, maze, dirs[i], out iconToDisplay);

            if (iconToDisplay == null)
                continue;

            IconViewers[i].sprite = iconToDisplay.IconSprite;
        }
    }
	

	// Twitch Plays

    private class QueueInfo
    {
        public int[] CurrentPosition { get; private set; }
        public int CurrentPositionDecimal { get; private set; }
        public int[] ParentPosition { get; private set; }
        public int? ParentPositionDecimal { get; private set; }
        public QMButton? Direction { get; private set; }

        public QueueInfo(int[] currentPosition, int[] parentPosition = null, QMButton? direction = null)
        {
            CurrentPosition = currentPosition;
            CurrentPositionDecimal = GetDecimal(currentPosition);
            ParentPosition = parentPosition;
            ParentPositionDecimal = parentPosition != null ? (int?)GetDecimal(parentPosition) : null;
            Direction = direction;
        }

        private static int GetDecimal(int[] coords)
        {
            var dec = 0;

            for (int i = 0; i < coords.Length; i++)
                dec += coords[i] * (int)Math.Pow(4, 4 - i);

            return dec;
        }
    }

    private List<List<QMButton>> GetPathsForAllKeysPresent()
    {
        var lastKnownPosition = currentPosition.ToArray();

        var directions = buttonToWallLetter.Keys.ToArray();
        
        var paths = new List<List<QMButton>>();

        foreach (var key in keys)
        {
            var q = new Queue<QueueInfo>();
            var visited = new Dictionary<int, QueueInfo>();
            
            q.Enqueue(new QueueInfo(lastKnownPosition));

            while (q.Count > 0)
            {
                var qi = q.Dequeue();

                if (visited.ContainsKey(qi.CurrentPositionDecimal))
                    continue;
                
                visited[qi.CurrentPositionDecimal] = qi;

                if (qi.CurrentPositionDecimal == key)
                {
                    lastKnownPosition = qi.CurrentPosition;
                    goto goalfound;
                }
                
                foreach (var direction in directions)
                    if (!maze[qi.CurrentPosition[0], qi.CurrentPosition[1], qi.CurrentPosition[2], qi.CurrentPosition[3]].Contains(buttonToWallLetter[direction]))
                    {
                        var newPosition = qi.CurrentPosition.ToArray();

                        switch (direction)
                        {
                            case QMButton.Up:
                            case QMButton.Down:
                                newPosition[2] = (direction == QMButton.Up ? newPosition[2] - 1 + 4 : newPosition[2] + 1) % 4;
                                break;
                            case QMButton.Left:
                            case QMButton.Right:
                                newPosition[3] = (direction == QMButton.Left ? newPosition[3] - 1 + 4 : newPosition[3] + 1) % 4;
                                break;
                            case QMButton.Top:
                            case QMButton.Bottom:
                                newPosition[1] = (direction == QMButton.Top ? newPosition[1] - 1 + 4 : newPosition[1] + 1) % 4;
                                break;
                            case QMButton.Ana:
                            case QMButton.Kata:
                                newPosition[0] = (direction == QMButton.Ana ? newPosition[0] - 1 + 4 : newPosition[1] + 1) % 4;
                                break;
                        }
                        
                        q.Enqueue(new QueueInfo(newPosition, qi.CurrentPosition, direction));
                    }
            }

            throw new InvalidOperationException($"Cannot find a valid path for {keys.FindIndex(x => x == key) + 1}, making it unsolvable!");
            
            goalfound:
            
            var path = new List<QMButton>();
            var r = key;

            while (true)
            {
                var nr = visited[r];

                if (nr.ParentPosition == null)
                    break;
                
                path.Add(nr.Direction.Value);
                r = nr.ParentPositionDecimal.Value;
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