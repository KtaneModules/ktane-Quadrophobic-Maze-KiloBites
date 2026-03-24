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

	public AudioSource ResetAudioPlayer;

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

	private Color cyclingColor;

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
        Log($"[Quadrophobic Maze #{moduleId}] < Icons Generated with Table Indexing (0-Indexed) >");
        
        foreach (var log in iconGrid.LogIcons())
	        Log($"[Quadrophobic Maze #{moduleId}] {log}");

        var paths = ObtainPaths();
        
        foreach (var path in paths)
	        Log(path.Select(x => x).Reverse().Join(", "));

    }

	void OnDestroy()
	{
		qmMazeIdCounter = 1;
		
		if (ResetAudioPlayer.isPlaying)
			ResetAudioPlayer.Stop();
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

    IEnumerator SolveAnimation()
    {
	    var ixes = new[] { 0, 1, 2, 4, 7, 6, 5, 3 };

	    var buttonCoroutines = new Coroutine[8];
	    
	    Audio.PlaySoundAtTransform("Solve", transform);

	    for (int i = 0; i < 8; i++)
	    {
		    buttonCoroutines[i] = StartCoroutine(CycleColorFast(IconViewers[ixes[i]]));
		    yield return new WaitForSeconds(0.2f);
	    }

	    yield return new WaitForSeconds(0.7f);

	    for (int i = 0; i < 8; i++)
	    {
		    StopCoroutine(buttonCoroutines[i]);
		    buttonCoroutines[i] = null;
		    buttonCoroutines[i] = StartCoroutine(CycleColorFast(IconViewers[ixes[i]], Color.green));
		    yield return new WaitForSeconds(0.2f);
	    }

	    moduleSolved = true;
	    Module.HandlePass();
	    
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
            Log($"[Quadrophobic Maze #{moduleId}] Reset has been initiated.");
            canReset = false;
            moduleAnimator = StartCoroutine(ResetAnimation());
        }
        else
        {
	        if (keys.First().Equals(currentPositionIcon))
	        {
		        Log($"[Quadrophobic Maze #{moduleId}] Key {4 - keys.Count + 1} has been collected successfully.");
		        
		        keys.RemoveAt(0);

		        if (canShowIcons)
			        canShowIcons = false;

		        if (keys.Count == 0)
		        {
			        Log($"[Quadrophobic Maze #{moduleId}] All keys have been collected. Solved!");
			        moduleAnimator = StartCoroutine(SolveAnimation());
		        }
		        else
		        {
			        Audio.PlaySoundAtTransform("InputCorrect", transform);
			        DisplayIcons();
		        }
	        }
	        else
	        {
		        Log($"[Quadrophobic Maze #{moduleId}] The current position ({currentPosition.Join(",")}) doesn't match the current key's goal, or is trying to be collected in the wrong order. Strike!");
		        moduleAnimator = StartCoroutine(StrikeAnimation());
	        }

        }
	}

	IEnumerator ResetAnimation()
	{
		Audio.PlaySoundAtTransform("ResetStart", transform);
		
		var oldColor = ButtonMats[1].color;

		foreach (var iconViewer in IconViewers)
		{
			iconViewer.enabled = false;
			iconViewer.GetComponentInParent<MeshRenderer>().material = ButtonMats[0];
		}

		yield return new WaitForSeconds(2);

		ResetAudioPlayer.volume = 1;
		ResetAudioPlayer.Play();

		foreach (var iconViewer in IconViewers)
		{
			iconViewer.GetComponentInParent<MeshRenderer>().material = ButtonMats[1];
			iconViewer.GetComponentInParent<MeshRenderer>().material.color = Color.black;
		}

		cyclingColor = Color.red;

		var buttonCoroutines = new Coroutine[2];
		
		buttonCoroutines[0] = StartCoroutine(LoadingAnimation());
		buttonCoroutines[1] = StartCoroutine(CycleColor());

		yield return new WaitForSeconds(DetermineUOT());
		
		StopCoroutine(buttonCoroutines[1]);

		var duration = 2f;
		var elapsed = 0f;
		
		var oldCyclingColor = cyclingColor;
		
		while (elapsed < duration)
		{
			yield return null;

			ResetAudioPlayer.volume = Easing.OutExpo(elapsed, 1f, 0f, duration);
			cyclingColor = Color.Lerp(oldCyclingColor, Color.black, elapsed);
			
			elapsed += Time.deltaTime;
		}

		cyclingColor = Color.black;

		ResetAudioPlayer.volume = 0;
		ResetAudioPlayer.Stop();
		StopCoroutine(buttonCoroutines[0]);

		yield return new WaitForSeconds(3);
		
		Audio.PlaySoundAtTransform("ResetDone", transform);

		foreach (var iconViewer in IconViewers)
			iconViewer.GetComponentInParent<MeshRenderer>().material.color = oldColor;
		
		canShowIcons = true;
		
		if (uotIncrease < 9)
			uotIncrease++;
		
		currentPosition = initialPosition.ToArray();
		keys = resetKeysForReference.ToList();
		UpdatePosition();
		DisplayIcons();
		moduleAnimator = null;
	}

	IEnumerator LoadingAnimation()
	{
		var cwOrder = new[] { 0, 1, 2, 4, 7, 6, 5, 3 };
		
		while (true)
		{
			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
					IconViewers[j].GetComponentInParent<MeshRenderer>().material.color = cwOrder[i] == j ? cyclingColor : Color.black;

				yield return new WaitForSeconds(0.4f);
			}
		}
	}

	IEnumerator CycleColor()
	{
		var colors = new[] { Color.red, new Color(1, 0.5f, 0), new Color(1, 1, 0), Color.green, Color.blue, new Color(0, 0, 0.5f), new Color(0.5f, 0, 0.5f) };

		var index = 0;
		
		
		while (true)
		{
			var elapsed = 0f;
			var duration = 1f;
			
			while (elapsed < duration)
			{
				cyclingColor = Color.Lerp(colors[index], colors[(index + 1) % 7], elapsed);
				yield return null;
				elapsed += Time.deltaTime;
			}

			index++;
			index %= 7;
		}
	}

	IEnumerator CycleColorFast(SpriteRenderer iconViewer, Color? fixedColor = null)
	{
		var elapsed = 0f;
		var duration = 0.05f;

		iconViewer.enabled = false;
		
		var renderer = iconViewer.GetComponentInParent<MeshRenderer>();

		if (fixedColor != null)
		{
			var rendererColor = renderer.material.color;

			while (elapsed < duration)
			{
				renderer.material.color = Color.Lerp(rendererColor, fixedColor.Value, elapsed);
				yield return null;
				elapsed += Time.deltaTime;
			}
			renderer.material.color = fixedColor.Value;

			yield break;
		}
		
		var colors = new[] { Color.red, new Color(1, 0.5f, 0), new Color(1, 1, 0), Color.green, Color.blue, new Color(0, 0, 0.5f), new Color(0.5f, 0, 0.5f) };

		renderer.material.color = Color.red;

		var index = 0;
		
		while (true)
		{
			while (elapsed < duration)
			{
				renderer.material.color = Color.Lerp(colors[index], colors[(index + 1) % 7], elapsed);
				yield return null;
				elapsed += Time.deltaTime;
			}

			index++;
			index %= 7;
			elapsed = 0f;
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

        var dirs = buttonToWallLetter.Keys.OrderBy(x => x).ToArray();
        
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
	private readonly string TwitchHelpMessage = @"!{0} reset [resets the module] || !{0} submit [submits the current position] || !{0} move udlrtbak [moves the current position according to the directions given]";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand(string command)
    {
		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		if (!isActivated || moduleAnimator != null)
		{
			yield return "sendtochaterror The module cannot be interacted with yet!";
			yield break;
		}

		switch (split[0])
		{
			case "RESET":
				if (split.Length > 1)
				{
					yield return "sendtochaterror Too many parameters!";
					yield break;
				}

				yield return null;
				Buttons[4].OnInteract();
				yield return new WaitForSeconds(1);
				Buttons[4].OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
				yield break;
			case "MOVE":
				if (split.Length == 1)
				{
					yield return "sendtochaterror Please specify what moves to make!";
					yield break;
				}

				if (split.Length > 2)
				{
					yield return "sendtochaterror Too many parameters!";
					yield break;
				}

				var letterToDir = new Dictionary<char, QMButton>
				{
					{ 'U', QMButton.Up },
					{ 'D', QMButton.Down },
					{ 'L', QMButton.Left },
					{ 'R', QMButton.Right },
					{ 'T', QMButton.Top },
					{ 'B', QMButton.Bottom },
					{ 'A', QMButton.Ana },
					{ 'K', QMButton.Kata }
				};

				if (!split[1].Any(letterToDir.ContainsKey))
				{
					var invalid = split[1].Where(x => !letterToDir.ContainsKey(x)).ToArray();

					yield return $"sendtochaterror {invalid.Join(", ")} {(invalid.Length > 1 ? "aren't" : "isn't")} valid character{(invalid.Length > 1 ? "s" : string.Empty)}!";
					yield break;
				}
				
				var directions = split[1].Select(x => letterToDir[x]).ToArray();

				yield return null;

				foreach (var direction in directions)
				{
					Buttons[(int)direction].OnInteract();
					yield return new WaitForSeconds(0.1f);
					Buttons[(int)direction].OnInteractEnded();
					yield return new WaitForSeconds(0.1f);
				}
				
				yield break;
			case "SUBMIT":
				if (split.Length > 1)
				{
					yield return "sendtochaterror Too many parameters!";
					yield break;
				}

				yield return null;
				Buttons[4].OnInteract();
				yield return new WaitForSeconds(0.1f);
				Buttons[4].OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
				yield return "solve";
				yield break;
		}
		
		yield return null;
    }

	IEnumerator TwitchHandleForcedSolve()
	{
		while (moduleAnimator != null || !isActivated)
		{
			if (moduleSolved)
				yield break;

			yield return true;
		}
		
		var paths = ObtainPaths();

		foreach (var path in paths)
		{
			for (int i = path.Count - 1; i >= 0; i--)
			{
				Buttons[(int)path[i]].OnInteract();
				yield return new WaitForSeconds(0.1f);
				Buttons[(int)path[i]].OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
			}

			Buttons[4].OnInteract();
			yield return new WaitForSeconds(0.1f);
			Buttons[4].OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
		}

		while (!moduleSolved)
			yield return true;
	}


}