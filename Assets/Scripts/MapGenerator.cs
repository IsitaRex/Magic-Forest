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
    private GameObject currentTemple; // Store a reference to the current temple
    private bool[,] gridCellOccupied; // Tracks which cells in the 5x5 grid are occupied
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
        gridCellOccupied = new bool[5, 5]; // Initialize the grid cell occupancy array

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
        BuildClearingRegion(4, 0.7f, 10);
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
            // ClearTrees();
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
        // Call function to connect clearings
        ConnectClearings();
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

    // Try to find a valid location for the clearing
    int maxAttempts = 100; // Limit the number of attempts to avoid infinite loops
    int attempts = 0;
    bool validLocation = false;
    int gridX = 0, gridY = 0;

    do
    {
        // Choose a random cell in the 5x5 grid
        gridX = pseudoRandom.Next(0, 5);
        gridY = pseudoRandom.Next(0, 5);

        // Check if the cell is already occupied
        if (!gridCellOccupied[gridX, gridY])
        {
            // Check if the new location is far enough from all existing clearings
            validLocation = true;
            foreach (Vector2Int existingClearing in clearingLocations)
            {
                int existingGridX = existingClearing.x / cellWidth;
                int existingGridY = existingClearing.y / cellHeight;

                // Check if the new location is in the same or adjacent grid cell
                if (Mathf.Abs(gridX - existingGridX) <= 1 && Mathf.Abs(gridY - existingGridY) <= 1)
                {
                    validLocation = false;
                    break;
                }
            }
        }

        attempts++;
    } while (!validLocation && attempts < maxAttempts);

    // If no valid location is found, pick a random available cell
    if (!validLocation)
    {
        Debug.LogWarning("Could not find a valid location after " + maxAttempts + " attempts. Picking a random available cell.");
        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                if (!gridCellOccupied[x, y])
                {
                    availableCells.Add(new Vector2Int(x, y));
                }
            }
        }

        if (availableCells.Count > 0)
        {
            Vector2Int randomCell = availableCells[pseudoRandom.Next(0, availableCells.Count)];
            gridX = randomCell.x;
            gridY = randomCell.y;
        }
        else
        {
            Debug.LogError("No available cells in the 5x5 grid.");
            return;
        }
    }

    // Mark the chosen cell as occupied
    gridCellOccupied[gridX, gridY] = true;

    // Calculate the center of the chosen cell
    int clearingX = gridX * cellWidth + cellWidth / 2;
    int clearingY = gridY * cellHeight + cellHeight / 2;
    newClearingLocation = new Vector2Int(clearingX, clearingY);

    // Set the new clearing location
    clearingLocation = newClearingLocation;
    clearingLocations.Add(clearingLocation); // Store the new clearing location

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

        // Set this cell to 0 (clearing) based on probability
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
        if (currentLayer > maxLayer)
        {
            break; // Stop if we exceed the maximum layer
        }
    }
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

// Function to connect all consecutive clearings with paths
void ConnectClearings()
{

    // Connect consecutive clearings
    for (int i = 0; i < clearingLocations.Count - 1; i++)
    {
        Vector2Int start = clearingLocations[i];
        Vector2Int end = clearingLocations[i + 1];
        
        // Parameters for path generation
        int numSamplePoints = 13; // Number of points to sample along the shortest path
        float curviness = 0.9f; // How much the path should curve (0-1)
        int pathThickness = 1; // Thickness of the path
        
        // Generate an S-shaped path between the two clearings
        CreateSShapedPath(start, end, numSamplePoints, curviness, pathThickness);
    }
}

// Create an S-shaped path between two points
void CreateSShapedPath(Vector2Int start, Vector2Int end, int numSamplePoints, float curviness, int pathThickness)
{
    // Step 1: Find the shortest path using A*
    List<Vector2Int> shortestPath = FindShortestPath(start, end);
    
    // If no path was found, create a fallback path
    if (shortestPath.Count == 0)
    {
        Debug.Log($"No path found between {start} and {end}. Creating fallback path.");
        shortestPath = CreateFallbackPath(start, end);
    }
    
    // Step 2: Sample points from the path
    List<Vector2Int> sampledPoints = SamplePathPoints(shortestPath, numSamplePoints);
    
    // Step 3: Generate an S-shaped curve using cubic interpolation
    List<Vector2Int> sShapedPath = InterpolateCubicPath(sampledPoints, curviness);
    
    // Step 4: Draw the path with the specified thickness
    DrawPathWithThickness(sShapedPath, pathThickness);
}

// Find the shortest path between two points using A* algorithm
List<Vector2Int> FindShortestPath(Vector2Int start, Vector2Int end)
{
    // A* algorithm implementation
    Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
    Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
    Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
    
    // Priority queue implemented with a sorted list
    List<KeyValuePair<float, Vector2Int>> openSet = new List<KeyValuePair<float, Vector2Int>>();
    HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
    
    // Initialize scores
    gScore[start] = 0;
    fScore[start] = HeuristicCost(start, end);
    openSet.Add(new KeyValuePair<float, Vector2Int>(fScore[start], start));
    
    while (openSet.Count > 0)
    {
        // Get the node with the lowest fScore
        Vector2Int current = openSet[0].Value;
        openSet.RemoveAt(0);
        
        if (current.Equals(end))
        {
            // Reconstruct path
            return ReconstructPath(cameFrom, current);
        }
        
        closedSet.Add(current);
        
        // Check all neighbors
        Vector2Int[] directions = {
            new Vector2Int(0, 1),    // up
            new Vector2Int(1, 0),    // right
            new Vector2Int(0, -1),   // down
            new Vector2Int(-1, 0),   // left
            new Vector2Int(1, 1),    // diagonal up-right
            new Vector2Int(1, -1),   // diagonal down-right
            new Vector2Int(-1, -1),  // diagonal down-left
            new Vector2Int(-1, 1)    // diagonal up-left
        };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = new Vector2Int(current.x + dir.x, current.y + dir.y);
            
            // Check if the neighbor is valid
            if (!IsInMapRange(neighbor.x, neighbor.y) || closedSet.Contains(neighbor))
                continue;
            
            // Check if the neighbor is a wall (value = 1)
            if (map[neighbor.x, neighbor.y] == 1)
                continue;
            
            // Calculate tentative gScore
            float tentativeGScore = gScore[current] + 
                (dir.x != 0 && dir.y != 0 ? 1.414f : 1f); // Diagonal cost is sqrt(2)
            
            if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
            {
                // This path is better than any previous one
                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + HeuristicCost(neighbor, end);
                
                // Add to open set if not already there
                bool found = false;
                for (int i = 0; i < openSet.Count; i++)
                {
                    if (openSet[i].Value.Equals(neighbor))
                    {
                        found = true;
                        openSet[i] = new KeyValuePair<float, Vector2Int>(fScore[neighbor], neighbor);
                        break;
                    }
                }
                
                if (!found)
                {
                    openSet.Add(new KeyValuePair<float, Vector2Int>(fScore[neighbor], neighbor));
                }
                
                // Keep the open set sorted by fScore
                openSet.Sort((a, b) => a.Key.CompareTo(b.Key));
            }
        }
    }
    
    // No path found
    return new List<Vector2Int>();
}

// Create a fallback path when A* fails to find a route
List<Vector2Int> CreateFallbackPath(Vector2Int start, Vector2Int end)
{
    List<Vector2Int> fallbackPath = new List<Vector2Int>();
    
    // Add start point
    fallbackPath.Add(start);
    
    // Add a midpoint to create a slight curve
    Vector2Int midPoint = new Vector2Int(
        (start.x + end.x) / 2, 
        (start.y + end.y) / 2
    );
    
    // Add some variance to the midpoint to create a curved path
    System.Random random = new System.Random(seed.GetHashCode() + start.x * 1000 + end.y);
    int offsetRange = Mathf.Max(5, Mathf.Min(width, height) / 10);
    midPoint.x += random.Next(-offsetRange, offsetRange + 1);
    midPoint.y += random.Next(-offsetRange, offsetRange + 1);
    
    // Make sure the midpoint is within map bounds
    midPoint.x = Mathf.Clamp(midPoint.x, 1, width - 2);
    midPoint.y = Mathf.Clamp(midPoint.y, 1, height - 2);
    
    // Add the midpoint
    fallbackPath.Add(midPoint);
    
    // Add end point
    fallbackPath.Add(end);
    
    // Now add points along the lines between start, mid and end
    List<Vector2Int> detailedPath = new List<Vector2Int>();
    
    // Add points between start and mid
    AddPointsAlongLine(detailedPath, start, midPoint);
    
    // Add points between mid and end
    AddPointsAlongLine(detailedPath, midPoint, end);
    
    return detailedPath;
}

// Add points along a line using Bresenham's line algorithm
void AddPointsAlongLine(List<Vector2Int> path, Vector2Int start, Vector2Int end)
{
    int x0 = start.x;
    int y0 = start.y;
    int x1 = end.x;
    int y1 = end.y;
    
    int dx = Mathf.Abs(x1 - x0);
    int dy = Mathf.Abs(y1 - y0);
    int sx = x0 < x1 ? 1 : -1;
    int sy = y0 < y1 ? 1 : -1;
    int err = dx - dy;
    
    while (true)
    {
        path.Add(new Vector2Int(x0, y0));
        
        if (x0 == x1 && y0 == y1)
            break;
        
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
// Calculate heuristic (Manhattan distance)
float HeuristicCost(Vector2Int a, Vector2Int b)
{
    return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}

// Reconstruct the path from start to end
List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
{
    List<Vector2Int> path = new List<Vector2Int>();
    path.Add(current);
    
    while (cameFrom.ContainsKey(current))
    {
        current = cameFrom[current];
        path.Insert(0, current);
    }
    
    return path;
}

// Sample points from the path
List<Vector2Int> SamplePathPoints(List<Vector2Int> path, int numSamplePoints)
{
    List<Vector2Int> sampledPoints = new List<Vector2Int>();
    
    // Always include start and end points
    sampledPoints.Add(path[0]);
    
    if (path.Count <= 2)
    {
        // If path is too short, just return start and end
        sampledPoints.Add(path[path.Count - 1]);
        return sampledPoints;
    }
    
    // Calculate sample intervals
    float step = (float)(path.Count - 1) / (numSamplePoints - 1);
    
    // Add intermediate points
    for (int i = 1; i < numSamplePoints - 1; i++)
    {
        int index = Mathf.FloorToInt(i * step);
        sampledPoints.Add(path[index]);
    }
    
    // Add end point
    sampledPoints.Add(path[path.Count - 1]);
    
    return sampledPoints;
}

// Create an S-shaped path using cubic interpolation
List<Vector2Int> InterpolateCubicPath(List<Vector2Int> controlPoints, float curviness)
{
    if (controlPoints.Count < 2)
        return controlPoints;
    
    List<Vector2Int> resultPath = new List<Vector2Int>();
    int numSegments = 20; // Number of points to generate per segment
    
    for (int i = 0; i < controlPoints.Count - 1; i++)
    {
        Vector2 p0 = i > 0 ? controlPoints[i - 1] : controlPoints[i];
        Vector2 p1 = controlPoints[i];
        Vector2 p2 = controlPoints[i + 1];
        Vector2 p3 = i + 2 < controlPoints.Count ? controlPoints[i + 2] : p2 + (p2 - p1);
        
        // Apply curviness to control points
        Vector2 dir1 = (p2 - p0).normalized * curviness;
        Vector2 dir2 = (p3 - p1).normalized * curviness;
        Vector2 cp1 = p1 + dir1 * Vector2.Distance(p1, p2) * 0.5f;
        Vector2 cp2 = p2 - dir2 * Vector2.Distance(p1, p2) * 0.5f;
        
        // Calculate cubic Bezier curve points
        for (int j = 0; j <= numSegments; j++)
        {
            float t = j / (float)numSegments;
            Vector2 point = CubicBezier(p1, cp1, cp2, p2, t);
            resultPath.Add(new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)));
        }
    }
    
    return resultPath;
}

// Calculate cubic Bezier point
Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
{
    float u = 1 - t;
    float tt = t * t;
    float uu = u * u;
    float uuu = uu * u;
    float ttt = tt * t;
    
    Vector2 p = uuu * p0; // (1-t)^3 * P0
    p += 3 * uu * t * p1; // 3(1-t)^2 * t * P1
    p += 3 * u * tt * p2; // 3(1-t) * t^2 * P2
    p += ttt * p3; // t^3 * P3
    
    return p;
}

// Draw the path with the specified thickness
void DrawPathWithThickness(List<Vector2Int> path, int thickness)
{
    foreach (Vector2Int point in path)
    {
        // Draw a "circle" of the specified thickness
        for (int xOffset = -thickness; xOffset <= thickness; xOffset++)
        {
            for (int yOffset = -thickness; yOffset <= thickness; yOffset++)
            {
                int x = point.x + xOffset;
                int y = point.y + yOffset;
                
                // Check if within circle and map bounds
                if (xOffset * xOffset + yOffset * yOffset <= thickness * thickness && 
                    IsInMapRange(x, y))
                {
                    map[x, y] = 0; // Set to empty space (0)
                }
            }
        }
    }
}
}