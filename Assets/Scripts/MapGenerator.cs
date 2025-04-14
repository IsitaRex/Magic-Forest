using NPBehave;
using UnityEngine;
using System;
using System.Collections.Generic;

/*
 * Based on "Procedural Cave Generation" by Sebastian Lague
 * https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9
 */
public class MapGenerator : MonoBehaviour
{

	public int width;
	public int height;

	public string seed;
	public bool useRandomSeed;

	[Range(0, 100)]
	public int randomFillPercent;

	int[,] map;
	private GameObject dragon; // Reference to the dragon
	private float dragonMoveSpeed = 0.5f; // Time in seconds between movements
	private Vector3 dragonScale = new Vector3(1, 2, 1); // Default scale
	public int numberOfDragons = 3; // Number of dragons to place
	private NPBehave.Root dragonBehaviorTree;
	private List<GameObject> dragons = new List<GameObject>(); // List to store all dragon instances
	private GameObject[] treePrefabs; // Array to store the tree prefabs
	private List<GameObject> placedTrees = new List<GameObject>(); // List to store all instantiated trees
	private Vector2Int templeLocation; // Store the location of the temple
	
    private Vector2Int clearingLocation; // Store the location of the temple
    private List<Vector2Int> clearingLocations = new List<Vector2Int>(); // Store the locations of the clearings of the dragons and temple
	private List<Vector3Int> spiralLocation =  new List<Vector3Int>(); // Store the location of the spiral
	/*
	 * Generate the map on start, on mouse click
	 */
	void Start()
	{
		treePrefabs = new GameObject[]
    {
        Resources.Load<GameObject>("Tree1"),
        Resources.Load<GameObject>("Tree2"),
        Resources.Load<GameObject>("Tree3")
    };

		GenerateMap();
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			GenerateMap();
		}
	}

	void GenerateMap()
	{

        // Clear previous map data
        ClearMapData();

        // Initialize a new map
        map = new int[width, height];

		// Stage 1: populate the grid cells
		PopulateMap();

		// Stage 2: apply cellular automata rules
		for (int i = 0; i < 5; i++)
		{
			SmoothMap();
		}

		// Stage 3: finalise the map
		ProcessMap();
		AddMapBorder();

		// Generate mesh
		MeshGenerator meshGen = GetComponent<MeshGenerator>();
		meshGen.GenerateMesh(map, 1);
		 // Place the dragon
        
	}

	/*
	 * STAGE 1: Populate the map
	 */
	void PopulateMap()
    {
		if (useRandomSeed)
		{
			seed = Time.time.ToString();
		}

		System.Random pseudoRandom = new System.Random(seed.GetHashCode());
        FillMap(); // Fill the map with walls
        int maxLayers = 15;
        BuildClearingRegion(13, 0.9f, maxLayers); // Build the temple region
        // GenerateSpiralWalls(clearingLocation, pseudoRandom, new Vector2Int(4, 6), new Vector2Int(100, 150), 0.2f);

        // Build The clearing regions for the dragons
        for (int i = 0; i < 4; ++i)
        {
            BuildClearingRegion(8, 0.7f, maxLayers);
            // GenerateSpiralWalls(clearingLocation, pseudoRandom, new Vector2Int(4, 6), new Vector2Int(50, 60), 0.7f);
        }
    }
	
    void PopulateMapSpirals(){
        if (useRandomSeed)
		{
			seed = Time.time.ToString();
		}

		System.Random pseudoRandom = new System.Random(seed.GetHashCode());
        GenerateSpiralWalls(clearingLocations[0], pseudoRandom, new Vector2Int(4, 6), new Vector2Int(100, 150), 0.2f);
        for(int i = 1; i < clearingLocations.Count; ++i)
        {
            GenerateSpiralWalls(clearingLocations[i], pseudoRandom, new Vector2Int(4, 6), new Vector2Int(50, 60), 0.7f);
        }
    }
	void FillMap()
	{
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
                map[x, y] = 1;
			}
		}
		
	}

	/*
	 * STAGE 2: Smooth map with CA
	 */
	void SmoothMap()
	{
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
                // if(map[x, y] == 2) continue; // Skip if a spiral
				int neighbourWallTiles = GetSurroundingWallCount(x, y);

				if (neighbourWallTiles > 4)
					map[x, y] = 1;
				else if (neighbourWallTiles < 4)
					map[x, y] = 0;

			}
		}
	}

	int GetSurroundingWallCount(int gridX, int gridY)
	{
		int wallCount = 0;
		for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
		{
			for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
			{
				if (IsInMapRange(neighbourX, neighbourY))
				{
					if (neighbourX != gridX || neighbourY != gridY)
					{
						wallCount += (map[neighbourX, neighbourY]!= 0) ? 1 : 0;
					}
				}
				else
				{
					wallCount++;
				}
			}
		}

		return wallCount;
	}

	bool IsInMapRange(int x, int y)
	{
		return x >= 0 && x < width && y >= 0 && y < height;
	}

void ClearTrees()
{
    foreach (GameObject tree in placedTrees)
    {
        if (tree != null)
        {
            Destroy(tree);
        }
    }
    placedTrees.Clear(); // Clear the list after destroying the trees
}
void PlaceTrees()
{
		ClearTrees();
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            // Only place trees on non-empty spots
            if (map[x, y] != 0 && UnityEngine.Random.Range(0, 100) < 100) // 20% chance to place a tree
            {
                // Select a random tree prefab
                GameObject treePrefab = treePrefabs[UnityEngine.Random.Range(0, treePrefabs.Length)];

                // Calculate the position for the tree
                Vector3 position = new Vector3(
                    -width / 2 + x + 0.5f,
                    0, // Adjust Y-axis if needed
                    -height / 2 + y + 0.5f
                );

                // Instantiate the tree
                GameObject tree = Instantiate(treePrefab, position, Quaternion.identity);

                // Scale down the tree
                tree.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f); // Adjust scale as needed

                // Add the tree to the list
                placedTrees.Add(tree);
            }
        }
    }
}

	/*
	 * Stage 3: produce the finished map
	 */
	void ProcessMap()
	{
        PopulateMapSpirals();
		// Repaint();
        // ConnectIslands(5); 
        // GenerateBorderSpirals(10, new System.Random(seed.GetHashCode())); // Generate 10 spirals at random border points
		PlaceTrees();
        PlaceObjects();
	}

	void AddMapBorder()
	{
		int borderSize = 1;
		int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

		for (int x = 0; x < borderedMap.GetLength(0); x++)
		{
			for (int y = 0; y < borderedMap.GetLength(1); y++)
			{
				if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
				{
					borderedMap[x, y] = map[x - borderSize, y - borderSize];
				}
				else
				{
					borderedMap[x, y] = 1;
				}
			}
		}
		map = borderedMap;
	}

void AddDragonBehavior(GameObject dragon, Vector2Int startPosition)
{
    // Create the behavior tree
    var moveAction = new NPBehave.Action(() =>
    {
        // Find adjacent empty slots
        List<Vector2Int> adjacentSlots = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int newPos = startPosition + dir;
            if (newPos.x >= 0 && newPos.x < width && newPos.y >= 0 && newPos.y < height && map[newPos.x, newPos.y] == 0)
            {
                adjacentSlots.Add(newPos);
            }
        }

        if (adjacentSlots.Count > 0)
        {
            // Move to a random adjacent slot
            Vector2Int targetSlot = adjacentSlots[UnityEngine.Random.Range(0, adjacentSlots.Count)];
            Vector3 targetPosition = new Vector3(
                -width / 2 + targetSlot.x + 0.5f,
                0,
                -height / 2 + targetSlot.y + 0.5f
            );

            if (dragon != null) // Ensure the dragon still exists
            {
                dragon.transform.position = targetPosition;
                startPosition = targetSlot; // Update the dragon's position
            }
        }
    });

    // Add a Wait node to control the speed of movement
    var waitNode = new NPBehave.Wait(dragonMoveSpeed);

    // Sequence: Wait -> Move
    var sequence = new NPBehave.Sequence(waitNode, moveAction);

    // Create the behavior tree and store it
    dragonBehaviorTree = new NPBehave.Root(new NPBehave.Repeater(sequence));
    dragonBehaviorTree.Start();
}
void BuildClearingRegion(int guaranteedLayers, float probabilityDecayRate, int maxLayer)
{
    System.Random pseudoRandom = new System.Random(seed.GetHashCode());
    Vector2Int newClearingLocation;

    // Divide the map into a 5x5 grid
    int cellWidth = width / 5;
    int cellHeight = height / 5;

    // Try to find a valid location for the temple
    int maxAttempts = 100; // Limit the number of attempts to avoid infinite loops
    int attempts = 0;
    bool validLocation = false;

    do
    {
        // Choose a random cell in the 5x5 grid
        int gridX = pseudoRandom.Next(0, 5);
        int gridY = pseudoRandom.Next(0, 5);

        // Calculate the center of the chosen cell
        int templeX = gridX * cellWidth + cellWidth / 2;
        int templeY = gridY * cellHeight + cellHeight / 2;
        newClearingLocation = new Vector2Int(templeX, templeY);

        // Check if the new location is far enough from all existing temples
        validLocation = true;
        foreach (Vector2Int existingClearing in clearingLocations)
        {
            // Check if the new location is in the same or adjacent grid cell
            int existingGridX = existingClearing.x / cellWidth;
            int existingGridY = existingClearing.y / cellHeight;

            if (Mathf.Abs(gridX - existingGridX) <= 1 && Mathf.Abs(gridY - existingGridY) <= 1)
            {
                validLocation = false;
                break;
            }
        }

        attempts++;
    } while (!validLocation && attempts < maxAttempts);

    if (!validLocation)
    {
        Debug.LogWarning("Could not find a valid location for the temple after " + maxAttempts + " attempts.");
        return;
    }

    // Set the new temple location
    clearingLocation = newClearingLocation;
    clearingLocations.Add(clearingLocation); // Store the new temple location

    // BFS queue
    Queue<Vector2Int> queue = new Queue<Vector2Int>();
    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

    queue.Enqueue(clearingLocation);
    visited.Add(clearingLocation);

    // Start with 100% probability
    float currentProbability = 1f;
    int currentLayer = 0;
    int nodesInCurrentLayer = 1;
    int nodesInNextLayer = 0;

    while (queue.Count > 0)
    {
        Vector2Int cell = queue.Dequeue();

        // Set this cell to 1 (land) based on probability
        if (currentLayer <= guaranteedLayers || pseudoRandom.NextDouble() < currentProbability)
        {
            map[cell.x, cell.y] = 0;
        }

        // Explore neighbors
        Vector2Int[] directions = {
            new Vector2Int(0, 1),    // up
            new Vector2Int(1, 0),    // right
            new Vector2Int(0, -1),   // down
            new Vector2Int(-1, 0)    // left
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = new Vector2Int(cell.x + dir.x, cell.y + dir.y);

            // Check bounds and if already visited
            if (neighbor.x > 0 && neighbor.x < width - 1 &&
                neighbor.y > 0 && neighbor.y < height - 1 &&
                !visited.Contains(neighbor))
            {
                queue.Enqueue(neighbor);
                visited.Add(neighbor);
                nodesInNextLayer++;
            }
        }

        nodesInCurrentLayer--;
        if (nodesInCurrentLayer == 0)
        {
            nodesInCurrentLayer = nodesInNextLayer;
            nodesInNextLayer = 0;
            currentLayer++;

            // Reduce probability after guaranteed layers
            if (currentLayer > guaranteedLayers)
            {
                currentProbability *= probabilityDecayRate;
            }
        }
        if(currentLayer > maxLayer)
        {
            break; // Stop if we exceed the maximum layer
        }
    }
}

void GenerateRandomSpirals(System.Random pseudoRandom){
	// go trough spiralLocation and generate a random spiral
	for (int i = 0; i < spiralLocation.Count; i++)
	{
		Vector2Int center =new Vector2Int(spiralLocation[i][0], spiralLocation[i][1]);
		int maxSpirals = pseudoRandom.Next(1, 1);  // Number of spirals to generate
		int maxLength = pseudoRandom.Next(50, 100); // Random length for each spiral
		int currentAngle = spiralLocation[i][2];
		// Random select an angle between currentAngle - PI/2 and currentAngle + PI/2
		float angleRange = Mathf.PI / 2;
		float minAngle = currentAngle - angleRange;
		float maxAngle = currentAngle + angleRange;
		float startAngle = (float)pseudoRandom.NextDouble() * (maxAngle - minAngle) + minAngle;
		
		float growthFactor = 0.3f; // Growth factor for the spiral
		float startDistance = 0.01f; // Starting distance from the center
		int direction = pseudoRandom.Next(0, 2) == 0 ? 1 : -1;
		int futureSpiralPlacement = pseudoRandom.Next(0, maxLength); //Random placement for a potential new spiral
		GenerateEulerSpiral(center, startAngle, growthFactor, startDistance, maxLength, pseudoRandom, direction, futureSpiralPlacement);
	}
	// clear the spiralLocation list
	spiralLocation.Clear();
}
void GenerateSpiralWalls(Vector2Int center, System.Random pseudoRandom, Vector2Int maxSpiralsInterval, Vector2Int maxSpiralsLengthInterval, float growthFactor )
{
    // Parameters for spiral generation
    // int maxSpirals = 6;  // Number of spirals to generate
    int maxSpirals = pseudoRandom.Next(maxSpiralsInterval[0], maxSpiralsInterval[1]);  // Number of spirals to generate

    // int maxLength = Mathf.Min(width, height) / 2;  // Maximum length of each spiral
    int maxLength = 100;  // Maximum length of each spiral
    int direction = pseudoRandom.Next(0, 2) == 0 ? 1 : -1;
    float increaseAngle = 2*Mathf.PI/maxSpirals; // Starting angle for the spiral
    float startAngle = 0; // Starting angle for the spiral
    float startDistance = 0.01f; // Starting distance from the center
    // GenerateEulerSpiral(center, startAngle, growthFactor, startDistance, maxLength, pseudoRandom);
    
    for (int i = 0; i < maxSpirals; i++)
    {
        // Random angle to start the spiral
        // startAngle = (float)pseudoRandom.NextDouble() * 2 * Mathf.PI;
        startAngle = i * increaseAngle; // Incremental angle for each spiral
        maxLength = pseudoRandom.Next(maxSpiralsLengthInterval[0], maxSpiralsLengthInterval[1]); // Random length for each spiral
        int futureSpiralPlacement = pseudoRandom.Next((int)Math.Floor((double)maxLength/3), (int)Math.Floor((double)maxLength/3)); //Random placement for a potential new spiral 
        // Generate the spiral
        GenerateEulerSpiral(center, startAngle, growthFactor, startDistance, maxLength, pseudoRandom, direction, futureSpiralPlacement);
    }
}

void GenerateEulerSpiral(Vector2Int center, float startAngle, float growthFactor, float startDistance, int maxLength, System.Random pseudoRandom, int direction, int futureSpiralPlacement)
{
    Vector2 currentPos = new Vector2(center.x, center.y);
    float angle = startAngle;
    float distance = startDistance;
    // Create wall segments
    for (int i = 0; i < maxLength; i++)
    {
        // Calculate next position using Euler spiral formula
        // In an Euler spiral, the curvature increases linearly with distance
        angle += direction* growthFactor * distance / 10f;
        
        // Calculate next position
        Vector2 nextPos = currentPos + new Vector2(
            Mathf.Cos(angle) * 1.0f,  // Step size of 1 unit per iteration
            Mathf.Sin(angle) * 1.0f
        );
        
        // Convert to grid coordinates and bound check
        int gridX = Mathf.RoundToInt(nextPos.x);
        int gridY = Mathf.RoundToInt(nextPos.y);
        
        // Ensure we're still in bounds
        if (gridX > 0 && gridX < width - 1 && gridY > 0 && gridY < height - 1)
        {
            // Place a wall
            // map[gridX, gridY] = 0;
            DrawLine((int)currentPos.x, (int)currentPos.y, gridX, gridY);

						// check if the current position is the futureSpiralPlacement
						if (i == futureSpiralPlacement)
						{
							spiralLocation.Add(new Vector3Int(gridX, gridY, (int)startAngle));
						}
            
            
            
            // Update current position
            currentPos = nextPos;
            
            // Increase distance for spiral effect
            distance += 0.1f;
        }
        else
        {
            // If we've gone out of bounds, stop this spiral
            break;
        }
    }
}
void DrawLine(int x0, int y0, int x1, int y1)
{
    int dx = Mathf.Abs(x1 - x0);
    int dy = Mathf.Abs(y1 - y0);
    int sx = (x0 < x1) ? 1 : -1;
    int sy = (y0 < y1) ? 1 : -1;
    int err = dx - dy;

    while (true)
    {
        // Place a wall at the current position
        map[x0, y0] = 0;

        // Check if we've reached the end point
        if (x0 == x1 && y0 == y1) break;

        int e2 = 2 * err;
        if (e2 > -dy)
        {
            err -= dy;
            x0 += sx;
        }
        if (e2 < dx)
        {
            err += dx;
            y0 += sy;
        }
    }
}

void ClearMapData()
{
    // Clear clearing locations
    clearingLocations.Clear();

    // Clear spiral locations
    spiralLocation.Clear();

    // Clear placed trees
    foreach (GameObject tree in placedTrees)
    {
        if (tree != null)
        {
            Destroy(tree);
        }
    }
    placedTrees.Clear();

    // Clear dragons
    foreach (GameObject dragon in dragons)
    {
        if (dragon != null)
        {
            Destroy(dragon);
        }
    }
    dragons.Clear();

    // Destroy the temple
    if (currentTemple != null)
    {
        Destroy(currentTemple);
        currentTemple = null;
    }

    // Stop the dragon behavior tree if it exists
    if (dragonBehaviorTree != null)
    {
        dragonBehaviorTree.Stop();
        dragonBehaviorTree = null;
    }

    Debug.Log("Map data cleared.");
}
void Repaint(){
	// this function should transform all 0s in the map to 1s and the 2s to 0s
	for (int x = 0; x < width; x++)
	{
		for (int y = 0; y < height; y++)
		{
			if (map[x, y] == 2)
			{

				map[x, y] = 0;
			}
		}
	}
}

private GameObject currentTemple; // Store a reference to the current temple

void PlaceObjects()
{
    // Load the temple prefab
    GameObject templePrefab = Resources.Load<GameObject>("Temple");
    if (templePrefab == null)
    {
        Debug.LogError("Temple prefab not found in Resources folder!");
        return;
    }

    // Place the temple at the first clearing location
    if (clearingLocations.Count > 0)
    {
        Vector2Int templeLocation = clearingLocations[0];
        Vector3 templePosition = new Vector3(
            -width / 2 + templeLocation.x + 0.5f,
            0, // Adjust Y-axis if needed
            -height / 2 + templeLocation.y + 0.5f
        );

        // Instantiate the temple and store the reference
        currentTemple = Instantiate(templePrefab, templePosition, Quaternion.Euler(90, 0, 0));
        currentTemple.name = "Temple"; // Ensure the temple has a consistent name for debugging
    }

    // Load the dragon prefabs
    for (int i = 1; i <= 4; i++)
    {
        GameObject dragonPrefab = Resources.Load<GameObject>($"Dragon{i}");
        if (dragonPrefab == null)
        {
            Debug.LogError($"Dragon{i} prefab not found in Resources folder!");
            continue;
        }

        // Place dragons at the other clearing locations
        if (i < clearingLocations.Count)
        {
            Vector2Int dragonLocation = clearingLocations[i];
            Vector3 dragonPosition = new Vector3(
                -width / 2 + dragonLocation.x + 0.5f,
                0, // Adjust Y-axis if needed
                -height / 2 + dragonLocation.y + 0.5f
            );

            // Instantiate the dragon
            GameObject dragon = Instantiate(dragonPrefab, dragonPosition, Quaternion.Euler(90, 0, 0));

            // Apply the scale
            dragon.transform.localScale = dragonScale;

            // Add the dragon to the list
            dragons.Add(dragon);

            // Add the behavior tree for movement
            AddDragonBehavior(dragon, dragonLocation);
        }
    }
}

void ConnectIslands(int stepsBeforeSpiral)
{
    if (clearingLocations.Count < 2)
    {
        Debug.LogWarning("Not enough islands to connect.");
        return;
    }

    System.Random pseudoRandom = new System.Random(seed.GetHashCode());

    // Iterate through all pairs of clearing locations
    for (int i = 0; i < clearingLocations.Count - 1; i++)
    {
        Vector2Int start = clearingLocations[i];
        Vector2Int end = clearingLocations[i + 1];

        // Draw a line to connect the two islands
        // ConnectWithLine(start, end);
        ConnectWithCubicInterpolation(start, end, pseudoRandom, 2.0f, 1);
    }
}

void ConnectWithLine(Vector2Int start, Vector2Int end)
{
    int x0 = start.x;
    int y0 = start.y;
    int x1 = end.x;
    int y1 = end.y;

    int dx = Mathf.Abs(x1 - x0);
    int dy = Mathf.Abs(y1 - y0);
    int sx = (x0 < x1) ? 1 : -1;
    int sy = (y0 < y1) ? 1 : -1;
    int err = dx - dy;

    int stepCounter = 0;

    while (true)
    {
        map[x0, y0] = 0; // Mark the path with 2
        // Check if we've reached the end point
        if (x0 == x1 && y0 == y1) break;

        int e2 = 2 * err;
        if (e2 > -dy)
        {
            err -= dy;
            x0 += sx;
        }
        if (e2 < dx)
        {
            err += dx;
            y0 += sy;
        }

        stepCounter++;
    }
}
void GenerateSpiralFromPoint(Vector2Int center, System.Random pseudoRandom)
{
    // Parameters for the spiral
    float startAngle = (float)pseudoRandom.NextDouble() * 2 * Mathf.PI; // Random starting angle
    float growthFactor = 0.3f; // Growth factor for the spiral
    float startDistance = 0.01f; // Starting distance from the center
    int maxLength = pseudoRandom.Next(30, 50); // Random length for the spiral
    int direction = pseudoRandom.Next(0, 2) == 0 ? 1 : -1; // Random direction (clockwise or counterclockwise)

    // Generate the spiral
    GenerateEulerSpiral(center, startAngle, growthFactor, startDistance, maxLength, pseudoRandom, direction, -1);
}

void GenerateBorderSpirals(int numberOfSpirals, System.Random pseudoRandom)
{
    for (int i = 0; i < numberOfSpirals; i++)
    {
        // Select a random border position
        Vector2Int borderPoint = GetRandomBorderPoint(pseudoRandom);

        // Generate a spiral from the selected border point
        GenerateSpiralFromPoint(borderPoint, pseudoRandom);
    }
}

Vector2Int GetRandomBorderPoint(System.Random pseudoRandom)
{
    // Determine which border to use: 0 = top, 1 = bottom, 2 = left, 3 = right
    int border = pseudoRandom.Next(0, 4);

    switch (border)
    {
        case 0: // Top border
            return new Vector2Int(pseudoRandom.Next(0, width), 0);
        case 1: // Bottom border
            return new Vector2Int(pseudoRandom.Next(0, width), height - 1);
        case 2: // Left border
            return new Vector2Int(0, pseudoRandom.Next(0, height));
        case 3: // Right border
            return new Vector2Int(width - 1, pseudoRandom.Next(0, height));
        default:
            return new Vector2Int(0, 0); // Fallback (shouldn't happen)
    }
}

void ConnectWithCubicInterpolation(Vector2Int start, Vector2Int end, System.Random pseudoRandom, float curveIntensity, int thickness)
{
    // Define control points for the cubic Bézier curve
    Vector2 controlPoint1 = new Vector2(
        start.x + pseudoRandom.Next(-width / 4, width / 4) * curveIntensity,
        start.y + pseudoRandom.Next(-height / 4, height / 4) * curveIntensity
    );

    Vector2 controlPoint2 = new Vector2(
        end.x + pseudoRandom.Next(-width / 4, width / 4) * curveIntensity,
        end.y + pseudoRandom.Next(-height / 4, height / 4) * curveIntensity
    );

    // Number of steps to interpolate
    int steps = 50;

    // Generate the cubic Bézier curve
    for (int i = 0; i <= steps; i++)
    {
        float t = i / (float)steps;

        // Cubic Bézier formula
        Vector2 point = Mathf.Pow(1 - t, 3) * (Vector2)start +
                        3 * Mathf.Pow(1 - t, 2) * t * controlPoint1 +
                        3 * (1 - t) * Mathf.Pow(t, 2) * controlPoint2 +
                        Mathf.Pow(t, 3) * (Vector2)end;

        // Convert to grid coordinates
        int gridX = Mathf.RoundToInt(point.x);
        int gridY = Mathf.RoundToInt(point.y);

        // Ensure the point is within bounds and make the curve thicker
        SetThickLine(gridX, gridY, thickness);
    }
}

void SetThickLine(int x, int y, int thickness)
{
    for (int offsetX = -thickness; offsetX <= thickness; offsetX++)
    {
        for (int offsetY = -thickness; offsetY <= thickness; offsetY++)
        {
            int newX = x + offsetX;
            int newY = y + offsetY;

            // Ensure the new position is within bounds
            if (IsInMapRange(newX, newY))
            {
                map[newX, newY] = 0; // Set the cell to empty
            }
        }
    }
}
}