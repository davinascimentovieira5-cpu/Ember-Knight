using System.Collections.Generic;
using UnityEngine;
using EmberKnight.Enemies;

namespace EmberKnight.Rooms
{
    [System.Serializable]
    public class EnemySpawnPoint
    {
        public EnemyType type;
        public Vector2 position;
    }

    [System.Serializable]
    public class RoomDefinition
    {
        public string roomName = "Sala 1";
        public string subtitle = "Elimine os inimigos";
        public float width = 1600f;
        public List<EnemySpawnPoint> enemySpawns = new List<EnemySpawnPoint>();
        public List<Vector2> potionSpawns = new List<Vector2>();
    }

    /// <summary>
    /// Controla a progressão entre salas, replicando o array `rooms` do
    /// protótipo original. As plataformas de cada sala devem existir na
    /// cena (como GameObjects filhos, ativados/desativados por sala) —
    /// aqui cuidamos de spawns de inimigos, pickups e da condição de
    /// "sala limpa".
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject wispPrefab;
        public GameObject brutePrefab;
        public GameObject potionPrefab;

        [Header("Sala atual")]
        public int currentRoomIndex = 0;

        public List<RoomDefinition> rooms = new List<RoomDefinition>
        {
            new RoomDefinition {
                roomName = "Sala 1", subtitle = "Elimine os inimigos", width = 1600f,
                enemySpawns = new List<EnemySpawnPoint> {
                    new EnemySpawnPoint{ type = EnemyType.Wisp,  position = new Vector2(5.2f, 1f) },
                    new EnemySpawnPoint{ type = EnemyType.Wisp,  position = new Vector2(9.0f, 1f) },
                    new EnemySpawnPoint{ type = EnemyType.Brute, position = new Vector2(12.5f, 1f) },
                },
                potionSpawns = new List<Vector2> { new Vector2(2.5f, 1f) }
            },
            new RoomDefinition {
                roomName = "Sala 2", subtitle = "Corredor das lâminas", width = 1400f,
                enemySpawns = new List<EnemySpawnPoint> {
                    new EnemySpawnPoint{ type = EnemyType.Wisp,  position = new Vector2(3.4f, 1f) },
                    new EnemySpawnPoint{ type = EnemyType.Brute, position = new Vector2(6.4f, 1f) },
                    new EnemySpawnPoint{ type = EnemyType.Wisp,  position = new Vector2(9.4f, 1f) },
                    new EnemySpawnPoint{ type = EnemyType.Wisp,  position = new Vector2(12.0f, 1f) },
                },
                potionSpawns = new List<Vector2> { new Vector2(6.0f, 1f) }
            },
            new RoomDefinition {
                roomName = "Sala 3", subtitle = "Arena final", width = 1800f,
                enemySpawns = new List<EnemySpawnPoint> {
                    new EnemySpawnPoint{ type = EnemyType.Brute, position = new Vector2(4.0f, 1f) },
                    new EnemySpawnPoint{ type = EnemyType.Brute, position = new Vector2(8.0f, 1f) },
                    new EnemySpawnPoint{ type = EnemyType.Wisp,  position = new Vector2(12.0f, 1f) },
                    new EnemySpawnPoint{ type = EnemyType.Wisp,  position = new Vector2(15.0f, 1f) },
                    new EnemySpawnPoint{ type = EnemyType.Brute, position = new Vector2(16.5f, 1f) },
                },
                potionSpawns = new List<Vector2> { new Vector2(9.0f, 1f), new Vector2(15.0f, 1f) }
            },
        };

        readonly List<EnemyController> activeEnemies = new List<EnemyController>();

        public System.Action<RoomDefinition> OnRoomStarted;
        public System.Action<RoomDefinition> OnRoomCleared;
        public System.Action OnAllRoomsCleared;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            LoadRoom(0);
        }

        public void LoadRoom(int index)
        {
            currentRoomIndex = index;
            var def = rooms[index];

            ClearActiveEnemies();

            foreach (var spawn in def.enemySpawns)
            {
                GameObject prefab = spawn.type == EnemyType.Brute ? brutePrefab : wispPrefab;
                if (prefab == null) continue;

                var go = Instantiate(prefab, spawn.position, Quaternion.identity);
                var enemy = go.GetComponent<EnemyController>();
                if (enemy != null) activeEnemies.Add(enemy);
            }

            foreach (var pos in def.potionSpawns)
            {
                if (potionPrefab != null) Instantiate(potionPrefab, pos, Quaternion.identity);
            }

            OnRoomStarted?.Invoke(def);
        }

        void ClearActiveEnemies()
        {
            foreach (var e in activeEnemies)
            {
                if (e != null) Destroy(e.gameObject);
            }
            activeEnemies.Clear();
        }

        /// <summary>Chamado pelo EnemyController quando ele morre.</summary>
        public void NotifyEnemyDefeated(EnemyController enemy)
        {
            activeEnemies.Remove(enemy);

            if (activeEnemies.Count == 0)
            {
                OnRoomCleared?.Invoke(rooms[currentRoomIndex]);

                if (currentRoomIndex + 1 < rooms.Count)
                {
                    // dê um tempo antes de carregar a próxima sala, ou
                    // acione isso a partir de um portal/gate na cena
                    Invoke(nameof(AdvanceRoom), 1.5f);
                }
                else
                {
                    OnAllRoomsCleared?.Invoke();
                }
            }
        }

        void AdvanceRoom()
        {
            LoadRoom(currentRoomIndex + 1);
        }

        public RoomDefinition CurrentRoom => rooms[currentRoomIndex];
    }
}
