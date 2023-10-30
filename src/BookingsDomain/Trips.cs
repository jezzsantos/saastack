using System.Collections;

namespace BookingsDomain;

public class Trips : IReadOnlyList<TripEntity>
{
    private readonly List<TripEntity> _trips = new();

    public IEnumerator<TripEntity> GetEnumerator()
    {
        return _trips.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _trips.Count;

    public TripEntity this[int index] => _trips[index];

    public void Add(TripEntity trip)
    {
        _trips.Add(trip);
    }

    public TripEntity? Latest()
    {
        return _trips.LastOrDefault();
    }
}