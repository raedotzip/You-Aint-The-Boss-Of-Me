/**
 * Separates the map into thirds by spawning 3 walls radiating from the boss,
 * 120 degrees apart, with one end anchored at the boss position.
 *
 * Flow:
 *   1. Warning phase (warningTime): floor highlights show where walls will appear.
 *      Boss stays still.
 *   2. Active phase (activeTime): walls spawn, boss resumes normal behavior.
 *      Player takes damage if standing inside a wall.
 *   3. After activeTime elapses, attack ends and boss picks next state.
 */

using UnityEngine;

public class Boss1MapSeparatorAttack : EnemyBaseState
{
    private float wallWidth      = 1f;
    private float wallHeight     = 3f;
    private float wallLength     = 30f;
    private float overlapBehind  = 3f;   // how far behind the boss the wall starts
    private float warningTime    = 2f;
    private float activeTime     = 8f;

    // Raycasting to clamp wall to platform
    private float raycastStep       = 1f;    // check every 1 unit along the wall
    private float maxHeightDiff     = 2f;    // if ground drops more than this, stop the wall
    private float raycastFromHeight = 10f;   // cast down from this height above the start point
    private LayerMask groundLayer;

    // Actual lengths computed per-attack after raycasting
    private float[] wallLengths = new float[3];

    private float   stateTimer    = 0f;
    private bool    wallsSpawned  = false;
    private bool    attackDone    = false;

    // IDs of floor highlight shapes so we can remove them when walls appear
    private int[] highlightIds = new int[3];

    // Cached per-attack data
    private Vector3   bossPos;
    private Vector3[] wallDirs = new Vector3[3];

    public override void EnterState(EnemyStateManager state)
    {
        stateTimer   = 0f;
        wallsSpawned = false;
        attackDone   = false;

        // Use whatever layer your platform mesh is on — "Default" catches most cases
        groundLayer = LayerMask.GetMask("Default", "Ground", "Terrain", "MapWall");

        bossPos = state.transform.position;
        bossPos.y = 0f;

        Vector3 toPlayer = state.player.position - bossPos;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f)
            toPlayer = Vector3.forward;

        // Player direction angle from boss
        float playerAngle = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;

        // Place walls at +60, +180, +300 degrees from the player angle
        // This puts the player exactly in the center of the first 120° gap
        for (int i = 0; i < 3; i++)
        {
            float wallAngle = playerAngle + 60f + i * 120f;
            float rad = wallAngle * Mathf.Deg2Rad;
            wallDirs[i] = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            // Wall starts behind the boss — compute length from that actual start point
            Vector3 wallStart = bossPos - wallDirs[i] * overlapBehind;
            wallLengths[i] = ComputeWallLength(wallStart, wallDirs[i]);
        }

        SpawnWarningHighlights(state);
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (attackDone) return;

        stateTimer += Time.deltaTime;

        // --- Warning phase ends: remove highlights, spawn walls ---
        if (!wallsSpawned && stateTimer >= warningTime)
        {
            RemoveHighlights();
            SpawnWalls(state);
            wallsSpawned = true;
        }

        // --- Active phase ends: finish attack ---
        if (wallsSpawned && stateTimer >= warningTime + activeTime)
        {
            attackDone = true;
            ((Boss1StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        return 0f;
    }

    // ===============================
    // WALL LENGTH — clamp to platform edge
    // ===============================
    // origin is the actual wall start point (already offset behind the boss)
    // returns total wall length from that start point
    private float ComputeWallLength(Vector3 wallStart, Vector3 dir)
    {
        float baseHeight = SampleGroundHeight(wallStart);
        float maxLength  = wallLength + overlapBehind;

        // If there's no ground even at the start, return a minimum wall
        if (baseHeight == float.MinValue)
            return 2f;

        float dist = 0f;
        while (dist <= maxLength)
        {
            Vector3 samplePos = wallStart + dir * dist;
            float h = SampleGroundHeight(samplePos);

            if (h == float.MinValue || Mathf.Abs(h - baseHeight) > maxHeightDiff)
                return Mathf.Max(2f, dist - raycastStep);

            dist += raycastStep;
        }

        return maxLength;
    }

    // Returns float.MinValue if no ground was found
    private float SampleGroundHeight(Vector3 pos)
    {
        Vector3 rayOrigin = pos + Vector3.up * raycastFromHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastFromHeight + 5f, groundLayer))
            return hit.point.y;
        return float.MinValue;
    }

    // ===============================
    // WARNING HIGHLIGHTS
    // ===============================
    private void SpawnWarningHighlights(EnemyStateManager state)
    {
        if (MapHighlightManager.Instance == null) return;

        for (int i = 0; i < 3; i++)
        {
            float   totalLength = wallLengths[i];
            Vector3 wallStart   = bossPos - wallDirs[i] * overlapBehind;
            Vector3 midpoint    = wallStart + wallDirs[i] * (totalLength / 2f);

            MapShape shape = new MapShape
            {
                type        = ShapeType.Box,
                position    = midpoint + Vector3.up * 8f,
                rotation    = Quaternion.LookRotation(wallDirs[i]),
                size        = new Vector3(wallWidth * 2f, 0.1f, totalLength),
                color       = new Color(1f, 0.6f, 0f, 0.7f),
                maxLifeTime = warningTime + 0.1f,
            };

            highlightIds[i] = MapHighlightManager.Instance.CreateShape(shape);
        }
    }

    private void RemoveHighlights()
    {
        if (MapHighlightManager.Instance == null) return;

        for (int i = 0; i < 3; i++)
            MapHighlightManager.Instance.RemoveShape(highlightIds[i]);
    }

    // ===============================
    // WALL SPAWN
    // ===============================
    private void SpawnWalls(EnemyStateManager state)
    {
        for (int i = 0; i < 3; i++)
        {
            float   totalLength = wallLengths[i];
            Vector3 wallStart   = bossPos - wallDirs[i] * overlapBehind;
            Vector3 midpoint    = wallStart + wallDirs[i] * (totalLength / 2f);
            SpawnWall(midpoint, wallDirs[i], totalLength, state);
        }
    }

    private void SpawnWall(Vector3 position, Vector3 direction, float length, EnemyStateManager state)
    {
        Quaternion rotation = Quaternion.LookRotation(direction);

        Obstacle o = new Obstacle
        {
            position       = position,
            rotation       = rotation,

            shapeType      = ObstacleShapeType.Box,
            boxHalfExtents = new Vector3(wallWidth / 2f, wallHeight / 2f, length / 2f),

            warningDuration = 0f,    // already warned via floor highlight
            activeDuration  = activeTime,

            movementType   = ObstacleMovementType.Stationary,
            scalesOverTime = false,

            visualPrefab   = state.obstacleData.wallPrefab,
        };

        ObstacleManager.Instance.SpawnObstacle(o);
    }
}
