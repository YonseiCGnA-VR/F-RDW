using UnityEngine;
using System.Collections;

//<summary>
//Game object, that creates maze and instantiates it in scene
//</summary>
public class MazeSpawner : MonoBehaviour {
	public enum MazeGenerationAlgorithm{
		PureRecursive,
		RecursiveTree,
		RandomTree,
		OldestTree,
		RecursiveDivision,
		RecursiveTreeWithOpenSpace,
	}

	public MazeGenerationAlgorithm Algorithm = MazeGenerationAlgorithm.PureRecursive;
	public bool FullRandom = false;
	public bool TimeSeed = false;
	public int RandomSeed = 12345;
	public GameObject Floor = null;
	public GameObject Wall = null;
	public GameObject Pillar = null;
	public int Rows = 5;
	public int Columns = 5;
	public float CellWidth = 5;
	public float CellHeight = 5;
	public bool AddGaps = true;
	public int NumberofCoins = 3;
	public GameObject GoalPrefab = null;
	public bool addGraph = false;
	public int openSpaceSize = 2;
	private int coinCount = 0;
	private int randomSeed = 0;

	private BasicMazeGenerator mMazeGenerator = null;

	void Awake () {
		randomSeed = RandomSeed;
		generateMaze();
	}

	public void setRandomSeed(int seed)
    {
		RandomSeed = seed;
    }

	public void initRandomSeed()
    {
		RandomSeed = randomSeed;
    }

	public void increaseRandomSeed()
    {
		RandomSeed = RandomSeed + 1;

    }

	public void DestroyMaze()
    {
        var temp = this.GetComponentsInChildren<Transform>();

        for (int i = 1; i < temp.Length; i++)
        {
            DestroyImmediate(temp[i].gameObject);
        }
        //return;

        //for (var i = this.transform.childCount - 1; i > 0; i--)
        //{
        //    Object.Destroy(this.transform.GetChild(i).gameObject);
        //}

        return;
    }


	public void generateMaze()
    {
		Debug.Log("RandomSeed = " + RandomSeed);
		coinCount = 0;
		if (!FullRandom)
		{
			Random.seed = RandomSeed;
		}
		if (TimeSeed)
		{
			Random.seed = System.DateTime.Now.Minute * System.DateTime.Now.Second;
		}
		switch (Algorithm)
		{
			case MazeGenerationAlgorithm.PureRecursive:
				mMazeGenerator = new RecursiveMazeGenerator(Rows, Columns);
				break;
			case MazeGenerationAlgorithm.RecursiveTree:
				mMazeGenerator = new RecursiveTreeMazeGenerator(Rows, Columns);
				break;
			case MazeGenerationAlgorithm.RandomTree:
				mMazeGenerator = new RandomTreeMazeGenerator(Rows, Columns);
				break;
			case MazeGenerationAlgorithm.OldestTree:
				mMazeGenerator = new OldestTreeMazeGenerator(Rows, Columns);
				break;
			case MazeGenerationAlgorithm.RecursiveDivision:
				mMazeGenerator = new DivisionMazeGenerator(Rows, Columns);
				break;
			case MazeGenerationAlgorithm.RecursiveTreeWithOpenSpace:
				mMazeGenerator = new RecursiveTreeMazeV2(Rows, Columns, openSpaceSize);
				break;

		}
		mMazeGenerator.GenerateMaze();
		for (int row = 0; row < Rows; row++)
		{
			for (int column = 0; column < Columns; column++)
			{
				float x = column * (CellWidth + (AddGaps ? .2f : 0));
				float z = row * (CellHeight + (AddGaps ? .2f : 0));
				MazeCell cell = mMazeGenerator.GetMazeCell(row, column);
				GameObject tmp;
				tmp = Instantiate(Floor, new Vector3(x, 0, z), Quaternion.Euler(0, 0, 0)) as GameObject;
				if (addGraph)
				{
					addGraphComponent(tmp, cell);
				}

				tmp.transform.parent = transform;
				tmp.name = "Floor_" + (column + row * Columns).ToString();

				if (cell.WallRight)
				{
					tmp = Instantiate(Wall, new Vector3(x + CellWidth / 2, 0, z) + Wall.transform.position, Quaternion.Euler(0, 90, 0)) as GameObject;// right
					tmp.tag = "VirtualWall";
					tmp.layer = 12;
					tmp.transform.parent = transform;
				}
				if (cell.WallFront)
				{
					tmp = Instantiate(Wall, new Vector3(x, 0, z + CellHeight / 2) + Wall.transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;// front
					tmp.layer = 12;
					tmp.tag = "VirtualWall";
					tmp.transform.parent = transform;
				}
				if (cell.WallLeft)
				{
					tmp = Instantiate(Wall, new Vector3(x - CellWidth / 2, 0, z) + Wall.transform.position, Quaternion.Euler(0, 270, 0)) as GameObject;// left
					tmp.layer = 12;
					tmp.tag = "VirtualWall";
					tmp.transform.parent = transform;
				}
				if (cell.WallBack)
				{
					tmp = Instantiate(Wall, new Vector3(x, 0, z - CellHeight / 2) + Wall.transform.position, Quaternion.Euler(0, 180, 0)) as GameObject;// back
					tmp.layer = 12;
					tmp.tag = "VirtualWall";
					tmp.transform.parent = transform;
				}
				if (cell.IsGoal && GoalPrefab != null)
				{
					if (coinCount < NumberofCoins)
					{
						coinCount = coinCount + 1;
						tmp = Instantiate(GoalPrefab, new Vector3(x, 1, z), Quaternion.Euler(0, 0, 0)) as GameObject;
						tmp.transform.parent = transform;
					}

				}
			}
		}
		if (Pillar != null)
		{
			for (int row = 0; row < Rows + 1; row++)
			{
				for (int column = 0; column < Columns + 1; column++)
				{
					float x = column * (CellWidth + (AddGaps ? .2f : 0));
					float z = row * (CellHeight + (AddGaps ? .2f : 0));
					GameObject tmp = Instantiate(Pillar, new Vector3(x - CellWidth / 2, 0, z - CellHeight / 2), Quaternion.identity) as GameObject;
					tmp.layer = 12;
					tmp.transform.parent = transform;
					tmp.tag = "VirtualWall";
					
				}
			}
		}
	}

	public void addGraphComponent(GameObject tmp, MazeCell cell)
    {
		tmp.tag = "Graph";
		if (cell.WallRight && cell.WallLeft)
        {
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.Straight_Z;
        }else if(cell.WallFront && cell.WallBack)
        {
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.Straight_X;
        }
		else if(cell.WallRight && cell.WallFront)
        {
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.quadrant_3_turn;
        }
		else if(cell.WallLeft && cell.WallFront)
        {
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.quadrant_4_turn;
        }
		else if(cell.WallRight && cell.WallBack)
        {
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.quadrant_2_turn;
        }
		else if(cell.WallLeft && cell.WallBack)
        {
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.quadrant_1_turn;
        }else if(cell.WallLeft && !cell.WallRight && !cell.WallFront && !cell.WallBack)
        {
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.right_T_maze;
        }
		else if (!cell.WallLeft && cell.WallRight && !cell.WallFront && !cell.WallBack)
		{
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.left_T_maze;
		}
		else if (!cell.WallLeft && !cell.WallRight && cell.WallFront && !cell.WallBack)
		{
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.down_T_maze;
		}
		else if (!cell.WallLeft && !cell.WallRight && !cell.WallFront && cell.WallBack)
		{
			tmp.AddComponent<GraphScript>().type = graph.GraphObject.Type.up_T_maze;
		}
        else
        {
			Debug.LogError("Something is wrong when add GraphScript component to floor ...");
        }
	}
}
