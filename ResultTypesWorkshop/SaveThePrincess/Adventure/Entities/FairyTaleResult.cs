using FunicularSwitch;

namespace SaveThePrincess.Adventure.Entities;

internal record FairyTaleResult(Hero Hero, Option<Princess> Princesses, IReadOnlyCollection<Loot> Loot);