using NPBehave;
using UnityEngine;
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
		for (int i = 0; i < 3; i++)
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
    PlaceDragons();
	}

	/*
	 * STAGE 1: Populate the map
	 */
	void PopulateMap()
    {
		RandomFillMap();
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
				if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
				{
					map[x, y] = 1;
				}
				else
				{
					map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
				}
			}
		}
		 BuildTempleRegion(20, 0.7f);
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
		PlaceTrees();
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
    int templeX = pseudoRandom.Next(width / 4, width * 3 / 4);
    int templeY = pseudoRandom.Next(height / 4, height * 3 / 4);
		templeLocation = new Vector2Int(templeX, templeY);
    
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
            map[cell.x, cell.y] = 1;
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
}