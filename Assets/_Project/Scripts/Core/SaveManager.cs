using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdLamp
{
    /// <summary>
    /// Checkpoint saves (entering the villa, restoring the generator). Loading reloads the scene,
    /// rebuilds the world, then re-applies story state and replays persistent event effects silently.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [Serializable]
        public class ItemState
        {
            public string key;
            public bool active;
            public bool untouched;
            public string socket;
            public Vector3 position;
            public Quaternion rotation;
        }

        [Serializable]
        public class DoorState
        {
            public string id;
            public bool open;
            public bool locked;
        }

        [Serializable]
        public class SaveData
        {
            public string label;
            public List<string> flags = new List<string>();
            public List<CounterEntry> counters = new List<CounterEntry>();
            public float clock;
            public int perception;
            public List<string> perceptionSources = new List<string>();
            public List<string> fired = new List<string>();
            public List<Sms> inbox = new List<Sms>();
            public List<ItemState> items = new List<ItemState>();
            public List<DoorState> doors = new List<DoorState>();
            public List<string> lightsOn = new List<string>();
            public List<PlacementSocket.LogEntry> placements = new List<PlacementSocket.LogEntry>();
            public bool power = true;
            public Vector3 playerPosition;
            public float playerYaw;
        }

        /// <summary>Set before a scene reload; consumed by SliceBootstrap after the world is rebuilt.</summary>
        public static SaveData Pending;

        static string PathOnDisk => Path.Combine(Application.persistentDataPath, "thirdlamp_checkpoint.json");

        public bool HasCheckpoint => File.Exists(PathOnDisk);

        public void Checkpoint(string label)
        {
            try
            {
                var data = Capture(label);
                File.WriteAllText(PathOnDisk, JsonUtility.ToJson(data, true));
                Game.Hud.Tip("Checkpoint", 2f);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void LoadCheckpoint()
        {
            if (!HasCheckpoint)
            {
                Game.Hud.Subtitle("(No checkpoint yet.)", 2f);
                return;
            }
            Pending = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathOnDisk));
            Reload();
        }

        public void Restart()
        {
            Pending = null;
            if (HasCheckpoint) File.Delete(PathOnDisk);
            Reload();
        }

        static void Reload()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0
                ? SceneManager.GetActiveScene().name
                : "Slice");
        }

        SaveData Capture(string label)
        {
            var d = new SaveData
            {
                label = label,
                flags = Game.State.AllFlags(),
                counters = Game.State.AllCounters(),
                clock = Game.State.clockMinutes,
                perception = Game.Perception.Value,
                perceptionSources = Game.Perception.AllSources(),
                fired = Game.Director.FiredIds(),
                inbox = new List<Sms>(Game.Phone.Inbox),
                lightsOn = Game.Lighting.SwitchedOnGroups(),
                placements = new List<PlacementSocket.LogEntry>(PlacementSocket.Log),
                power = Game.Lighting.PowerOn,
                playerPosition = Game.Player.transform.position,
                playerYaw = Game.Player.Yaw,
            };

            foreach (var kv in Game.World.All)
            {
                var go = kv.Value;
                if (go == null) continue;
                var item = go.GetComponent<InspectableObject>();
                if (item != null && item.canPickUp)
                {
                    var s = new ItemState { key = kv.Key, active = go.activeSelf, untouched = !item.HasBeenMoved };
                    if (s.untouched) { }
                    else if (item.IsHeld)
                    {
                        s.position = Game.Player.transform.position + Game.Player.transform.forward * 0.4f + Vector3.up * 0.02f;
                        s.rotation = Quaternion.identity;
                    }
                    else if (item.CurrentSocket != null)
                    {
                        s.socket = item.CurrentSocket.GetComponent<WorldTag>()?.id;
                    }
                    else
                    {
                        s.position = go.transform.position;
                        s.rotation = go.transform.rotation;
                    }
                    d.items.Add(s);
                }
                var door = go.GetComponent<Openable>();
                if (door != null) d.doors.Add(new DoorState { id = kv.Key, open = door.IsOpen, locked = door.locked });
            }
            return d;
        }

        public void Apply(SaveData d)
        {
            Game.State.Restore(d.flags, d.counters, d.clock);
            Game.Perception.Restore(d.perception, d.perceptionSources);
            if (!d.power) Game.Lighting.SetPower(false);
            Game.Lighting.RestoreGroups(d.lightsOn);
            Game.Director.RestoreFired(d.fired);

            foreach (var ds in d.doors)
            {
                var door = Game.World.Get<Openable>(ds.id);
                if (door == null) continue;
                door.locked = ds.locked;
                door.SetOpen(ds.open, true, true);
            }

            foreach (var s in d.items)
            {
                var go = Game.World.Find(s.key);
                if (go == null) continue;
                var item = go.GetComponent<InspectableObject>();
                go.SetActive(s.active);
                if (s.untouched) continue;
                item.HasBeenMoved = true;
                if (item.CurrentSocket != null) item.CurrentSocket.Release(item);
                if (!string.IsNullOrEmpty(s.socket))
                {
                    var socket = Game.World.Get<PlacementSocket>(s.socket);
                    if (socket != null) { socket.Place(item, true); continue; }
                }
                go.transform.SetParent(null, true);
                go.transform.SetPositionAndRotation(s.position, s.rotation);
            }

            foreach (var p in FindObjectsByType<FlagPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) p.RestoreFromFlags();
            foreach (var sw in FindObjectsByType<LightSwitch>(FindObjectsSortMode.None)) sw.RefreshVisual();

            Game.Phone.Restore(d.inbox);
            PlacementSocket.Log.Clear();
            PlacementSocket.Log.AddRange(d.placements);
            Game.Player.Teleport(d.playerPosition, d.playerYaw);
            Game.State.clockRunning = true;
        }
    }
}
