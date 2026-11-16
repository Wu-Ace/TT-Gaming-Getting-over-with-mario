using System.Collections.Generic;
using UnityEngine;

namespace Odyssey.Pvp
{
    public sealed class PvpContact
    {
        public const float MaxDisplacement = 0.12f;
        private const float ContactDistance = 0.015f;
        private const float SeparationDistance = 0.05f;

        private readonly List<PvpActor> activeActors = new List<PvpActor>(4);
        private readonly List<float> pushes = new List<float>(4);
        private Dictionary<int, int> previousSpawns = new Dictionary<int, int>(4);
        private Dictionary<int, int> currentSpawns = new Dictionary<int, int>(4);
        private HashSet<(int, int)> previousContacts = new HashSet<(int, int)>();
        private HashSet<(int, int)> currentContacts = new HashSet<(int, int)>();

        public void Step(IReadOnlyList<PvpActor> actors)
        {
            activeActors.Clear();
            pushes.Clear();
            currentSpawns.Clear();
            currentContacts.Clear();

            foreach (var actor in actors)
            {
                if (!actor || !actor.ContactActive)
                    continue;

                activeActors.Add(actor);
                pushes.Add(0f);
                currentSpawns.Add(actor.GetInstanceID(), actor.SpawnRevision);
            }

            for (int i = 0; i < activeActors.Count; i++)
            {
                for (int j = i + 1; j < activeActors.Count; j++)
                    CollectContact(i, j);
            }

            for (int i = 0; i < activeActors.Count; i++)
            {
                float limit = Mathf.Min(MaxDisplacement, activeActors[i].BodyCollider.bounds.size.x * 0.15f);
                activeActors[i].PushHorizontally(Mathf.Clamp(pushes[i], -limit, limit));
            }

            var oldContacts = previousContacts;
            previousContacts = currentContacts;
            currentContacts = oldContacts;
            var oldSpawns = previousSpawns;
            previousSpawns = currentSpawns;
            currentSpawns = oldSpawns;
        }

        private void CollectContact(int firstIndex, int secondIndex)
        {
            var first = activeActors[firstIndex];
            var second = activeActors[secondIndex];
            int firstId = first.GetInstanceID();
            int secondId = second.GetInstanceID();
            var pair = (Mathf.Min(firstId, secondId), Mathf.Max(firstId, secondId));
            bool alreadyTouching = previousContacts.Contains(pair);
            var separation = first.BodyCollider.Distance(second.BodyCollider);
            if (!separation.isValid ||
                separation.distance > (alreadyTouching ? SeparationDistance : ContactDistance))
                return;

            currentContacts.Add(pair);
            if (alreadyTouching || JustSpawned(first, firstId) || JustSpawned(second, secondId))
                return;

            float direction = first.Body.position.x <= second.Body.position.x ? -1f : 1f;
            pushes[firstIndex] += direction * MaxDisplacement;
            pushes[secondIndex] -= direction * MaxDisplacement;
        }

        private bool JustSpawned(PvpActor actor, int id) =>
            !previousSpawns.TryGetValue(id, out int revision) || revision != actor.SpawnRevision;

        public void Prime(IReadOnlyList<PvpActor> actors)
        {
            Clear();
            Step(actors);
        }

        public void Forget(PvpActor actor)
        {
            if (!actor)
                return;

            int id = actor.GetInstanceID();
            previousSpawns.Remove(id);
            previousContacts.RemoveWhere(pair => pair.Item1 == id || pair.Item2 == id);
        }

        public void Clear()
        {
            previousSpawns.Clear();
            previousContacts.Clear();
        }
    }
}
