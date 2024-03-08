using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClimbPoint : MonoBehaviour
{
    [SerializeField] bool mountPoint;
    [SerializeField] List<Neighbour> neighbours;

    private void Awake()
    {
        List<Neighbour> twoWaysNeighbours = neighbours.Where(n => n.isTwoWay).ToList();

        foreach (Neighbour neighbour in neighbours.Where(n => n.isTwoWay))
        {
            neighbour.point?.CreateConnection(this, -neighbour.direction, neighbour.connectionType, neighbour.isTwoWay);
        }
    }

    public void CreateConnection(ClimbPoint point, Vector2 direction, ConnectionType connectionType, bool isTwoWay = true)
    {
        Neighbour neighbour = new Neighbour()
        {
            point = point,
            direction = direction,
            connectionType = connectionType,
            isTwoWay = isTwoWay
        };

        neighbours.Add(neighbour);
    }

    public Neighbour GetNeighbour(Vector2 direction)
    {
        Neighbour neighbour = null;

        if(direction.y != 0)
            neighbour = neighbours.FirstOrDefault(n => n.direction.y == direction.y);

        if(neighbour == null && direction.x != 0)
            neighbour = neighbours.FirstOrDefault(n => n.direction.x == direction.x);

        return neighbour;
    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(transform.position, transform.forward, Color.magenta);

        foreach(Neighbour neighbour in neighbours)
        {
            if (neighbour.point != null)
                Debug.DrawLine(transform.position, neighbour.point.transform.position, neighbour.isTwoWay ? Color.blue : Color.gray);
        }
    }

    public bool MountPoint => mountPoint;
}

[System.Serializable]
public class Neighbour
{
    public ClimbPoint point;
    public Vector2 direction;
    public ConnectionType connectionType;
    public bool isTwoWay = true;
}

public enum ConnectionType
{
    JUMP,
    MOVE
}
