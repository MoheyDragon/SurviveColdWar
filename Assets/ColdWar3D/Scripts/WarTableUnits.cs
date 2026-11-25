using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The board's units after they arrive: where tank columns and warships stand, how they follow the front when
// territory changes hands, and how lost wars, factories and spies disappear from the map.
//  - Every tank stands on its own land tile, every warship on its own patch of open water: nothing is stacked.
//  - Tanks never stand or drive on water; warships keep a tile of water between them and any coast.
//  - When the land a column stands on changes sides, it drives to the enemy's new territory (or as close to the
//    enemy capital as it can get); a warship whose coast changed sails to the enemy's new coast.
//  - When an event takes wars, factories or spies away, the same number disappear from the board.
public partial class WarTable
{
    // A proxy war on the board: a tank column or a warship, and the ground or water it holds.
    class Deployment
    {
        public Side side;
        public Transform root;
        public Transform[] units;
        public int[] tiles;          // land tiles, one per tank
        public bool naval;
        public int sea = -1;         // the sea tile the ship holds
        public Vector3 site;         // where the war is marked on the board
        public bool ready;           // arrived (the war has started)
        public bool moving;          // driving or sailing to a new spot
    }

    readonly List<Deployment> deployments = new List<Deployment>();
    readonly HashSet<int> seaReserved = new HashSet<int>();
    Dictionary<int, int> seaDistance;
    float nextUnitCheck;

    // ---------------------------------------------------------------- Land

    bool IsLand(Vector3 point) => tileAt != null && tileAt.ContainsKey(HexKey(point));

    bool EnemyHolds(Side side, int tile) => side.isRed ? board.ThresholdOf(tile) >= board.Com : board.ThresholdOf(tile) < board.Com;

    // Tiles for a tank column: a lead tile on the enemy's land and the tiles beside and behind it, all free and clear
    // of buildings. When the enemy has no room left, the free land nearest the enemy (its capital, or its land).
    // A column already on the board (from >= 0) only looks at land it can reach by road.
    int[] ColumnTiles(Side side, int count, Vector3 near, int from = -1)
    {
        Side enemy = Enemy(side);
        HashSet<int> reach = from >= 0 ? Reachable(from) : null;
        var options = new List<int>();
        for (int i = 0; i < board.TileCount; i++)
        {
            if (!claimed.Contains(i) && EnemyHolds(side, i) && !OnBuildingGround(board.TileTop(i), 1.1f) && (reach == null || reach.Contains(i)))
                options.Add(i);
        }

        int[] result = PickCluster(options, count, near, enemy.capital, reach == null);
        if (result == null)
        {
            // No room on enemy land: the free land closest to the enemy capital (or, cut off from it, to the
            // nearest enemy land).
            Vector3 goal = enemy.capital;
            if (reach != null && !reach.Contains(board.NearestTile(Flat(enemy.capital))))
                goal = NearestEnemyLand(side, near);
            var any = new List<int>();
            for (int i = 0; i < board.TileCount; i++)
            {
                if (!claimed.Contains(i) && !OnBuildingGround(board.TileTop(i), 1.1f) && (reach == null || reach.Contains(i)))
                    any.Add(i);
            }
            result = PickCluster(any, count, goal, goal, false);
        }

        if (result == null)
            result = new[] { board.NearestTile(Flat(enemy.capital)) };
        foreach (int tile in result)
            claimed.Add(tile);
        return result;
    }

    // A lead tile plus count-1 free neighbours. Random among the candidates (so wars spread over the enemy's land)
    // or the one nearest the goal.
    int[] PickCluster(List<int> options, int count, Vector3 near, Vector3 goal, bool random)
    {
        if (options.Count == 0)
            return null;
        var free = new HashSet<int>(options);
        var order = new List<int>(options);
        if (random)
        {
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }
        else
            order.Sort((a, b) => (Flat(board.TileTop(a)) - Flat(goal)).sqrMagnitude.CompareTo((Flat(board.TileTop(b)) - Flat(goal)).sqrMagnitude));

        foreach (int lead in order)
        {
            var cluster = new List<int> { lead };
            // Neighbours to the left and right first (side by side, facing the enemy), so parked tanks never overlap
            // nose to tail.
            var around = new List<int>(LandNeighbours(lead));
            Vector2 toGoal = (Flat(goal) - Flat(board.TileTop(lead))).normalized;
            float Across(int tile) => Mathf.Abs(Vector2.Dot((Flat(board.TileTop(tile)) - Flat(board.TileTop(lead))).normalized, toGoal));
            around.Sort((a, b) => Across(a).CompareTo(Across(b)));
            foreach (int next in around)
            {
                if (cluster.Count >= count)
                    break;
                if (free.Contains(next))
                    cluster.Add(next);
            }
            if (cluster.Count >= count)
                return cluster.ToArray();
        }
        return null;
    }

    // Every land tile a column can drive to from a tile (staying off building ground).
    HashSet<int> Reachable(int from)
    {
        var seen = new HashSet<int> { from };
        var queue = new Queue<int>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (int next in LandNeighbours(current))
            {
                if (seen.Contains(next) || OnBuildingGround(board.TileTop(next), 0.3f))
                    continue;
                seen.Add(next);
                queue.Enqueue(next);
            }
        }
        return seen;
    }

    Vector3 NearestEnemyLand(Side side, Vector3 near)
    {
        float best = float.MaxValue;
        Vector3 spot = Enemy(side).capital;
        for (int i = 0; i < board.TileCount; i++)
        {
            if (!EnemyHolds(side, i))
                continue;
            float d = (Flat(board.TileTop(i)) - Flat(near)).sqrMagnitude;
            if (d < best)
            {
                best = d;
                spot = board.TileTop(i);
            }
        }
        return spot;
    }

    // Any land path between two tiles that stays off building ground (the start and end may be anywhere on land).
    List<Vector3> LandPath(int from, int to)
    {
        var cameFrom = new Dictionary<int, int> { { from, -1 } };
        var queue = new Queue<int>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == to)
                break;
            foreach (int next in LandNeighbours(current))
            {
                if (cameFrom.ContainsKey(next) || next != to && OnBuildingGround(board.TileTop(next), 0.3f))
                    continue;
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        var path = new List<Vector3>();
        if (!cameFrom.ContainsKey(to))
            return null;
        for (int t = to; t >= 0; t = cameFrom[t])
            path.Insert(0, board.TileTop(t));
        if (path.Count == 1)
            path.Add(path[0]);
        return path;
    }

    // Faces a parked tank toward the enemy capital.
    static Quaternion Facing(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        d.y = 0f;
        return d.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(d.normalized) : Quaternion.identity;
    }

    void ParkTanks(Deployment war)
    {
        Side enemy = Enemy(war.side);
        // The column's root first: the tanks are its children and would move with it.
        war.root.position = board.TileTop(war.tiles[0]);
        for (int i = 0; i < war.units.Length; i++)
        {
            Vector3 top = board.TileTop(war.tiles[Mathf.Min(i, war.tiles.Length - 1)]);
            war.units[i].position = top + Vector3.up * 0.02f;
            war.units[i].rotation = Facing(top, enemy.capital);
        }
    }

    // Drives a parked column, in single file over land, to new tiles.
    IEnumerator Redeploy(Deployment war, int[] tiles)
    {
        List<Vector3> path = LandPath(war.tiles[0], tiles[0]);
        if (path == null)
        {
            // No road there (it never drives over water): give the new tiles back and stay.
            foreach (int tile in tiles)
                if (System.Array.IndexOf(war.tiles, tile) < 0)
                    claimed.Remove(tile);
            yield break;
        }
        war.moving = true;
        foreach (int tile in war.tiles)
            claimed.Remove(tile);
        war.tiles = tiles;
        warSites.Remove(war.site);

        var along = new float[path.Count];
        for (int i = 1; i < path.Count; i++)
            along[i] = along[i - 1] + (Flat(path[i]) - Flat(path[i - 1])).magnitude;
        float length = Mathf.Max(0.001f, along[path.Count - 1]);
        const float Gap = 1.25f;
        float total = length + Gap * (war.units.Length - 1);
        float travelled = 0f;
        const float Speed = 1.6f;
        while (travelled < total && war.root != null)
        {
            travelled += WorldDelta * Speed;
            for (int i = 0; i < war.units.Length; i++)
            {
                float d = Mathf.Clamp(travelled - Gap * i, 0f, length);
                int k = 1;
                while (k < path.Count - 1 && along[k] < d)
                    k++;
                float span = Mathf.Max(0.001f, along[k] - along[k - 1]);
                Vector3 position = Vector3.Lerp(path[k - 1], path[k], Mathf.Clamp01((d - along[k - 1]) / span));
                Vector3 heading = path[k] - path[k - 1];
                heading.y = 0f;
                war.units[i].position = position + Vector3.up * 0.1f;
                if (heading.sqrMagnitude > 0.0001f)
                    war.units[i].rotation = Quaternion.Slerp(war.units[i].rotation, Quaternion.LookRotation(heading.normalized), WorldDelta * 6f);
            }
            yield return null;
        }

        if (war.root == null)
            yield break;
        ParkTanks(war);
        war.site = board.TileTop(tiles[0]);
        warSites.Add(war.site);
        war.moving = false;
    }

    // ---------------------------------------------------------------- Sea

    // How many tiles of water lie between each sea tile and the nearest land (1 = right on the coast).
    void EnsureSeaDistance()
    {
        EnsureSeaGrid();
        if (seaDistance != null)
            return;
        seaDistance = new Dictionary<int, int>();
        var queue = new Queue<int>();
        foreach (int sea in seaAt.Values)
        {
            foreach (Vector2Int key in AroundKey(SeaPoint(sea)))
            {
                if (tileAt.ContainsKey(key))
                {
                    seaDistance[sea] = 1;
                    queue.Enqueue(sea);
                    break;
                }
            }
        }
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (Vector2Int key in AroundKey(SeaPoint(current)))
            {
                if (seaAt.TryGetValue(key, out int next) && !seaDistance.ContainsKey(next))
                {
                    seaDistance[next] = seaDistance[current] + 1;
                    queue.Enqueue(next);
                }
            }
        }
    }

    int SeaDistance(int sea) => seaDistance != null && seaDistance.TryGetValue(sea, out int d) ? d : 99;

    // Whose coast a tile of open water lies off (one tile of water between them): 1 = red, 0 = blue, -1 = none.
    int OffshoreOwner(int sea)
    {
        if (SeaDistance(sea) != 2)
            return -1;
        foreach (Vector2Int key in AroundKey(SeaPoint(sea)))
        {
            if (seaAt.TryGetValue(key, out int coast) && SeaDistance(coast) == 1)
            {
                int owner = CoastOwner(coast);
                if (owner >= 0)
                    return owner;
            }
        }
        return -1;
    }

    // A free spot of open water off the enemy's coast, never next to land and never beside another ship.
    int PickOffshore(Side side)
    {
        int enemy = side.isRed ? 0 : 1;
        var options = new List<int>();
        foreach (int sea in seaAt.Values)
        {
            if (seaReserved.Contains(sea) || OffshoreOwner(sea) != enemy || OnBuildingGround(SeaPoint(sea), 1.5f))
                continue;
            options.Add(sea);
        }
        return options.Count == 0 ? -1 : options[UnityEngine.Random.Range(0, options.Count)];
    }

    void ReserveSea(int sea, bool reserve)
    {
        // The ship's own tile and two rings of water around it, so ships never overlap.
        var area = new HashSet<int> { sea };
        for (int ring = 0; ring < 2; ring++)
        {
            foreach (int tile in new List<int>(area))
                foreach (Vector2Int key in AroundKey(SeaPoint(tile)))
                    if (seaAt.TryGetValue(key, out int next))
                        area.Add(next);
        }
        foreach (int tile in area)
        {
            if (reserve) seaReserved.Add(tile);
            else seaReserved.Remove(tile);
        }
    }

    // A route over open water (never through the tile next to a coast) from one sea tile toward a goal; when
    // fromOwn is set it starts off the side's own coast at least a short voyage away.
    List<Vector3> OpenWaterRoute(int destination, int startOwner, int fromSea)
    {
        var cameFrom = new Dictionary<int, int> { { destination, -1 } };
        var steps = new Dictionary<int, int> { { destination, 0 } };
        var queue = new Queue<int>();
        queue.Enqueue(destination);
        int start = -1, staging = destination, stagingSteps = 0;
        while (queue.Count > 0 && start < 0)
        {
            int current = queue.Dequeue();
            foreach (Vector2Int key in AroundKey(SeaPoint(current)))
            {
                if (!seaAt.TryGetValue(key, out int next) || cameFrom.ContainsKey(next) || SeaDistance(next) < 2)
                    continue;
                cameFrom[next] = current;
                steps[next] = steps[current] + 1;
                if (fromSea >= 0 ? next == fromSea : OffshoreOwner(next) == startOwner && steps[next] >= 6)
                {
                    start = next;
                    break;
                }
                if (steps[next] <= 10 && steps[next] > stagingSteps)
                {
                    staging = next;
                    stagingSteps = steps[next];
                }
                queue.Enqueue(next);
            }
        }

        if (start < 0)
            start = fromSea >= 0 ? fromSea : staging;
        var route = new List<Vector3>();
        if (!cameFrom.ContainsKey(start))
        {
            route.Add(SeaPoint(start));
            route.Add(SeaPoint(destination));
            return route;
        }
        for (int t = start; t >= 0; t = cameFrom[t])
            route.Add(SeaPoint(t));
        if (route.Count == 1)
            route.Insert(0, route[0] + Vector3.back * 0.5f);
        return route;
    }

    // A parked warship lies along the coast, not pointing into it.
    Quaternion AlongCoast(int sea)
    {
        Vector3 p = SeaPoint(sea);
        Vector3 toLand = Vector3.zero;
        foreach (Vector2Int key in AroundKey(p))
            if (seaAt.TryGetValue(key, out int next) && SeaDistance(next) == 1)
                toLand += SeaPoint(next) - p;
        toLand.y = 0f;
        if (toLand.sqrMagnitude < 0.0001f)
            return Quaternion.identity;
        return Quaternion.LookRotation(Vector3.Cross(Vector3.up, toLand.normalized));
    }

    IEnumerator Resail(Deployment war, int destination)
    {
        war.moving = true;
        List<Vector3> route = OpenWaterRoute(destination, -1, war.sea);
        ReserveSea(war.sea, false);
        ReserveSea(destination, true);
        war.sea = destination;
        warSites.Remove(war.site);

        var along = new float[route.Count];
        for (int i = 1; i < route.Count; i++)
            along[i] = along[i - 1] + (Flat(route[i]) - Flat(route[i - 1])).magnitude;
        float length = Mathf.Max(0.001f, along[route.Count - 1]);
        float travelled = 0f;
        Transform ship = war.units[0];
        while (travelled < length && ship != null)
        {
            travelled += WorldDelta * 1.4f;
            int k = 1;
            while (k < route.Count - 1 && along[k] < travelled)
                k++;
            float span = Mathf.Max(0.001f, along[k] - along[k - 1]);
            Vector3 position = Vector3.Lerp(route[k - 1], route[k], Mathf.Clamp01((travelled - along[k - 1]) / span));
            position.y = 0.01f;
            ship.position = position;
            Vector3 heading = route[k] - route[k - 1];
            heading.y = 0f;
            if (heading.sqrMagnitude > 0.0001f)
                ship.rotation = Quaternion.Slerp(ship.rotation, Quaternion.LookRotation(heading.normalized), WorldDelta * 4f);
            yield return null;
        }

        if (ship == null)
            yield break;
        // The fleet's root first: the ship is its child and would move with it.
        war.root.position = SeaPoint(destination);
        ship.position = SeaPoint(destination) + Vector3.up * 0.01f;
        ship.rotation = AlongCoast(destination);
        war.site = SeaPoint(destination);
        warSites.Add(war.site);
        war.moving = false;
    }

    // ---------------------------------------------------------------- Keeping the board in step

    void UpdateUnits()
    {
        if (WorldTime < nextUnitCheck || country == null || LoseLockActive)
            return;
        nextUnitCheck = WorldTime + 0.5f;

        foreach (Side side in new[] { blue, red })
        {
            Party party = side.isRed ? country.Communism : country.Capitalism;
            RemoveLostWars(side, party.WarNumber);
            RemoveLostFactories(side, party.FactoriesNumber);
            RemoveLostSpies(side, party.SpiesNumber);
        }

        FollowTheFront();
    }

    static bool LoseLockActive => CountryManager.LoseLock;

    // Wars lost: the newest ones that have started leave the board, with an explosion where they stood.
    void RemoveLostWars(Side side, int wars)
    {
        int shown = 0;
        foreach (Deployment war in deployments)
            if (war.side == side && war.ready) shown++;
        for (int i = deployments.Count - 1; i >= 0 && shown > wars; i--)
        {
            Deployment war = deployments[i];
            if (war.side != side || !war.ready)
                continue;
            Vector3 at = war.root != null ? war.root.position : war.site;
            fx.Explosion(at + Vector3.up * 0.3f, 1.2f);
            warSites.Remove(war.site);
            if (war.naval)
            {
                ReserveSea(war.sea, false);
                side.fleets.Remove(war.root);
            }
            else
            {
                foreach (int tile in war.tiles)
                    claimed.Remove(tile);
                side.tankColumns.Remove(war.root);
            }
            if (war.root != null)
                Destroy(war.root.gameObject);
            deployments.RemoveAt(i);
            side.wars = Mathf.Max(0, side.wars - 1);
            shown--;
        }
    }

    // Factories destroyed: the newest ones go, with an explosion and smoke, and their ground is free again.
    void RemoveLostFactories(Side side, int factories)
    {
        while (side.factorySpots.Count > factories)
        {
            FactorySpot spot = side.factorySpots[side.factorySpots.Count - 1];
            side.factorySpots.RemoveAt(side.factorySpots.Count - 1);
            Vector3 top = board.TileTop(spot.tile);
            fx.Explosion(top + Vector3.up * 0.4f, 1.3f);
            fx.Smoke(top + Vector3.up * 0.2f, null, 4f, 0.6f, new Color(0.1f, 0.09f, 0.09f, 0.75f));
            claimed.Remove(spot.tile);
            footprints.Remove(new Rect(top.x - 0.7f, top.z - 0.7f, 1.4f, 1.4f));
            if (spot.factory != null)
            {
                int index = side.upgradable.IndexOf(spot.factory.transform);
                if (index >= 0)
                {
                    side.upgradable.RemoveAt(index);
                    side.upgradableScales.RemoveAt(index);
                }
                Destroy(spot.factory);
            }
            side.factories = Mathf.Max(0, side.factories - 1);
        }
    }

    // Spies caught: their drones are shot down.
    void RemoveLostSpies(Side side, int spies)
    {
        int shown = 0;
        foreach (Roamer roamer in roamers)
            if (roamer.side == side && roamer.transform != null) shown++;
        for (int i = roamers.Count - 1; i >= 0 && shown > spies; i--)
        {
            Roamer roamer = roamers[i];
            if (roamer.side != side || roamer.transform == null)
                continue;
            fx.Explosion(roamer.transform.position, 0.8f);
            Destroy(roamer.transform.gameObject);
            roamers.RemoveAt(i);
            side.spies = Mathf.Max(0, side.spies - 1);
            shown--;
        }
    }

    // When land changes hands, columns standing on land that is no longer the enemy's drive on to the enemy's
    // territory, and warships off a coast that changed hands sail to the enemy's coast.
    void FollowTheFront()
    {
        int moves = 0;
        foreach (Deployment war in deployments)
        {
            if (!war.ready || war.moving || war.root == null || moves >= 2)
                continue;
            if (war.naval)
            {
                int enemy = war.side.isRed ? 0 : 1;
                if (OffshoreOwner(war.sea) == enemy)
                    continue;
                int destination = PickOffshore(war.side);
                if (destination < 0)
                    continue;
                StartCoroutine(Resail(war, destination));
                moves++;
            }
            else
            {
                bool stillEnemy = true;
                foreach (int tile in war.tiles)
                    stillEnemy &= EnemyHolds(war.side, tile);
                if (stillEnemy)
                    continue;
                foreach (int tile in war.tiles)
                    claimed.Remove(tile);
                int[] tiles = ColumnTiles(war.side, war.units.Length, war.root.position, war.tiles[0]);
                foreach (int tile in war.tiles)
                    claimed.Add(tile);
                bool better = true;
                foreach (int tile in tiles)
                    better &= EnemyHolds(war.side, tile);
                // Nowhere better to go: stay put (already as close as it can get).
                if (!better && tiles[0] == war.tiles[0])
                {
                    foreach (int tile in tiles)
                        if (System.Array.IndexOf(war.tiles, tile) < 0)
                            claimed.Remove(tile);
                    continue;
                }
                StartCoroutine(Redeploy(war, tiles));
                moves++;
            }
        }
    }
}
