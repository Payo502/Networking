using System;

namespace shared
{
	/**
	 * Super simple board model for TicTacToe that contains the minimal data to actually represent the board. 
	 * It doesn't say anything about whose turn it is, whether the game is finished etc.
	 * IF you want to actually implement a REAL Tic Tac Toe, that means you will have to add the data required for that (and serialize it!).
	 */
	public class TicTacToeBoardData : ASerializable
	{
		//board representation in 1d array, one element for each cell
		//0 is empty, 1 is player 1, 2 is player 2
		//might be that for your game, a 2d array is actually better
		public int[] board = new int[9];

		/**
		 * Returns who has won.
		 * 
		 * If there are any 0 on the board, noone has won yet (return 0).
		 * If there are only 1's on the board, player 1 has won (return 1).
		 * If there are only 2's on the board, player 2 has won (return 2).
		 */
		public int WhoHasWon()
		{
			//this is just an example of a possible win condition, 
			//but not the 'real' tictactoe win condition.
			for (int row = 0; row < 3; row++)
			{
				if (board[row * 3] != 0 && 
				    board[row * 3] == board[row * 3 + 1] && 
				    board[row * 3] == board[row * 3 + 2])
				{
					return board[row * 3];
				}
			}
			for (int col = 0; col < 3; col++)
			{
				if (board[col] != 0 && 
				    board[col] == board[col + 3] && 
				    board[col] == board[col + 6])
				{
					return board[col];
				}
			}
			if (board[0] != 0 && board[0] == board[4] && board[0] == board[8])
			{
				return board[0];
			}
			if (board[2] != 0 && board[2] == board[4] && board[2] == board[6])
			{
				return board[2];
			}

			if (!Array.Exists(board, element => element == 0))
			{
				return -1;
			}

			return 0;
			/*int total = 1;
			foreach (int cell in board) total *= cell;

			if (total == 1)		return 1;       //1*1*1*1*1*1*1*1*1
			if (total == 512)	return 2;		//2*2*2*2*2*2*2*2*2
			return 0;		*/					//noone has one yet
		}
		
		public override void Serialize(Packet pPacket)
		{
			for (int i = 0; i < board.Length; i++) pPacket.Write(board[i]);
		}

		public override void Deserialize(Packet pPacket)
		{
			for (int i = 0; i < board.Length; i++) board[i] = pPacket.ReadInt();
		}

		public override string ToString()
		{
			return GetType().Name +":"+ string.Join(",", board);
		}
	}
}

