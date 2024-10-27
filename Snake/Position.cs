using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snake;
public class Position
{
    public Position(int row, int column)
    {
        Row = row;
        Col = column;
    }

    public int Row { get; }
    public int Col { get; }

    public override bool Equals(object obj)
    {
        return obj is Position position &&
               Row == position.Row &&
               Col == position.Col;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Row, Col);
    }

    public Position Translate(Direction dir)
    {
        return new Position(Row + dir.RowOffset, Col + dir.ColumnOffset);
    }

    public static bool operator ==(Position left, Position right)
    {
        return EqualityComparer<Position>.Default.Equals(left, right);
    }

    public static bool operator !=(Position left, Position right)
    {
        return !(left == right);
    }
}
