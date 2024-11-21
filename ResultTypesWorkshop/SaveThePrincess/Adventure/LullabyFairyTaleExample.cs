using FunicularSwitch;
using Microsoft.Extensions.Logging;
using SaveThePrincess.Adventure.Entities;
using SaveThePrincess.Adventure.FairyTales;

namespace SaveThePrincess.Adventure;

internal class LullabyFairyTaleExample(ILogger logger) : LullabyFairyTale(logger)
{
    /**
     * Use the methods in the LullabyFairyTale class to implements this method.
     * The methods are all friendly and should never throw exceptions.
     * It will be an easy task to be a hero.
     */
    public Result<FairyTaleResult> TellStory() =>
        from hero in CallForAHero()
        let castle = TravelToCastle(hero)
        from enemies in EnterCastle(hero, castle)
        from loot in DefeatEnemies(hero, enemies)
        from princess in FreeThePrincess(hero, castle)
        from fine in TravelingHome(hero, princess, loot)
        select fine;
}