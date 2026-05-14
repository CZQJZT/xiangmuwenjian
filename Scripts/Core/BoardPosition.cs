using System;
using UnityEngine;

namespace JunqiGame.Core
{
    /// <summary>
    /// 棋盘位置结构（对应原始代码中的坐标系统，如"a1", "b12"等）
    /// </summary>
    [Serializable]
    public struct BoardPosition : IEquatable<BoardPosition>
    {
                public char Column;  // a-e
        public int Row;      // 1-13

        // 列的范围
        public const char MinColumn = 'a';
        public const char MaxColumn = 'e';
        
        // 行的范围
        public const int MinRow = 1;
        public const int MaxRow = 13;

        public BoardPosition(char column, int row)
        {
            Column = column;
            Row = row;
        }

        /// <summary>
        /// 从字符串解析位置（如"a1", "b12"）
        /// </summary>
        public static BoardPosition FromString(string positionStr)
        {
            if (string.IsNullOrEmpty(positionStr) || positionStr.Length < 2)
                throw new ArgumentException($"Invalid position string: {positionStr}");

            char column = char.ToLower(positionStr[0]);
            int row = int.Parse(positionStr.Substring(1));

            return new BoardPosition(column, row);
        }

        /// <summary>
        /// 转换为字符串格式（如"a1"）
        /// </summary>
        public override string ToString()
        {
            return $"{char.ToLower(Column)}{Row}";
        }

        /// <summary>
        /// 验证位置是否在棋盘范围内
        /// </summary>
        public bool IsValid()
        {
            // 基本范围检查
            if (Column < MinColumn || Column > MaxColumn ||
                Row < MinRow || Row > MaxRow)
            {
                return false;
            }
            
            // 第7行特殊规则：偶数列（b7, d7）不可用
            if (Row == 7)
            {
                // 只允许奇数列：a7, c7, e7
                if (Column == 'b' || Column == 'd')
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 填充相邻位置到预分配缓冲区（零分配）
        /// </summary>
        /// <param name="buffer">长度至少为4的数组</param>
        /// <returns>实际相邻位置数量</returns>
        public int GetAdjacentPositions(BoardPosition[] buffer)
        {
            int count = 0;
            int colIndex = Column - MinColumn;

            if (colIndex > 0)
                buffer[count++] = new BoardPosition((char)(Column - 1), Row);
            if (colIndex < 4)
                buffer[count++] = new BoardPosition((char)(Column + 1), Row);
            if (Row > MinRow)
                buffer[count++] = new BoardPosition(Column, Row - 1);
            if (Row < MaxRow)
                buffer[count++] = new BoardPosition(Column, Row + 1);

            return count;
        }

        /// <summary>
        /// 获取相邻位置（旧版兼容，仍有GC分配）
        /// </summary>
        public BoardPosition[] GetAdjacentPositions()
        {
            var result = new BoardPosition[4];
            int count = GetAdjacentPositions(result);
            if (count < 4) System.Array.Resize(ref result, count);
            return result;
        }

        public bool Equals(BoardPosition other)
        {
            return Column == other.Column && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Column, Row);
        }

        public static bool operator ==(BoardPosition left, BoardPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardPosition left, BoardPosition right)
        {
            return !(left == right);
        }
    }
}
