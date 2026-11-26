using System.Collections.Generic;
using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Lightweight ambient traffic built from the supplied Complete Vehicle Pack
    /// prefabs. Cars follow a closed waypoint loop extracted from the city bounds,
    /// stay on their lane and slow down when another vehicle is ahead. Bodies are
    /// kinematic so the player can collide with them without destabilising physics.
    /// </summary>
    public sealed class TrafficSystem : MonoBehaviour
    {
        private sealed class Agent
        {
            public Transform Transform;
            public Rigidbody Body;
            public int Waypoint;
            public float Speed;
            public float Lane;
        }

        private readonly List<Agent> _agents = new List<Agent>();
        private Vector3[] _waypoints;
        private GameObject[] _prefabs;
        private bool _running;

        public int ActiveCars => _agents.Count;

        public void Build(CityBuilder city, int carCount = 14)
        {
            _prefabs = Resources.LoadAll<GameObject>("TrafficCars");
            if (_prefabs == null || _prefabs.Length == 0)
            {
                Debug.LogWarning("[CityDrive] No traffic prefabs found in Resources/TrafficCars.");
                return;
            }

            _waypoints = BuildLoop(city != null ? city.Bounds : new Bounds(Vector3.zero, new Vector3(500f, 20f, 500f)));
            if (_waypoints == null || _waypoints.Length < 4) return;

            for (int i = 0; i < carCount; i++)
            {
                GameObject prefab = _prefabs[i % _prefabs.Length];
                int index = Mathf.RoundToInt((float)i / carCount * _waypoints.Length) % _waypoints.Length;
                Vector3 pos = _waypoints[index] + LaneOffset(index, i % 2 == 0 ? 2.6f : -2.6f);
                GameObject car = Instantiate(prefab, pos, Quaternion.LookRotation(NextDirection(index)));
                car.name = "Traffic " + prefab.name + " " + (i + 1);

                Collider[] existing = car.GetComponentsInChildren<Collider>(true);
                if (existing.Length == 0)
                {
                    BoxCollider box = car.AddComponent<BoxCollider>();
                    box.size = new Vector3(1.9f, 1.4f, 4.2f);
                }

                Rigidbody body = car.GetComponent<Rigidbody>();
                if (body == null) body = car.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;

                _agents.Add(new Agent
                {
                    Transform = car.transform,
                    Body = body,
                    Waypoint = index,
                    Speed = 8f + (i % 5) * 1.4f,
                    Lane = i % 2 == 0 ? 2.6f : -2.6f
                });
            }

            _running = true;
            Debug.Log($"[CityDrive] Traffic active: {_agents.Count} vehicles, {_waypoints.Length} waypoints.");
        }

        private static Vector3[] BuildLoop(Bounds bounds)
        {
            float margin = 18f;
            float minX = bounds.min.x + margin;
            float maxX = bounds.max.x - margin;
            float minZ = bounds.min.z + margin;
            float maxZ = bounds.max.z - margin;
            if (maxX - minX < 80f || maxZ - minZ < 80f) return null;

            Vector2[] flat =
            {
                new Vector2(minX, minZ), new Vector2(Mathf.Lerp(minX, maxX, 0.33f), minZ),
                new Vector2(Mathf.Lerp(minX, maxX, 0.66f), minZ), new Vector2(maxX, minZ),
                new Vector2(maxX, Mathf.Lerp(minZ, maxZ, 0.33f)), new Vector2(maxX, Mathf.Lerp(minZ, maxZ, 0.66f)),
                new Vector2(maxX, maxZ), new Vector2(Mathf.Lerp(minX, maxX, 0.66f), maxZ),
                new Vector2(Mathf.Lerp(minX, maxX, 0.33f), maxZ), new Vector2(minX, maxZ),
                new Vector2(minX, Mathf.Lerp(minZ, maxZ, 0.66f)), new Vector2(minX, Mathf.Lerp(minZ, maxZ, 0.33f))
            };

            Vector3[] points = new Vector3[flat.Length];
            for (int i = 0; i < flat.Length; i++)
            {
                Vector3 origin = new Vector3(flat[i].x, bounds.max.y + 120f, flat[i].y);
                float y = bounds.min.y + 0.35f;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 400f, ~0, QueryTriggerInteraction.Ignore))
                    y = hit.point.y + 0.35f;
                points[i] = new Vector3(flat[i].x, y, flat[i].y);
            }
            return points;
        }

        private Vector3 LaneOffset(int waypoint, float lane)
        {
            Vector3 dir = NextDirection(waypoint);
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            return side * lane;
        }

        private Vector3 NextDirection(int waypoint)
        {
            Vector3 from = _waypoints[waypoint];
            Vector3 to = _waypoints[(waypoint + 1) % _waypoints.Length];
            return (to - from).normalized;
        }

        private void FixedUpdate()
        {
            if (!_running || _waypoints == null) return;

            for (int i = 0; i < _agents.Count; i++)
            {
                Agent agent = _agents[i];
                Vector3 target = _waypoints[(agent.Waypoint + 1) % _waypoints.Length] +
                                 LaneOffset((agent.Waypoint + 1) % _waypoints.Length, agent.Lane);
                Vector3 direction = (target - agent.Transform.position).normalized;
                float distance = Vector3.Distance(agent.Transform.position, target);

                float desired = agent.Speed;
                for (int j = 0; j < _agents.Count; j++)
                {
                    if (i == j || Mathf.Abs(_agents[j].Lane - agent.Lane) > 1.2f) continue;
                    Vector3 delta = _agents[j].Transform.position - agent.Transform.position;
                    float ahead = Vector3.Dot(delta, direction);
                    if (ahead > 0f && ahead < 12f && Vector3.Cross(delta, direction).magnitude < 2.5f)
                        desired = Mathf.Min(desired, Mathf.Max(0f, (ahead - 5f) * 1.8f));
                }

                if (distance < 3f)
                    agent.Waypoint = (agent.Waypoint + 1) % _waypoints.Length;

                Vector3 next = agent.Transform.position + direction * (desired * Time.fixedDeltaTime);
                agent.Body.MovePosition(next);
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(direction, Vector3.up);
                    agent.Body.MoveRotation(Quaternion.Slerp(agent.Transform.rotation, look, Time.fixedDeltaTime * 4f));
                }
            }
        }
    }
}
