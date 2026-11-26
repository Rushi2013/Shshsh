using System.Collections.Generic;
using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Ambient traffic that follows the lane graph extracted from the actual flat
    /// road mesh in CIT.obj. No city-bounds rectangle, no invisible fallback ground.
    /// Each car picks the next connected road quad at a junction, keeps a following
    /// distance from other traffic and from the player, and moves kinematically on
    /// the validated surface.
    /// </summary>
    public sealed class TrafficSystem : MonoBehaviour
    {
        private sealed class Agent
        {
            public Transform Transform;
            public Rigidbody Body;
            public ArcadeCarController Player;
            public int Node;
            public int Previous;
            public float Speed;
            public float HeightOffset;
        }

        private readonly List<Agent> _agents = new List<Agent>();
        private CityBuilder _city;
        private Vector3[] _nodes;
        private bool _running;

        public int ActiveCars => _agents.Count;

        public void Build(CityBuilder city, int carCount = 8)
        {
            _city = city;
            _nodes = city != null ? city.LaneWaypoints : null;
            if (_nodes == null || _nodes.Length < 4)
            {
                Debug.LogWarning("[CityDrive] No validated road nodes; traffic disabled.");
                return;
            }

            GameObject[] prefabs = Resources.LoadAll<GameObject>("TrafficCars");
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning("[CityDrive] No traffic prefabs found in Resources/TrafficCars.");
                return;
            }

            List<int> spawns = new List<int>();
            for (int i = 0; i < _nodes.Length; i++)
                if (_city.NeighborsOf(i).Length > 0) spawns.Add(i);
            if (spawns.Count < 3)
            {
                Debug.LogWarning("[CityDrive] Road graph has too few connected nodes; traffic disabled.");
                return;
            }

            int count = Mathf.Min(carCount, prefabs.Length * 2, spawns.Count / 2);
            ArcadeCarController player = FindObjectOfType<ArcadeCarController>();
            for (int i = 0; i < count; i++)
            {
                int index = spawns[(i * spawns.Count / Mathf.Max(1, count)) % spawns.Count];
                GameObject prefab = prefabs[i % prefabs.Length];
                int[] neighbors = _city.NeighborsOf(index);
                Vector3 heading = _nodes[neighbors[0]] - _nodes[index];
                GameObject car = Instantiate(prefab, _nodes[index] + Vector3.up * 0.06f,
                    Quaternion.LookRotation(heading.normalized, Vector3.up));
                car.name = "Traffic " + prefab.name + " " + (i + 1);
                FitToRoad(car, _nodes[index]);

                Rigidbody body = car.GetComponent<Rigidbody>();
                if (body == null) body = car.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;

                Collider[] colliders = car.GetComponentsInChildren<Collider>(true);
                if (colliders.Length == 0)
                {
                    BoxCollider box = car.AddComponent<BoxCollider>();
                    box.size = new Vector3(1.9f, 1.4f, 4.2f);
                    box.center = new Vector3(0f, 0.7f, 0f);
                }

                _agents.Add(new Agent
                {
                    Transform = car.transform,
                    Body = body,
                    Player = player,
                    Node = index,
                    Previous = -1,
                    Speed = 8.5f + (i % 5) * 1.1f,
                    HeightOffset = car.transform.position.y - _nodes[index].y
                });
            }

            _running = _agents.Count > 0;
            Debug.Log("[CityDrive] Road traffic active: " + _agents.Count + " cars on the CIT lane graph.");
        }

        private static void FitToRoad(GameObject car, Vector3 roadPoint)
        {
            Bounds bounds = BoundsOf(car);
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.1f && Mathf.Abs(footprint - 4.2f) > 0.4f)
                car.transform.localScale *= 4.2f / footprint;
            Physics.SyncTransforms();
            bounds = BoundsOf(car);
            if (bounds.size.x > 0f)
                car.transform.position += Vector3.up * (roadPoint.y + 0.04f - bounds.min.y);
        }

        private static Bounds BoundsOf(GameObject car)
        {
            Bounds bounds = new Bounds(car.transform.position, Vector3.one);
            bool found = false;
            foreach (Renderer renderer in car.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.bounds.size == Vector3.zero) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private int ChooseNext(Agent agent)
        {
            int[] options = _city.NeighborsOf(agent.Node);
            if (options.Length == 0) return -1;
            if (options.Length == 1) return options[0];

            Vector3 forward = agent.Transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) return options[0];

            int best = options[0];
            float bestScore = float.MinValue;
            for (int i = 0; i < options.Length; i++)
            {
                int candidate = options[i];
                if (candidate == agent.Previous && options.Length > 1) continue;
                Vector3 direction = _nodes[candidate] - _nodes[agent.Node];
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.01f) continue;
                float alignment = Vector3.Dot(forward, direction.normalized);
                float jitter = Mathf.Repeat(agent.Node * 0.37f + candidate * 0.19f, 0.08f);
                float score = alignment + jitter;
                if (score > bestScore) { bestScore = score; best = candidate; }
            }
            return best;
        }

        private void FixedUpdate()
        {
            if (!_running || _nodes == null || _city == null) return;
            float dt = Time.fixedDeltaTime;

            for (int i = 0; i < _agents.Count; i++)
            {
                Agent agent = _agents[i];
                int next = ChooseNext(agent);
                if (next < 0) continue;

                Vector3 target = _nodes[next];
                Vector3 direction = target - agent.Transform.position;
                direction.y = 0f;
                float distance = direction.magnitude;
                if (distance < 0.01f)
                {
                    agent.Previous = agent.Node;
                    agent.Node = next;
                    continue;
                }
                direction /= distance;

                float desired = agent.Speed;
                // Slow for turns.
                int[] options = _city.NeighborsOf(next);
                if (options.Length > 1) desired *= 0.72f;

                for (int j = 0; j < _agents.Count; j++)
                {
                    if (i == j) continue;
                    desired = Mathf.Min(desired, FollowingSpeed(agent.Transform.position, direction,
                        _agents[j].Transform.position, desired));
                }
                if (agent.Player != null)
                    desired = Mathf.Min(desired, FollowingSpeed(agent.Transform.position, direction,
                        agent.Player.transform.position, desired));

                if (distance < 0.5f)
                {
                    agent.Previous = agent.Node;
                    agent.Node = next;
                }

                Vector3 position = agent.Transform.position + direction * Mathf.Min(distance, desired * dt);
                position.y = Mathf.Lerp(agent.Transform.position.y, target.y + agent.HeightOffset, dt * 4f);
                agent.Body.MovePosition(position);
                agent.Body.MoveRotation(Quaternion.Slerp(agent.Transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up), dt * 3.5f));
            }
        }

        private static float FollowingSpeed(Vector3 origin, Vector3 direction, Vector3 other, float current)
        {
            Vector3 delta = other - origin;
            float ahead = Vector3.Dot(delta, direction);
            if (ahead <= 0f || ahead > 16f) return current;
            float lateral = Vector3.Cross(delta, direction).magnitude;
            if (lateral > 3f) return current;
            return Mathf.Max(0f, (ahead - 6f) * 1.6f);
        }
    }
}
