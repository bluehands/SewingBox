using FunicularSwitch;
using Microsoft.Extensions.Logging;
using SaveThePrincess.Adventure.Entities;
using SaveThePrincess.Adventure.FairyTales;

namespace SaveThePrincess.Adventure;

internal class NightmareFairyTaleImpl(ILogger logger) : NightmareFairyTale(logger)
{
    /**
     * Use the methods in the NightmareFairyTale class to implements this method.
     * The methods are all very evil and should always throw exceptions.
     * It will be a difficult road to be a hero.
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