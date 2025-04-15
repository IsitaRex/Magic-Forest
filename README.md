# Lónglíng (龙林) 🐉🌸
*"Lóng" (龙) means "dragon" in Chinese, and "Líng" (林) means "forest."*

Deep within the heart of *Lónglíng*, the mystic Dragon Forest, lies the fabled **Temple of Eternal Whispers**. Legends speak of an ancient sanctuary hidden among the dense trees, where the secrets of immortality and untold power are guarded by the forest's eternal protectors—dragons.  

The forest is alive, its paths twisting and turning into spirals that defy logic and reason. These spirals are no accident; they are the dragons' ancient magic, designed to disorient and confuse any who dare to enter. The spirals are said to represent the eternal cycle of life, death, and rebirth, a sacred pattern that mirrors the dragons' own mystical nature.  

As an adventurer, your quest is to uncover the entrance to the **Temple of Eternal Whispers**, but the journey will not be easy. The dragons will test your resolve, leading you deeper into the labyrinthine spirals of the forest. Each clearing you find may hold a clue—or a trap. The spirals are not just paths; they are trials, meant to separate the worthy from the unworthy.  

Will you navigate the enchanted spirals, outwit the dragons, and uncover the temple's secrets? Or will you become another lost soul, forever wandering the mystical paths of *Lónglíng*?  

<div align="center">
  <img src="./docs/LevelSamples.png" alt="Some Level Samples" width="800">
</div>

# Level Generator Overview ⚙️

The level generator for **Lónglíng** operates in three distinct stages to create a dense, mystical forest filled with spiraling paths, clearings, and hidden secrets. Below is a detailed breakdown of each stage.


---

## **Stage 1:** Initial Grid Generation

### Dense Forest Initialization 🌸
The map is initialized as a 2D grid where all cells are set to `1`, representing a dense forest. This ensures that the level starts as a completely filled forest, and clearings and paths are carved out during subsequent steps.

---
### Generating the Temple and Dragon Clearings ⛩️ 🐲
The level will feature a Temple clearing, the heart of the forest, several Dragon clearings and a clearing where the player should start. Dragons, being fiercely territorial, guard the temple with unwavering vigilance. Their ancient magic test the resolve of intruders, ensuring that only the most determined and worthy adventurers can uncover the secrets of the Temple of Eternal Whispers.

A **clearing** is generated using **Breadth-First Search (BFS)** to create a space for the **Temple of Eternal Whispers**. To prevent collisions between the clearings, the map was divided into a 5x5 grid. This ensures that when a clearing is placed, no other clearings are located in the 8 neighboring cells or in the same cell. This approach maintains proper spacing between clearings, creating a balanced and visually appealing layout. You can see an illustration of this concept in the figure below:


<div align="center">
  <img src="./docs/Collisions.png" alt="Collision Avoidance in a 5x5 Grid" width="600">
</div>

---
### BFS Algorithm 
<div align="center">
  <img src="./docs/BFS.png" alt="Collision Avoidance in a 5x5 Grid" width="600">
</div>


  - Start from a randomly chosen point `clearingLocation` within the central region of the map (to avoid edges).
  - Use a queue to explore neighboring cells in all four cardinal directions (up, down, left, right).
  - Mark cells as empty (`0`) based on a **probability decay**:
    - Initially, the probability of clearing a cell is `100%`.
    - After a certain number of guaranteed layers, the probability decreases exponentially using a **decay rate** (e.g., `currentProbability *= probabilityDecayRate`).
  - This creates a natural-looking clearing that is larger in the center and tapers off toward the edges.
  - Stop when reaching a maximum number of layers `maxLayers`
  - These clearings are later **smoothed** in Stage 2 to make their shapes more organic and irregular.


**Temple Clearing Parameters ⛩️**

The number of guaranteed layers and maximum layers for the temple is increased to create a larger clearing, ensuring that the temple prefab has sufficient space and fits seamlessly within the environment and also stands out.
```
guaranteedLayers = 13
probabilityDecayRate = 0.9
maxLayers = 15
```
**Dragon Clearing Parameters 🐲**
```
guaranteedLayers = 8
probabilityDecayRate = 0.7
maxLayers = 15
```

**Player Clearing Parameters**
```
guaranteedLayers = 4
probabilityDecayRate = 0.7
maxLayers = 10
```

## Stage 2: Cellular Automata for Smoothing

The cellular automata algorithm is applied to smooth the clearings and make their shapes more irregular and natural-looking. This enhances the variety and realism of the level.

### Algorithm
- For each cell in the grid:
  - Count the number of neighboring cells that are walls (`1`).
  - Apply the following rules:
    - If a cell has more than 4 wall neighbors, it becomes a wall (`1`).
    - If a cell has fewer than 4 wall neighbors, it becomes empty (`0`).
- This process is repeated for a fixed number of iterations (e.g., 5) to achieve the desired smoothing effect.


---

## Stage 3: Connecting Spirals and Clearings

### Populating Map with Spirals 🌀
**Spirals** are generated using the **Euler Spiral formula**, starting from the temple clearing and dragon clearings.

The **Euler Spiral**, also known as the **Clothoid**, is a curve whose curvature increases linearly with its arc length. This property makes it ideal for generating smooth, natural-looking spirals. 

**Implementation**:
  - The curvature of the spiral increases linearly with distance, creating a smooth, natural curve.
  - The angle of the spiral is updated iteratively:
    ```csharp
    angle += direction * growthFactor * distance / 10f;
    ```
    - `direction`: Determines whether the spiral curves clockwise (`1`) or counterclockwise (`-1`).
    - `growthFactor`: Controls how quickly the curvature increases.
    - `distance`: Represents the distance from the starting point, incremented in each iteration.
  - The next position in the spiral is calculated as:
    ```csharp
    nextPos = currentPos + new Vector2(
        Mathf.Cos(angle) * stepSize,
        Mathf.Sin(angle) * stepSize
    );
    ```
    - `stepSize`: Determines the spacing between points in the spiral.
- **Parameters**:
  - **Starting Distance**: The initial distance from the center of the clearing.
  - **Growth Factor**: Controls the rate of curvature increase (e.g., `0.3f`).
  - **Step Size**: Controls the spacing between points (e.g., `0.5f`).
  - **Direction**: Randomly chosen for each spiral to create variety (clockwise or counterclockwise).
---

### Connect Clearings 🌉
To ensure the game is playable, it is crucial to establish connectivity between the temple, the dragons, and the player. The level generator achieves this in two steps: first, computing the **Minimum Spanning Tree (MST)** between the clearings, and then building paths with an **S-shape** to create smooth and natural connections.

#### Building the MST
The MST ensures that all clearings are accessible while preserving the mystical and maze-like essence of the forest. By minimizing the total connection distance, the MST avoids unnecessary or overly long paths, creating a more cohesive and efficient layout. 

##### Algorithm:

1. Treat each clearing as a node in a graph.
2. Compute the Manhattan distance between all pairs of clearings to determine edge weights.
3. Use Prim's Algorithm to construct the MST:
    * Start with the temple clearing as the initial node.
    * Iteratively connect the closest unvisited clearing to the visited nodes based on the edge weights.
    * Repeat until all clearings are connected.



#### S-Shaped Path Generation
Once the MST is generated, it determines which clearings need to be connected. The second stage focuses on creating these connections in a smooth and natural way. First, we check if a path already exists between the clearings using a Breadth-First Search (BFS). If no path is found, a fallback path is generated.

The fallback path is created by drawing a straight line between the two points and introducing a midpoint with added variance to create a slight curve. With the start, midpoint, and end points defined, the path is then interpolated using cubic Bézier curves to produce smooth, natural-looking connections.

The curviness parameter (set to `0.9f`) determines how pronounced the curves are, allowing for fine-tuning of the path's shape. Additionally, paths are drawn with a variable thickness (`pathThickness = 1`) to create natural-looking trails that blend seamlessly into the forest environment.

### Place Trees 🌲

Trees are placed on all wall cells `map[x, y] != 0` to create a dense, mystical atmosphere while ensuring paths remain navigable.

Three different tree prefabs are used for visual variety (`Tree1`, `Tree2`, `Tree3`).Each tree is randomly scaled between `0.05f` and `0.2f` to create natural variation.

### Place Other Objects 🏯

The final step places the temple, dragons, and player in their respective clearings, bringing the mystical forest to life. Each object is properly scaled and positioned to maintain the visual harmony of the scene. Finally, when the map is regenerated (by clicking), all objects are cleared and placed anew.

#### Object Placement
- **Temple Placement**: The Temple of Eternal Whispers is placed at the first clearing location:
  ```csharp
  temple = PlacePrefab(templePrefab, clearingLocations[0], "Temple");
  ```

- **Dragon Placement**: Four unique dragons guard their territories throughout the forest:
  ```csharp
  for (int i = 1; i <= 4; i++)
  {
      GameObject dragon = PlacePrefab(dragonPrefabs[i - 1], clearingLocations[i], $"Dragon{i}");
      dragon.transform.localScale = dragonScale;
      dragons.Add(dragon);
      AddDragonBehavior(dragon, clearingLocations[i]);
  }
  ```
  
- **Dragon Behavior**: Each dragon is assigned a behavior tree using [NPBehave](https://github.com/meniku/NPBehave):
  - Dragons move around their clearing at a defined speed (`dragonMoveSpeed = 0.5f`).
  - Movement is restricted to adjacent empty cells to prevent dragons from leaving their territory.
  - The behavior tree operates in a loop: Wait → Move → Repeat.

- **Player Placement**: The player's starting position is set at the last clearing:
  ```csharp
  player = PlacePrefab(playerPrefab, clearingLocations[^1], "Player");
  ```


# References and Acknowledgments 📚

This project draws inspiration and utilizes resources from several sources, which have been instrumental in its development:

#### **Sebastian Lague's Cellular Automata Tutorial**

This project is heavily inspired by Sebastian Lague’s popular tutorial on Cellular Automata. His work provided the foundation for the scripts used in this level generator,  particularly the use of **Cellular Automata** for clearing smoothing. You can watch the tutorial series here: [**Sebastian Lague - Cellular Automata Tutorial**](https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9).


#### **NPBehave**
The dragon behavior system is implemented using [**NPBehave**](https://github.com/meniku/NPBehave), a lightweight and flexible behavior tree library for Unity. NPBehave enables the dragons to exhibit dynamic and reactive behaviors, such as patrolling their clearings and responding to player actions.

#### **Unity Documentation**
The project relies heavily on Unity's built-in tools and APIs for procedural generation, object placement, and randomization. Key Unity features used include:
- **UnityEngine.Random** for random number generation.
- **Mathf** for mathematical operations like trigonometry and interpolation.
- **Unity's Prefab System** for efficient object instantiation.

#### **Other Resources**
- **Bézier Curves**: The S-shaped paths between clearings are created using cubic Bézier interpolation. For more information on Bézier curves, see: [**Bézier Curve - Wikipedia**](https://en.wikipedia.org/wiki/B%C3%A9zier_curve).
- **Prim's Algorithm**: The Minimum Spanning Tree (MST) implementation is based on Prim's Algorithm, which ensures efficient and cohesive connectivity between clearings. Learn more: [**Prim's Algorithm - GeeksforGeeks**](https://www.geeksforgeeks.org/prims-minimum-spanning-tree-mst-greedy-algo-5/).
- **Euler Spiral (Clothoid)**: The spiraling paths in the forest are based on the mathematical concept of the **Euler Spiral**, also known as the **Clothoid**.  
Learn more about Euler Spirals: [**Euler Spiral - Wikipedia**](https://en.wikipedia.org/wiki/Euler_spiral).


# Use of AI 🤖

This project was developed with the assistance of **GitHub Copilot (GPT-4o)**, an AI-powered programming tool. While all the ideas, designs, and logic behind the implementation were my own, I used Copilot to generate baseline code for various parts of the project. These generated suggestions were then reviewed, edited, and refined by me to align with the project's requirements and goals.

Additionally, GitHub Copilot was used to improve the writing and structure of the **README.md** file, ensuring clarity and quality in the documentation.
