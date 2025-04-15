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
	private List<GameObject> dragons = new List<GameObject>(); // List to store all dragon instances
	private Vector3 dragonScale = new Vector3(1, 1, 1); // Default scale of the dragon
	private NPBehave.Root dragonBehaviorTree;
	private float dragonMoveSpeed = 0.5f; // Time in seconds between movements
    private GameObject[] dragonPrefabs; // Array to store dragon prefabs
	private GameObject[] treePrefabs; // Array to store tree prefabs
    private GameObject templePrefab; // Temple prefab
    private GameObject playerPrefab; // Player prefab
	private List<GameObject> placedTrees = new List<GameObject>(); // List to store all instantiated trees
	private Vector2Int templeLocation; // Store the location of the temple
    private List<Vector2Int> clearingLocations = new List<Vector2Int>(); // Store the locations of the clearings of the dragons, temple and player
    private GameObject temple; // Store a reference to the current temple
    private bool[,] gridCellOccupied; // Division of the map in regions of 5xt and tracks which cells are occupied
    private GameObject player; // Reference to the player instance 
	/*
	 * Generate the map on start, on mouse click
	 */
	void Start()
	{
		InitializePrefabs(); // Initialize the prefabs
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
        
	}

	/*
	 * STAGE 1: Populate the map (Build the clearing regions)
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

        // Build The clearing regions for the dragons
        for (int i = 0; i < 4; ++i)
        {
            BuildClearingRegion(8, 0.7f, maxLayers);
        }

        BuildClearingRegion(4, 0.7f, 10); // Build the clearing of the player
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
				int neighbourWallTiles = GetSurroundingWallCount(x, y);

				if (neighbourWallTiles > 4)
					map[x, y] = 1;
				else if (neighbourWallTiles < 4)
					map[x, y] = 0;

			}
		}
	}

    /*
	 * Stage 3: produce the finished map
	 */
	void ProcessMap()
    {
        PopulateMapSpirals(); // Generate the spiral paths from the clearings
        ConnectClearings(); // Connect the clearings with paths
        PlaceTrees(); // Place trees on the map
        PlaceObjects(); // Place the temple, dragons and player
    }

    // Fill the map with walls
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
	
    // Generate spiral paths around the clearings
    void PopulateMapSpirals(){
        // Generate bigger spiral walls around the temple
        GenerateSpiralWalls(clearingLocations[0], new Vector2Int(4, 6), new Vector2Int(100, 150), 0.2f);

        // Generate smaller spiral walls around the dragons and player
        for(int i = 1; i < clearingLocations.Count; ++i)
        {
            GenerateSpiralWalls(clearingLocations[i], new Vector2Int(4, 6), new Vector2Int(50, 60), 0.7f);
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

    // Place trees on the map
    void PlaceTrees()
    {
            // ClearTrees();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Only place trees on non-empty spots
                if (map[x, y] != 0) 
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
                    GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(90, 0, 0));

                    // Scale the trees randomly to add variety
                    float randomScale = UnityEngine.Random.Range(0.05f, 0.2f);
                    tree.transform.localScale = new Vector3( randomScale, randomScale, randomScale);

                    // Add the tree to the list
                    placedTrees.Add(tree);
                }
            }
        }
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
    
    // Build a clearing region
    void BuildClearingRegion(int guaranteedLayers, float probabilityDecayRate, int maxLayer)
    {
        // Step 1: Find a valid clearing location
        Vector2Int clearingLocation = FindClearingLocation();
        // Step 2: Perform BFS to create the clearing
        PerformBFSForClearing(clearingLocation, guaranteedLayers, probabilityDecayRate, maxLayer);
    }

    // Find a valid clearing location in the map
    Vector2Int FindClearingLocation()
    {   
        /*
            * Find a valid clearing location in the map
            * The location is chosen randomly from the 5x5 grid cells
            * A location is valid if:
            * 1. It is not already occupied by another clearing
            * 2. It is not too close to other clearings (adjacent or diagonal)
            * Trying to find a valid location for 100 attempts. If no valid location is found, pick a random available cell
        */

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        // Divide the map into a 5x5 grid
        int cellWidth = width / 5;
        int cellHeight = height / 5;

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
        }

        // Mark the chosen cell as occupied
        gridCellOccupied[gridX, gridY] = true;

        // Calculate the center of the chosen cell
        int clearingX = gridX * cellWidth + cellWidth / 2;
        int clearingY = gridY * cellHeight + cellHeight / 2;

        Vector2Int clearingLocation = new Vector2Int(clearingX, clearingY);
        clearingLocations.Add(clearingLocation); // Store the new clearing location

        return clearingLocation;
    }

    // Perform BFS to create a clearing region
    void PerformBFSForClearing(Vector2Int clearingLocation, int guaranteedLayers, float probabilityDecayRate, int maxLayer)
    {
        /*
            * Perform BFS to create a clearing region
            * The BFS starts from the clearing location and expands outwards
            * The probability of clearing a cell decreases with distance from the clearing location
            * The number of guaranteed layers is the number of layers that will be cleared with 100% probability
        */

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
            if (currentLayer <= guaranteedLayers || UnityEngine.Random.Range(0f, 1f) < currentProbability)  map[cell.x, cell.y] = 0;

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
                if (currentLayer > guaranteedLayers) currentProbability *= probabilityDecayRate;
            }
            if (currentLayer > maxLayer) break; // Stop if we exceed the maximum layer
        }
    }


    // Generate spiral walls around the clearings
    void GenerateSpiralWalls(Vector2Int center, Vector2Int maxSpiralsInterval, Vector2Int maxSpiralsLengthInterval, float growthFactor)
    {
        // Parameters for spiral generation
        int maxSpirals = UnityEngine.Random.Range(maxSpiralsInterval[0], maxSpiralsInterval[1]); // Number of spirals to generate
        int direction = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1; // Direction of the spirals (clockwise or counter-clockwise)
        float increaseAngle = 2 * Mathf.PI / maxSpirals; // Angle increment for each spiral
        float startDistance = 0.01f; // Starting distance from the center

        for (int i = 0; i < maxSpirals; i++)
        {
            // Random angle to start the spiral
            float startAngle = i * increaseAngle; // Incremental angle for each spiral
            int maxLength = UnityEngine.Random.Range(maxSpiralsLengthInterval[0], maxSpiralsLengthInterval[1]); // Random length for each spiral

            // Generate the spiral
            GenerateEulerSpiral(center, startAngle, growthFactor, startDistance, maxLength, direction);
        }
    }

    // Generate an Euler spiral
    void GenerateEulerSpiral(Vector2Int center, float startAngle, float growthFactor, float startDistance, int maxLength, int direction)
    {
        /*
            * Generate an Euler spiral
            * The spiral starts at the center and expands outwards
            * The angle and distance are incremented to create the spiral effect
        */

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
                // Draw the line between current and next position
                DrawLine((int)currentPos.x, (int)currentPos.y, gridX, gridY);
                
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

    // Draw a line between two points
    void DrawLine(int x0, int y0, int x1, int y1)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // Set the cell to 0 (path)
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

    // Clear all map data before generating a new map
    void ClearMapData()
    {
        // Clear clearing locations
        clearingLocations.Clear();

        // Clear placed trees
        foreach (GameObject tree in placedTrees)
        {
            if (tree != null) Destroy(tree);
        }
        placedTrees.Clear();

        // Clear dragons
        foreach (GameObject dragon in dragons)
        {
            if (dragon != null) Destroy(dragon);
        }
        dragons.Clear();

        // Stop the dragon behavior tree if it exists
        if (dragonBehaviorTree != null) dragonBehaviorTree.Stop();

        // Destroy the temple
        if (temple != null) Destroy(temple);

        // Destroy the player
        if (player != null)  Destroy(player);
    }


    // Place the temple, dragons and player
    void PlaceObjects()
    {
        // Place the temple at the first clearing location
        if (templePrefab != null && clearingLocations.Count > 0) temple = PlacePrefab(templePrefab, clearingLocations[0], "Temple");

        // Place dragon prefabs
        for (int i = 1; i <= 4; i++)
        {
            if (dragonPrefabs[i - 1] != null && i < clearingLocations.Count)
            {
                GameObject dragon = PlacePrefab(dragonPrefabs[i - 1], clearingLocations[i], $"Dragon{i}");
                dragon.transform.localScale = dragonScale;
                dragons.Add(dragon);
                AddDragonBehavior(dragon, clearingLocations[i]);
            }
        }

        // Place the player prefab on the last clearing
        if (playerPrefab != null && clearingLocations.Count > 1) player = PlacePrefab(playerPrefab, clearingLocations[^1], "Player");
    }
 

    // Helper function to place a prefab at a specific clearing
    GameObject PlacePrefab(GameObject prefab, Vector2Int clearingLocation, string name)
    {
        Vector3 position = new Vector3(
            -width / 2 + clearingLocation.x + 0.5f,
            0, 
            -height / 2 + clearingLocation.y + 0.5f
        );

        GameObject instance = Instantiate(prefab, position, Quaternion.Euler(90, 0, 0));
        instance.name = name;

        return instance;
    }

    // Function to connect the clearings using the Minimum Spanning Tree (MST)
    void ConnectClearings()
    {

        // Get the Minimum Spanning Tree (MST) of the clearing positions
        List<(Vector2Int, Vector2Int)> mstEdges = GetMST(clearingLocations);

        // Connect the clearings using the MST edges
        foreach ((Vector2Int start, Vector2Int end) in mstEdges)
        {
            // Parameters for path generation
            float curviness = 0.9f; // How much the path should curve (0-1)
            int pathThickness = 1; // Thickness of the path

            // Generate an S-shaped path between the two clearings
            CreateSShapedPath(start, end, curviness, pathThickness);
        }
    }

    // Create an S-shaped path between two points
    void CreateSShapedPath(Vector2Int start, Vector2Int end, float curviness, int pathThickness)
    {
        // Find the shortest path using BFS
        bool isConnected = FindShortestPath(start, end);
        if (isConnected == true) return; // If there is already a path we don't need to create a new one

        // If no path was found, create a fallback path
        List<Vector2Int> shortestPath = CreateFallbackPath(start, end);

        // Generate an S-shaped curve using cubic interpolation
        List<Vector2Int> sShapedPath = InterpolateCubicPath(shortestPath, curviness);
        
        // Draw the path with the specified thickness
        DrawPathWithThickness(sShapedPath, pathThickness);
    }

    // Find the shortest path using BFS
    bool FindShortestPath(Vector2Int start, Vector2Int end)
    {
        // Queue for BFS
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Initialize BFS
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Check if we reached the end
            if (current == end) return true; 

            // Explore neighbors
            Vector2Int[] directions = {
                new Vector2Int(0, 1),    // up
                new Vector2Int(1, 0),    // right
                new Vector2Int(0, -1),   // down
                new Vector2Int(-1, 0)    // left
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;
                if (IsInMapRange(neighbor.x, neighbor.y) && !visited.Contains(neighbor) && map[neighbor.x, neighbor.y] == 0)
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        // No path found
        return false;
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
        
        return fallbackPath;
    }

    // Calculate Manhattan distance between two points
    float ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
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

    // Function to connect all clearings using a Minimum Spanning Tree (MST)
    List<(Vector2Int, Vector2Int)> GetMST(List<Vector2Int> clearingPositions)
    {
        // List to store the edges of the MST
        List<(Vector2Int, Vector2Int)> mstEdges = new List<(Vector2Int, Vector2Int)>();

        // Priority queue to store edges with their weights
        List<(float, Vector2Int, Vector2Int)> edges = new List<(float, Vector2Int, Vector2Int)>();

        // Set to track visited nodes
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Start with the first clearing position
        if (clearingPositions.Count == 0) return mstEdges;
        Vector2Int startNode = clearingPositions[0];
        visited.Add(startNode);

        // Add all edges from the start node to the priority queue
        foreach (Vector2Int otherNode in clearingPositions)
        {
            if (otherNode != startNode)
            {
                float weight = ManhattanDistance(startNode, otherNode);
                edges.Add((weight, startNode, otherNode));
            }
        }

        // Sort edges by weight (ascending)
        edges.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        // Prim's algorithm to find the MST
        while (visited.Count < clearingPositions.Count)
        {
            // Find the smallest edge that connects a visited node to an unvisited node
            (float weight, Vector2Int nodeA, Vector2Int nodeB) = edges[0];
            edges.RemoveAt(0);

            if (visited.Contains(nodeA) && !visited.Contains(nodeB))
            {
                // Add the edge to the MST
                mstEdges.Add((nodeA, nodeB));
                visited.Add(nodeB);

                // Add new edges from the newly visited node
                foreach (Vector2Int otherNode in clearingPositions)
                {
                    if (!visited.Contains(otherNode))
                    {
                        float newWeight = ManhattanDistance(nodeB, otherNode);
                        edges.Add((newWeight, nodeB, otherNode));
                    }
                }

                // Sort edges again after adding new ones
                edges.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            }
        }

        return mstEdges;
    }

    // Initialize prefabs
    void InitializePrefabs()
    {
        // Load tree prefabs
        treePrefabs = new GameObject[]
        {
            Resources.Load<GameObject>("Prefabs/Tree1"),
            Resources.Load<GameObject>("Prefabs/Tree2"),
            Resources.Load<GameObject>("Prefabs/Tree3")
        };

        // Load the temple prefab
        templePrefab = Resources.Load<GameObject>("Prefabs/Temple");
        if (templePrefab == null)
        {
            Debug.LogError("Temple prefab not found in Resources folder!");
        }

        // Load dragon prefabs
        dragonPrefabs = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            dragonPrefabs[i] = Resources.Load<GameObject>($"Prefabs/Dragon{i + 1}");
            if (dragonPrefabs[i] == null)
            {
                Debug.LogError($"Dragon{i + 1} prefab not found in Resources folder!");
            }
        }

        // Load the player prefab
        playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not found in Resources folder!");
        }
    }
}