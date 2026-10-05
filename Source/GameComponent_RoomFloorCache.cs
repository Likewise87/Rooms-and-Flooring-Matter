using Verse;

namespace TSA.RoomsAndFlooringMatter
{
    /// <summary>
    /// Clears the static room floor/roof cache on new/load so stale room IDs do not carry across games.
    /// </summary>
    public class GameComponent_RoomFloorCache : GameComponent
    {
        // RimWorld FillComponents uses Activator.CreateInstance(type, game).
        public GameComponent_RoomFloorCache(Game game)
        {
        }

        public override void StartedNewGame() => RoomFloorCache.Clear();

        public override void LoadedGame() => RoomFloorCache.Clear();
    }
}
