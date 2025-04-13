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
		map = new int[width, height];

		// Stage 1: populate the grid cells
		PopulateMap();

		// Stage 2: apply cellular automata rules
		for (int i = 0; i < 5; i++)
		{
			// SmoothMap();
		}

		// Stage 3: finalise the map
		ProcessMap();
		AddMapBorder();

		// Generate mesh
		MeshGenerator meshGen = GetComponent<MeshGenerator>();
		meshGen.GenerateMesh(map, 1);
		 // Place the dragon
    PlaceDragons();
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
        RandomFillMap();
        for(int i = 0; i < 3; ++i){
            BuildTempleRegion(10, 0.2f);
            GenerateSpiralWalls(templeLocation, pseudoRandom, new Vector2Int(4, 6));
        }
		// GenerateRandomSpirals(pseudoRandom);
		// RandomFillMap();
    }
	
	void RandomFillMap()
	{
		if (useRandomSeed)
		{
			seed = Time.time.ToString();
		}

		System.Random pseudoRandom = new System.Random(seed.GetHashCode());

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
                map[x, y] = 1;
				// if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
				// {
				// 	map[x, y] = 1;
				// }
				// else
				// {
				// 	map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
				// }
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
						wallCount += map[neighbourX, neighbourY];
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
            if (map[x, y] != 0 && UnityEngine.Random.Range(0, 100) < 20) // 20% chance to place a tree
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
		// Repaint();
		// PlaceTrees();
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
void PlaceDragons()
{
    // Stop the previous behavior tree if it exists
    if (dragonBehaviorTree != null)
    {
        dragonBehaviorTree.Stop();
        dragonBehaviorTree = null;
    }

    // Destroy all previous dragons
    foreach (GameObject dragon in dragons)
    {
        if (dragon != null)
        {
            Destroy(dragon);
        }
    }
    dragons.Clear();

    // Load the dragon prefab
    GameObject dragonPrefab = Resources.Load<GameObject>("Dragoncito");
    if (dragonPrefab == null)
    {
        Debug.LogError("Dragon prefab not found in Resources folder!");
        return;
    }

    // Find all empty slots
    List<Vector2Int> emptySlots = new List<Vector2Int>();
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            if (map[x, y] == 0) // Empty slot
            {
                emptySlots.Add(new Vector2Int(x, y));
            }
        }
    }

    // Place the specified number of dragons
    for (int i = 0; i < numberOfDragons; i++)
    {
        if (emptySlots.Count > 0)
        {
            // Select a random empty slot
            Vector2Int randomSlot = emptySlots[UnityEngine.Random.Range(0, emptySlots.Count)];
            emptySlots.Remove(randomSlot); // Remove the slot to avoid duplicate placement

            Vector3 position = new Vector3(
                -width / 2 + randomSlot.x + 0.5f,
                0, // Adjust Y-axis if needed
                -height / 2 + randomSlot.y + 0.5f
            );

            // Instantiate the dragon
            // GameObject dragon = Instantiate(dragonPrefab, position, Quaternion.identity);
            GameObject dragon = Instantiate(dragonPrefab, position, Quaternion.Euler(90, 0, 0));

            // Apply the scale
            dragon.transform.localScale = dragonScale;

            // Add the dragon to the list
            dragons.Add(dragon);

            // Add the behavior tree for movement
            AddDragonBehavior(dragon, randomSlot);
        }
        else
        {
            Debug.LogWarning("Not enough empty slots to place all dragons!");
            break;
        }
    }
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
void BuildTempleRegion(int guaranteedLayers, float probabilityDecayRate)
{
    System.Random pseudoRandom = new System.Random(seed.GetHashCode());
    
    // Choose a random point not too close to the edges
    int templeX = UnityEngine.Random.Range(width / 4, width * 3 / 4);
    int templeY = UnityEngine.Random.Range(height / 4, height * 3 / 4);
    templeLocation = new Vector2Int(templeX, templeY);
    
    templeLocation = new Vector2Int(templeX, templeY);
    Debug.Log("Temple location: " + templeLocation);
    
    // BFS queue
    Queue<Vector2Int> queue = new Queue<Vector2Int>();
    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    
    queue.Enqueue(templeLocation);
    visited.Add(templeLocation);
    
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
void GenerateSpiralWalls(Vector2Int center, System.Random pseudoRandom, Vector2Int maxSpiralsInterval)
{
    // Parameters for spiral generation
    // int maxSpirals = 6;  // Number of spirals to generate
		int maxSpirals = pseudoRandom.Next(maxSpiralsInterval[0], maxSpiralsInterval[1]);  // Number of spirals to generate

    // int maxLength = Mathf.Min(width, height) / 2;  // Maximum length of each spiral
    int maxLength = 100;  // Maximum length of each spiral
		int direction = pseudoRandom.Next(0, 2) == 0 ? 1 : -1;
		float increaseAngle = 2*Mathf.PI/maxSpirals; // Starting angle for the spiral
		float startAngle = 0; // Starting angle for the spiral
		float growthFactor = 0.3f; // Growth factor for the spiral
		float startDistance = 0.01f; // Starting distance from the center
		// GenerateEulerSpiral(center, startAngle, growthFactor, startDistance, maxLength, pseudoRandom);
    
    for (int i = 0; i < maxSpirals; i++)
    {
        // Random angle to start the spiral
        // startAngle = (float)pseudoRandom.NextDouble() * 2 * Mathf.PI;
				startAngle = i * increaseAngle; // Incremental angle for each spiral
        maxLength = pseudoRandom.Next(100, 150); // Random length for each spiral
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
            
            // // Occasionally widen the walls to create chambers
            // if (pseudoRandom.NextDouble() < 0.2f)
            // {
            //     // Choose a random direction to expand
            //     int expandDirX = pseudoRandom.Next(-1, 2);
            //     int expandDirY = pseudoRandom.Next(-1, 2);
                
            //     int expandX = gridX + expandDirX;
            //     int expandY = gridY + expandDirY;
                
            //     // Ensure expanded wall is in bounds
            //     if (expandX > 0 && expandX < width - 1 && expandY > 0 && expandY < height - 1)
            //     {
            //         map[expandX, expandY] = 1;
            //     }
            // }
            
            // // Occasionally branch off a new path
            // if (pseudoRandom.NextDouble() < 0.05f && i > 10)
            // {
            //     float branchAngle = angle + (float)(pseudoRandom.NextDouble() * Mathf.PI - Mathf.PI/2);
            //     float branchGrowth = growthFactor * 0.8f + (float)pseudoRandom.NextDouble() * 0.4f;
            //     GenerateEulerSpiral(
            //         new Vector2Int(gridX, gridY), 
            //         branchAngle, 
            //         branchGrowth, 
            //         distance * 0.5f, 
            //         maxLength / 3, 
            //         pseudoRandom
            //     );
            // }
            
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
void Repaint(){
	// this function should transform all 0s in the map to 1s and the 2s to 0s
	for (int x = 0; x < width; x++)
	{
		for (int y = 0; y < height; y++)
		{
			if (map[x, y] == 2)
			{

				map[x, y] = 0;
				continue;
			}
			else if (map[x, y] == 0)
			{
				map[x, y] = 1;
			}
		}
	}
}
 
}