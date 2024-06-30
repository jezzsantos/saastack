using System.Collections;

namespace BookingsDomain;

public class Trips : IReadOnlyList<Trip>
{
    private readonly List<Trip> _trips = new();

    public int Count => _trips.Count;

    public IEnumerator<Trip> GetEnumerator()
    {
        return _trips.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Trip this[int index] => _trips[index];

    public void Add(Trip trip)
    {
        _trips.Add(trip);
    }

    public Trip? Latest()
    {
        return _trips.LastOrDefault();
    }
}