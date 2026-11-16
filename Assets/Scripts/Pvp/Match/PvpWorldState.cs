using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Odyssey.Pvp
{
    // The host simulates the authored level. Guests display bounded chunks of its state;
    // each node has its own sequence so reordered/dropped UDP chunks cannot rewind it.
    internal sealed class PvpWorldState
    {
        private sealed class Node
        {
            public Transform Transform;
            public Rigidbody2D Body;
            public Collider2D[] Colliders;
            public SpriteRenderer[] Renderers;
            public uint Sequence;
        }
        private readonly List<Node> nodes = new List<Node>();
        private readonly Dictionary<uint, Sprite> sprites = new Dictionary<uint, Sprite>();
        private readonly bool host;
        private int cursor;
        private uint sequence;
        private const int ChunkBytes = 900;
        private const int ChunksPerTick = 6;
        internal int NodeCount => nodes.Count;
        internal uint LayoutHash { get; private set; } = 2166136261;

        internal PvpWorldState(Scene scene, bool authoritative)
        {
            host = authoritative;
            var roots = scene.GetRootGameObjects();
            var selected = new HashSet<Transform>();
            foreach (var root in roots)
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour is Enemy.EnemyDeath || behaviour is CampaignEnemy ||
                        behaviour is CampaignInteractable || behaviour is DonutFallPlatform ||
                        behaviour is VerticalRidePlatform || behaviour is OrbitRidePlatform ||
                        behaviour is OptionalCampaignCoin || behaviour is Winning.FloatingCoin)
                        foreach (var child in behaviour.GetComponentsInChildren<Transform>(true))
                            if (child.name != "Spinning icon coin") selected.Add(child);
                }
            foreach (var transform in selected.OrderBy(Path, StringComparer.Ordinal))
            {
                var colliders = transform.GetComponents<Collider2D>();
                var renderers = transform.GetComponents<SpriteRenderer>();
                if (colliders.Length > 32 || renderers.Length > 8)
                    throw new InvalidOperationException("PvP world node exceeds its bounded component count.");
                foreach (char letter in Path(transform) + ":" + colliders.Length + ":" + renderers.Length)
                    LayoutHash = (LayoutHash ^ letter) * 16777619;
                var body = transform.GetComponent<Rigidbody2D>();
                nodes.Add(new Node { Transform = transform, Body = body, Colliders = colliders, Renderers = renderers });
                if (!authoritative)
                {
                    if (body) body.simulated = false;
                    foreach (var behaviour in transform.GetComponents<MonoBehaviour>())
                        if (!(behaviour is OptionalCampaignCoin) && !(behaviour is Winning.FloatingCoin)) behaviour.enabled = false;
                    foreach (var animator in transform.GetComponents<Animator>()) animator.enabled = false;
                }
            }
            if (nodes.Count > ushort.MaxValue) throw new InvalidOperationException("PvP world contains too many dynamic nodes.");
            foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                if (sprite) sprites[SpriteKey(sprite)] = sprite;
        }

        private static string Path(Transform transform)
        {
            string key = "";
            while (transform)
            {
                key = transform.GetSiblingIndex().ToString("D5") + ":" + transform.name + "/" + key;
                transform = transform.parent;
            }
            return key;
        }
        private static uint SpriteKey(Sprite sprite)
        {
            if (!sprite) return 0;
            string key = sprite.texture.name + ":" + sprite.name + ":" + sprite.rect.x.ToString("R", CultureInfo.InvariantCulture) + ":" + sprite.rect.y.ToString("R", CultureInfo.InvariantCulture) + ":" + sprite.rect.width.ToString("R", CultureInfo.InvariantCulture) + ":" + sprite.rect.height.ToString("R", CultureInfo.InvariantCulture);
            uint hash = 2166136261;
            foreach (char letter in key) hash = (hash ^ letter) * 16777619;
            return hash == 0 ? 1u : hash;
        }

        internal IEnumerable<byte[]> CaptureChunks()
        {
            if (!host || nodes.Count == 0) yield break;
            uint tick = ++sequence;
            int remaining = nodes.Count;
            for (int chunk = 0; chunk < ChunksPerTick && remaining > 0; chunk++)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                writer.Write(tick);
                writer.Write(LayoutHash);
                writer.Write((ushort)0);
                ushort count = 0;
                while (remaining > 0)
                {
                    byte[] entry = CaptureNode(cursor);
                    if (stream.Length + entry.Length > ChunkBytes && count > 0) break;
                    writer.Write(entry);
                    cursor = (cursor + 1) % nodes.Count;
                    remaining--;
                    count++;
                }
                stream.Position = 8;
                writer.Write(count);
                writer.Flush();
                yield return stream.ToArray();
            }
        }
        private byte[] CaptureNode(int index)
        {
            var node = nodes[index];
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((ushort)index);
            bool exists = node.Transform;
            writer.Write(exists);
            if (!exists) return stream.ToArray();
            var transform = node.Transform;
            writer.Write(transform.gameObject.activeSelf);
            var p = transform.localPosition;
            writer.Write(p.x); writer.Write(p.y); writer.Write(p.z);
            writer.Write(transform.localEulerAngles.z);
            writer.Write(node.Body ? node.Body.velocity.x : 0f);
            writer.Write(node.Body ? node.Body.velocity.y : 0f);
            uint colliders = 0;
            for (int i = 0; i < node.Colliders.Length; i++)
                if (node.Colliders[i] && node.Colliders[i].enabled) colliders |= 1u << i;
            writer.Write(colliders);
            writer.Write((byte)node.Renderers.Length);
            foreach (var renderer in node.Renderers)
            {
                writer.Write(renderer && renderer.enabled);
                writer.Write(renderer && renderer.flipX);
                writer.Write(renderer && renderer.flipY);
                writer.Write(renderer ? SpriteKey(renderer.sprite) : 0);
            }
            var sorting = transform.GetComponent<SortingGroup>();
            writer.Write(sorting != null);
            if (sorting) { writer.Write(sorting.sortingLayerID); writer.Write(sorting.sortingOrder); }
            return stream.ToArray();
        }
        internal void ReadChunk(BinaryReader reader)
        {
            if (host) return;
            uint next = reader.ReadUInt32();
            if (reader.ReadUInt32() != LayoutHash) throw new InvalidDataException("PvP world layout differs from the host.");
            int count = reader.ReadUInt16();
            if (count > nodes.Count) throw new InvalidDataException("PvP world node count is invalid.");
            for (int item = 0; item < count; item++)
            {
                int index = reader.ReadUInt16();
                if (index >= nodes.Count) throw new InvalidDataException("PvP world node index is invalid.");
                var node = nodes[index];
                bool apply = next > node.Sequence && node.Transform;
                bool exists = reader.ReadBoolean();
                if (!exists)
                {
                    if (apply) { node.Sequence = next; node.Transform.gameObject.SetActive(false); }
                    continue;
                }
                bool active = reader.ReadBoolean();
                var position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                float rotation = reader.ReadSingle();
                var velocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                uint colliders = reader.ReadUInt32();
                int renderers = reader.ReadByte();
                if (renderers != node.Renderers.Length || !Finite(position.x) || !Finite(position.y) || !Finite(position.z) ||
                    !Finite(rotation) || !Finite(velocity.x) || !Finite(velocity.y)) throw new InvalidDataException("PvP world state is invalid.");
                if (apply)
                {
                    node.Sequence = next;
                    node.Transform.gameObject.SetActive(active);
                    node.Transform.localPosition = position;
                    node.Transform.localRotation = Quaternion.Euler(0, 0, rotation);
                    if (node.Body) node.Body.velocity = velocity;
                    for (int i = 0; i < node.Colliders.Length; i++)
                        if (node.Colliders[i]) node.Colliders[i].enabled = (colliders & (1u << i)) != 0;
                }
                foreach (var renderer in node.Renderers)
                {
                    bool enabled = reader.ReadBoolean(), flipX = reader.ReadBoolean(), flipY = reader.ReadBoolean();
                    uint sprite = reader.ReadUInt32();
                    if (!apply || !renderer) continue;
                    renderer.enabled = enabled; renderer.flipX = flipX; renderer.flipY = flipY;
                    if (sprite == 0) renderer.sprite = null;
                    else if (sprites.TryGetValue(sprite, out var source)) renderer.sprite = source;
                }
                bool sorted = reader.ReadBoolean();
                if (sorted)
                {
                    int layer = reader.ReadInt32(), order = reader.ReadInt32();
                    if (apply)
                    {
                        var group = node.Transform.GetComponent<SortingGroup>();
                        if (!group) group = node.Transform.gameObject.AddComponent<SortingGroup>();
                        group.sortingLayerID = layer; group.sortingOrder = order;
                    }
                }
            }
        }
        private static bool Finite(float number) => !float.IsNaN(number) && !float.IsInfinity(number);
    }
}
