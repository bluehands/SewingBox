using FunicularSwitch;
using SaveThePrincess.Adventure.Entities;

namespace SaveThePrincess.Adventure.FairyTales;

internal class LullabyFairyTaleExample : LullabyFairyTale
{
    public Result<FairyTaleResult> TellStory() =>
        from hero in CallForAHero()
        from castle in TravelToCastle(hero)
        from enemies in EnterCastle(hero, castle)
        from loot in DefeatEnemies(hero, enemies)
        from princess in FreeThePrincess(hero, castle)
        from fine in TravelingHome(hero, princess, loot)
        select fine;
}