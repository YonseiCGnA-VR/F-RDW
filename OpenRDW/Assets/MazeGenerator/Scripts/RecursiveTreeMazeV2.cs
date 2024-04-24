using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecursiveTreeMazeV2 : BasicMazeGenerator
{
	private int openSpaceSize = 0;
	//<summary>
	//Class representation of target cell
	//</summary>
	private struct CellToVisit
	{
		public int Row;
		public int Column;
		public Direction MoveMade;

		public CellToVisit(int row, int column, Direction move)
		{
			Row = row;
			Column = column;
			MoveMade = move;
		}

		public override string ToString()
		{
			return string.Format("[MazeCell {0} {1}]", Row, Column);
		}
	}

	System.Collections.Generic.List<CellToVisit> mCellsToVisit = new System.Collections.Generic.List<CellToVisit>();

	public RecursiveTreeMazeV2(int row, int column, int size) : base(row, column)
	{
		this.openSpaceSize = size;
	}

	public override void GenerateMaze()
	{
		Direction[] movesAvailable = new Direction[4];
		int movesAvailableCount = 0;
		mCellsToVisit.Add(new CellToVisit(Random.Range(0, RowCount), Random.Range(0, ColumnCount), Direction.Start));
		int openSpaceRow = 0;
		int openSpaceColumn = 0;
		if(!initOpenSpace(ref openSpaceRow, ref openSpaceColumn, openSpaceSize))
        {
			Debug.LogError("Error in initOpenSpace ...");
        }
		Debug.Log("openSpace Row : " + openSpaceRow + " Column : " + openSpaceColumn);
		while (mCellsToVisit.Count > 0)
		{
			movesAvailableCount = 0;
			CellToVisit ctv = mCellsToVisit[GetCellInRange(mCellsToVisit.Count - 1)];
			// initiate current cell's configuration
			//check move right
			if (ctv.Column + 1 < ColumnCount && !GetMazeCell(ctv.Row, ctv.Column + 1).IsVisited && !IsCellInList(ctv.Row, ctv.Column + 1))
			{
				movesAvailable[movesAvailableCount] = Direction.Right;
				movesAvailableCount++;
			}
			else if (!GetMazeCell(ctv.Row, ctv.Column).IsVisited && ctv.MoveMade != Direction.Left)
			{
				GetMazeCell(ctv.Row, ctv.Column).WallRight = true;
				if (ctv.Column + 1 < ColumnCount)
				{
					GetMazeCell(ctv.Row, ctv.Column + 1).WallLeft = true;
				}
			}
			//check move forward
			if (ctv.Row + 1 < RowCount && !GetMazeCell(ctv.Row + 1, ctv.Column).IsVisited && !IsCellInList(ctv.Row + 1, ctv.Column))
			{
				movesAvailable[movesAvailableCount] = Direction.Front;
				movesAvailableCount++;
			}
			else if (!GetMazeCell(ctv.Row, ctv.Column).IsVisited && ctv.MoveMade != Direction.Back)
			{
				GetMazeCell(ctv.Row, ctv.Column).WallFront = true;
				if (ctv.Row + 1 < RowCount)
				{
					GetMazeCell(ctv.Row + 1, ctv.Column).WallBack = true;
				}
			}
			//check move left
			if (ctv.Column > 0 && ctv.Column - 1 >= 0 && !GetMazeCell(ctv.Row, ctv.Column - 1).IsVisited && !IsCellInList(ctv.Row, ctv.Column - 1))
			{
				movesAvailable[movesAvailableCount] = Direction.Left;
				movesAvailableCount++;
			}
			else if (!GetMazeCell(ctv.Row, ctv.Column).IsVisited && ctv.MoveMade != Direction.Right)
			{
				GetMazeCell(ctv.Row, ctv.Column).WallLeft = true;
				if (ctv.Column > 0 && ctv.Column - 1 >= 0)
				{
					GetMazeCell(ctv.Row, ctv.Column - 1).WallRight = true;
				}
			}
			//check move backward
			if (ctv.Row > 0 && ctv.Row - 1 >= 0 && !GetMazeCell(ctv.Row - 1, ctv.Column).IsVisited && !IsCellInList(ctv.Row - 1, ctv.Column))
			{
				movesAvailable[movesAvailableCount] = Direction.Back;
				movesAvailableCount++;
			}
			else if (!GetMazeCell(ctv.Row, ctv.Column).IsVisited && ctv.MoveMade != Direction.Front)
			{
				GetMazeCell(ctv.Row, ctv.Column).WallBack = true;
				if (ctv.Row > 0 && ctv.Row - 1 >= 0)
				{
					GetMazeCell(ctv.Row - 1, ctv.Column).WallFront = true;
				}
			}

			if (!GetMazeCell(ctv.Row, ctv.Column).IsVisited && movesAvailableCount == 0)
			{
				GetMazeCell(ctv.Row, ctv.Column).IsGoal = true;
			}


			GetMazeCell(ctv.Row, ctv.Column).IsVisited = true;

			// push next cell to stack
			if (movesAvailableCount > 0)
			{
				switch (movesAvailable[Random.Range(0, movesAvailableCount)])
				{
					case Direction.Start:
						break;
					case Direction.Right:
						mCellsToVisit.Add(new CellToVisit(ctv.Row, ctv.Column + 1, Direction.Right));
						break;
					case Direction.Front:
						mCellsToVisit.Add(new CellToVisit(ctv.Row + 1, ctv.Column, Direction.Front));
						break;
					case Direction.Left:
						mCellsToVisit.Add(new CellToVisit(ctv.Row, ctv.Column - 1, Direction.Left));
						break;
					case Direction.Back:
						mCellsToVisit.Add(new CellToVisit(ctv.Row - 1, ctv.Column, Direction.Back));
						break;
				}
			}
			else
			{
				mCellsToVisit.Remove(ctv);
			}
		}

		// create Open Space
		for(int i = 0; i < RowCount; i++)
        {
			for(int j = 0; j < ColumnCount; j++)
            {
				//MazeCell cell = GetMazeCell(i, j);
				if (isCellOpenSpace(i, j, openSpaceRow, openSpaceColumn, openSpaceSize))
				{
					GetMazeCell(i, j).WallRight = false;
					GetMazeCell(i, j).WallLeft = false;
					GetMazeCell(i, j).WallFront = false;
					GetMazeCell(i, j).WallBack = false;
					if(j > 0) GetMazeCell(i, j - 1).WallRight = false;
					if(j < ColumnCount - 1) GetMazeCell(i, j + 1).WallLeft = false;
					if(i > 0) GetMazeCell(i - 1, j).WallFront = false;
					if(i < RowCount - 1) GetMazeCell(i + 1, j).WallBack = false;


				}
			}
        }
	}

	private bool IsCellInList(int row, int column)
	{
		return mCellsToVisit.FindIndex((other) => other.Row == row && other.Column == column) >= 0;
	}

	private bool initOpenSpace(ref int minRow, ref int minColomn, int size)
    {
		minRow = Random.Range(1, RowCount - size );
		minColomn = Random.Range(1, ColumnCount - size );

		return true;
    }

	private bool isCellOpenSpace(int row, int column, int spaceMinRow, int spaceMinCol, int size)
    {
		bool result = false;
		if( spaceMinRow <= row && row < spaceMinRow + size)
        {
			if(spaceMinCol <= column && column < spaceMinCol + size)
            {
				result = true;
            }
        }
		return result;
    }


	//<summary>
	//Abstract method for cell selection strategy
	//</summary>
	protected virtual int GetCellInRange(int max)
    {
		return max;
    }
}
