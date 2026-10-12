using UnityEngine;

namespace AllinDefense.Grid
{
    public class OutpostSlotManager : MonoBehaviour
    {
        public static OutpostSlotManager Instance { get; private set; }

        [SerializeField] private float selectionRadius = 0.85f;
        [SerializeField] private OutpostSlot[] slots;

        public int SlotCount => slots == null ? 0 : slots.Length;
        public float SelectionRadius => selectionRadius;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureDefaultSlots();
        }

        public bool TryGetNearestAvailableSlot(Vector3 worldPosition, out OutpostSlot slot)
        {
            slot = null;
            if (slots == null || slots.Length == 0)
                return false;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < slots.Length; i++)
            {
                var candidate = slots[i];
                if (candidate == null || !candidate.IsAvailable)
                    continue;

                float distance = Vector3.Distance(worldPosition, candidate.Position);
                if (distance > selectionRadius || distance >= bestDistance)
                    continue;

                bestDistance = distance;
                slot = candidate;
            }

            return slot != null;
        }

        public bool TryPlaceOutpost(Vector3 worldPosition, GameObject outpost)
        {
            if (outpost == null)
                return false;

            if (!TryGetNearestAvailableSlot(worldPosition, out var slot))
                return false;

            slot.Place(outpost);
            return true;
        }

        public bool TryReleaseOutpost(GameObject outpost)
        {
            if (outpost == null || slots == null)
                return false;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Occupant != outpost)
                    continue;

                slot.Clear();
                return true;
            }

            return false;
        }

        private void EnsureDefaultSlots()
        {
            if (slots != null && slots.Length > 0)
                return;

            slots = new[]
            {
                new OutpostSlot("slot_valley_west_entry", new Vector3(2.5f, 2.1f, 0f), OutpostSlotRole.Frontline),
                new OutpostSlot("slot_valley_west_ridge", new Vector3(5.5f, 2.8f, 0f), OutpostSlotRole.Ranged),
                new OutpostSlot("slot_valley_north_bend", new Vector3(6.6f, 5.9f, 0f), OutpostSlotRole.Control),
                new OutpostSlot("slot_valley_mid_cross", new Vector3(10.0f, 3.3f, 0f), OutpostSlotRole.Frontline),
                new OutpostSlot("slot_valley_east_ridge", new Vector3(13.8f, 4.4f, 0f), OutpostSlotRole.Ranged),
                new OutpostSlot("slot_valley_gate_low", new Vector3(15.6f, 1.0f, 0f), OutpostSlotRole.Control),
                new OutpostSlot("slot_valley_gate_high", new Vector3(18.2f, 4.6f, 0f), OutpostSlotRole.Ranged),
                new OutpostSlot("slot_valley_final_stand", new Vector3(19.2f, 7.9f, 0f), OutpostSlotRole.Frontline)
            };
        }

        private void OnDrawGizmos()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null)
                    continue;

                Gizmos.color = slot.IsAvailable ? Color.cyan : Color.gray;
                Gizmos.DrawWireSphere(slot.Position, selectionRadius);
                Gizmos.DrawCube(slot.Position, new Vector3(0.28f, 0.28f, 0.02f));
            }
        }
    }

    public enum OutpostSlotRole
    {
        Frontline,
        Ranged,
        Control
    }

    [System.Serializable]
    public class OutpostSlot
    {
        [SerializeField] private string id;
        [SerializeField] private Vector3 position;
        [SerializeField] private OutpostSlotRole role;
        [SerializeField] private GameObject occupant;

        public OutpostSlot(string id, Vector3 position, OutpostSlotRole role)
        {
            this.id = id;
            this.position = position;
            this.role = role;
        }

        public string Id => id;
        public Vector3 Position => position;
        public OutpostSlotRole Role => role;
        public GameObject Occupant => occupant;
        public bool IsAvailable => occupant == null;

        public void Place(GameObject newOccupant)
        {
            occupant = newOccupant;
            occupant.transform.position = position;
        }

        public void Clear()
        {
            occupant = null;
        }
    }
}
