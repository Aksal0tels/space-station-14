using System.Text;
using Content.Server.Administration;
using Content.Server.Communications;
using Content.Shared.Administration;
using Content.Shared.Communications;
using Content.Shared.SpaceStories.ForceUser;
using JetBrains.Annotations;
using Robust.Server;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Server.Stories.Sponsor.AntagSelect;
using Content.Shared.Stories.Sponsor.AntagSelect;
using Content.Server.Database;

namespace Content.Server.Stories.Sponsor.Commands;

[UsedImplicitly, AnyCommand]
public sealed class PickAntagCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPartnersManager _db = default!;
    public string Command => "pickantag";
    public string Description => "Выдает роль.";
    public string Help => "Usage: pickantag dragon";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var uiSystem = _entityManager.System<UserInterfaceSystem>();
        var antagSelectSystem = _entityManager.System<AntagSelectSystem>();

        if (shell.Player == null || shell.Player.AttachedEntity == null)
            return;

        var antag = args[0];
        var playerEntity = shell.Player.AttachedEntity.Value;

        if (!_proto.TryIndex<SponsorAntagPrototype>(antag, out var proto))
            return;

        var ev = new CanPickAttemptEvent(playerEntity, shell.Player, proto);
        _entityManager.EventBus.RaiseLocalEvent(playerEntity, (object) ev, true);

        if (ev.Cancelled)
            return;

        if (proto.Event == null)
            return;

        if (uiSystem.TryGetUi(playerEntity, AntagSelectUiKey.Key, out var ui))
            uiSystem.CloseUi(ui, shell.Player);

        var ev1 = proto.Event;
        ev1.EntityUid = playerEntity;
        _entityManager.EventBus.RaiseLocalEvent(playerEntity, (object) ev1, true);

        if (ev1.RoleTaken)
        {
            if (!antagSelectSystem.IssuedSponsorRoles.TryAdd(antag, 1))
                antagSelectSystem.IssuedSponsorRoles[antag] += 1;

            _db.SetAntagPicked(shell.Player.UserId);
        }
    }
}

