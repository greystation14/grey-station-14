using Content.Shared.Body.Systems;
using Content.Shared.Charges.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.SyndicateTeleporter;

public sealed class SyndicateTeleporterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public EntProtoId TeleportEffectPrototype = "TeleportEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicateTeleporterComponent, UseInHandEvent>(OnUse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<SyndicateTeleporterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.InWall)
                continue;

            var nextSave = comp.LastSave + comp.SaveDelay;
            if (now >= nextSave)
            {
                comp.LastSave = now;
                SaveTeleport((uid, comp));
            }
        }
    }

    private void OnUse(Entity<SyndicateTeleporterComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_charges.TryUseCharge(ent.Owner))
            return;

        Teleport(ent, args.User);
        args.Handled = true;
    }

    private void Teleport(Entity<SyndicateTeleporterComponent> ent, EntityUid user)
    {
        var (uid, comp) = ent;
        comp.User = user;

        var distance = comp.TeleportationValue + _random.Next(0, comp.RandomDistanceValue);
        var multiplier = new Vector2(distance, distance);

        var xform = Transform(user);
        var offsetValue = xform.LocalRotation.ToWorldVec().Normalized() * multiplier;
        var coords = xform.Coordinates.Offset(offsetValue); //set coordinates where we move on

        if (xform.GridUid is not {} gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        _audio.PlayPvs(comp.TeleportSound, user);

        Spawn(TeleportEffectPrototype, xform.Coordinates);

        _transform.SetCoordinates((user, xform, MetaData(user)), coords); // teleport

        Spawn(TeleportEffectPrototype, coords);

        var tilePos = _map.TileIndicesFor(gridUid, grid, coords);
        comp.InWall = _turf.IsTileBlocked(gridUid, tilePos, CollisionGroup.Impassable, grid);
    }

    private void SaveTeleport(Entity<SyndicateTeleporterComponent> ent)
    {
        var (uid, comp) = ent;
        if (comp.User is not {} user)
            return;

        comp.User = null;
        var xform = Transform(user);
        var offsetValue = xform.LocalPosition;
        var coords = xform.Coordinates.WithPosition(offsetValue);

        if (xform.GridUid is not {} gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var saveattempts = comp.SaveAttempts;
        var savedistance = comp.SaveDistance;

        var tilePos = _map.TileIndicesFor(gridUid, grid, coords);
        while (_turf.IsTileBlocked(gridUid, tilePos, CollisionGroup.Impassable, grid))
        {
            EntityUid? tuser = null;

            if (saveattempts > 0) // if we have chance to survive then teleport in random side away
            {
                double side = _random.Next(-180, 180);
                offsetValue = Angle.FromDegrees(side).ToWorldVec() * savedistance; //averages the resulting direction, turning it into one of 8 directions, (N, NE, E...)
                coords = xform.Coordinates.Offset(offsetValue);
                _transform.SetCoordinates(user, coords);

                Spawn(TeleportEffectPrototype, coords);
                _audio.PlayPredicted(comp.AlarmSound, uid, tuser);

                saveattempts--;
            }
            else
            {
                _body.GibBody(user, true);
                comp.InWall = false; // closing the countdown in update
                break;
            }

            tilePos = _map.TileIndicesFor(gridUid, grid, coords);
            if (!_turf.IsTileBlocked(gridUid, tilePos, CollisionGroup.Impassable, grid))
            {
                comp.InWall = false;
                return;
            }
        }
    }
}
