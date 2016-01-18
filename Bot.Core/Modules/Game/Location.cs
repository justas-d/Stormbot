using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Game
{
    public class Location
    {
        public class NearbyLoc
        {
            private readonly HashSet<uint> _ids = new HashSet<uint>();
            private readonly Dictionary<uint, Location> _cachedLocations = new Dictionary<uint, Location>();

            public NearbyLoc(IEnumerable<uint> ids)
            {
                foreach (uint id in ids)
                    _ids.Add(id);
            }

            public IEnumerable<Location> All => _ids.Select(Get);

            public Location Get(uint id)
            {
                // we have a cached location
                if (_cachedLocations.ContainsKey(id))
                {
                    // made sure the key is equal to the id.
                    Location cache = _cachedLocations[id];
                    if (cache.Id != id)
                        _cachedLocations.Remove(id);
                    else
                        return cache;
                }

                // no location cached, find the location by id, cache and return it.
                Location newCache = Location.Get(id);
                _cachedLocations.Add(newCache.Id, newCache);
                return newCache;
            }
        }

        public const uint DefaultLocationId = 0;
        private static readonly Dictionary<uint, Location> Dict = new Dictionary<uint, Location>();

        public uint Id { get; }
        public string Name { get; }
        public string Description { get; }

        ///<summary>A list of location objects this location contains.</summary>
        public List<LocObject> Objects { get; }

        ///<summary>A list of locations that a user can enter while in this location.</summary>
        public NearbyLoc NearbyLocations { get; }

        private Location(uint id, string name, string description, IEnumerable<LocObject> objects,
            IEnumerable<uint> nearbyLocations)
        {
            try
            {
                Id = id;
                Name = name;
                Description = description;
                Objects = objects.ToList();

                //Popualte the nearby locations hashset.
                NearbyLocations = new NearbyLoc(nearbyLocations);

                Dict.Add(id, this);
            }
            catch (Exception ex)
            {
                Logger.FormattedWrite("Location", ex.ToString(), ConsoleColor.Red);
            }
        }

        static Location()
        {
            new Location(0, "Castle", "A big castle.", new[]
            {
                LocObject.Barrel,
                LocObject.Bank
            },
                new uint[] {1}); //square

            new Location(1, "Town Square", "The town square, filled with merchants and people.", new[]
            {
                LocObject.GenericTree,
            },
                new uint[] {0}); //castle
        }

        /// <summary> Attempts to make the given player enter this area. Default id: <see cref="DefaultLocationId"/>
        /// If a location with the given location id wasn't found, it will return the default location: <see cref="DefaultLocationId"/></summary>
        public static Location Get(uint id = DefaultLocationId)
            => Dict.TrySafeGet(id) ?? Dict.TrySafeGet(DefaultLocationId);

        /// <summary> Determines whether the given player can enter this location. </summary>
        public virtual bool CanEnter(GamePlayer player) => player.Location.Id != Id;

        /// <summary> Attempts to make the given player enter this area. </summary>
        public virtual bool Enter(GamePlayer player)
        {
            if (!CanEnter(player)) return false;

            player.Location = this;
            return true;
        }

        ///<summary>Returns a string representation of the nearby locations HashSet.</summary>
        public string ToStringNearby()
        {
            StringBuilder builder = new StringBuilder("`");

            foreach (Location loc in NearbyLocations.All)
                builder.AppendLine($"* {loc.Name,-15}");

            return $"{builder}`";
        }

        #region Overrides

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder($"{Description}\r\nObjects:\r\n");

            foreach (LocObject obj in Objects)
                builder.AppendLine($"* {obj.Name,-15} {obj.Description}");

            return builder.ToString();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (int) Id*13;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Location p = obj as Location;
            if ((object) p == null)
                return false;

            return Id == p.Id;
        }

        public bool Equals(Location p)
        {
            if ((object) p == null)
                return false;

            return Id == p.Id;
        }

        public static bool operator ==(Location a, Location b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (((object) a == null) || ((object) b == null))
                return false;

            return a.Id == b.Id;
        }

        public static bool operator !=(Location a, Location b)
        {
            return !(a == b);
        }

        #endregion
    }

    public class LocObject
    {
        public string Name { get; }
        public string Description { get; }
        public uint Id { get; }

        public LocObject(uint id, string name, string desc)
        {
            Id = id;
            Name = name;
            Description = desc;
        }

        /// <summary>Attempts to make the given player interact with this location object.</summary>
        public virtual async Task OnInteract(GamePlayer player)
        {
            await player.User.SendPrivate("Nothing interesting happens.");
        }

        #region Overrides

        public override int GetHashCode()
        {
            unchecked
            {
                return (int) Id*13;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            LocObject p = obj as LocObject;
            if ((object) p == null)
                return false;

            return Id == p.Id;
        }

        public bool Equals(LocObject p)
        {
            if ((object) p == null)
                return false;

            return Id == p.Id;
        }

        public static bool operator ==(LocObject a, LocObject b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (((object) a == null) || ((object) b == null))
                return false;

            return a.Id == b.Id;
        }

        public static bool operator !=(LocObject a, LocObject b)
        {
            return !(a == b);
        }

        #endregion

        #region Generic Objects

        public static LocObject Barrel => new LocObject(0, "Barrel", "A boring old barrel.");
        public static LocObject Bank => new BankObj(1, "Bank", "A bank. You can deposit items here.");
        public static LocObject GenericTree => new GenericTreeObj(2, "Tree", "A plain tree, blooming with leaves.");

        #endregion
    }

    public class GenericTreeObj : LocObject
    {
        public GenericTreeObj(uint id, string name, string desc) : base(id, name, desc)
        {
        }
    }

    public class BankObj : LocObject
    {
        public BankObj(uint id, string name, string desc) : base(id, name, desc)
        {
        }
    }
}