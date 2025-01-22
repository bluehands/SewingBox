using FunicularSwitch;
using Microsoft.Extensions.Logging;
using SaveThePrincess.Adventure.Entities;
using SaveThePrincess.Adventure.FairyTales;

namespace SaveThePrincess.Adventure;

internal class LullabyFairyTaleImpl(ILogger logger) : LullabyFairyTale(logger)
{
    /**
     * Use the methods in the LullabyFairyTale class to implements this method.
     * The methods are all friendly and should never throw exceptions.
     * It will be an easy task to be a hero.
     */
    public Result<FairyTaleResult> TellStory()
    {
        // TODO: Looking for a hero which is brave enough for this adventure
        // TODO: Travel with your hero to the castle far away
        // TODO: Enter the castle and looking for enemies
        // TODO: Defeat the enemies and collect the loot
        // TODO: After all enemies are defeated free the princess
        // TODO: Travel home with the princess and the collected loot
        throw new NotImplementedException();
    }
}