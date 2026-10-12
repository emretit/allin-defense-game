using System.Collections;
using UnityEngine;
using AllinDefense.Grid;

namespace AllinDefense.Units
{
    public class UnitRespawnSpawner : MonoBehaviour
    {
        [SerializeField] private FriendlyUnitBase unitPrefab;
        [SerializeField] private float respawnDelay = 6f;

        private FriendlyUnitBase activeUnit;
        private Coroutine respawnRoutine;

        private void Start()
        {
            SpawnUnit();
        }

        private void Update()
        {
            if (activeUnit == null || !activeUnit.IsDefeated || respawnRoutine != null)
                return;

            respawnRoutine = StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            SpawnUnit();
            respawnRoutine = null;
        }

        private void SpawnUnit()
        {
            if (unitPrefab == null)
                return;

            Vector3 rally = transform.position;
            var points = CommandPointManager.Instance;
            if (points != null && points.TryGetNearestPoint(transform.position, out var point))
                rally = point.Position;

            if (activeUnit == null)
                activeUnit = Instantiate(unitPrefab, transform.position, Quaternion.identity);

            activeUnit.ResetUnit(transform.position, rally);
        }
    }
}
