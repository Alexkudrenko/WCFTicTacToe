using System;
namespace TicTacToe
{
    class GameLogic
    {
        byte[,] _field = new byte[3, 3];
        public byte CheckWin(byte playerChar)
        {
            if (playerChar == _field[0, 0] && playerChar == _field[1, 1] && playerChar == _field[2, 2] ||
                playerChar == _field[2, 0] && playerChar == _field[1, 1] && playerChar == _field[0, 2])
                return 1;
            for (int i = 0; i < 3; ++i)
                if (playerChar == _field[i, 0] && playerChar == _field[i, 1] && playerChar == _field[i, 2] ||
                    playerChar == _field[0, i] && playerChar == _field[1, i] && playerChar == _field[2, i])
                    return 1;
            #region проверка на пустые клетки поля
            bool flag = true; // на поле нет пустых клеток
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    if (_field[i, j] == 0)
                        return 0;
                }
            }
            if (flag)
                return 2;
            #endregion
            return 0;
        } // 1 - win, 0 - continue, 2 - draw
        public void ResetField()
        {
            Array.Clear(_field, 0, 9);
        }
        public void NewMove(string moveData, byte playerChar)
        {
            _field[short.Parse(moveData.Substring(0, 1)), short.Parse(moveData.Substring(1, 1))] = playerChar;
        }
    }
}