using System.Collections.Immutable;
using FunicularSwitch;

namespace SaveThePrincess.Adventure.Entities
{
    internal class Castle
    {
        readonly ImmutableList<Enemy> _enemies;

        public ImmutableList<Enemy> Enemies => _enemies.Where(e => e.IsAlive).ToImmutableList();
        public Option<Princess> Princess { get; }

        public Castle(ImmutableList<Enemy> enemies, Option<Princess> princess)
        {
            _enemies = enemies;
            Princess = princess;
        }
        public bool HasEnemies => Enemies.Count != 0;
    }
}
