namespace Library.Types;

public struct GenericVector2<T>(T x, T y) : IEquatable<GenericVector2<T>>
{
    public T X = x;

    public T Y = y;

    public bool Equals(GenericVector2<T> other)
    {
        return EqualityComparer<T>.Default.Equals(other.X, X)
            && EqualityComparer<T>.Default.Equals(other.Y, Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is GenericVector2<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}

public struct GenericVector3<T>(T x, T y, T z) : IEquatable<GenericVector3<T>>
{
    public T X = x;
    public T Y = y;
    public T Z = z;

    public bool Equals(GenericVector3<T> other)
    {
        return EqualityComparer<T>.Default.Equals(other.X, X)
               && EqualityComparer<T>.Default.Equals(other.Y, Y)
               && EqualityComparer<T>.Default.Equals(other.Z, Z);
    }

    public override bool Equals(object? obj)
    {
        return obj is GenericVector3<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
}



public struct GenericVector4<T>(T x, T y, T z, T w) : IEquatable<GenericVector4<T>>
{
    public T X = x;
    public T Y = y;
    public T Z = z;
    public T W = w;

    public bool Equals(GenericVector4<T> other)
    {
        return EqualityComparer<T>.Default.Equals(other.X, X)
               && EqualityComparer<T>.Default.Equals(other.Y, Y)
               && EqualityComparer<T>.Default.Equals(other.Z, Z)
               && EqualityComparer<T>.Default.Equals(other.W, W);
    }

    public override bool Equals(object? obj)
    {
        return obj is GenericVector4<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }
}