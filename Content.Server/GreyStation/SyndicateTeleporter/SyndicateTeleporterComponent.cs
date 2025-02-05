using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SyndicateTeleporter;

[RegisterComponent, Access(typeof(SyndicateTeleporterSystem))]
[AutoGenerateComponentPause]
public sealed partial class SyndicateTeleporterComponent : Component
{
    /// <summary>
    /// adds a random value to which you teleport, which is added to the guaranteed teleport value. from 0 to the set number. set 0 if you don't need randomness when teleporting
    /// </summary>
    [DataField]
    public int RandomDistanceValue = 4;

    /// <summary>
    /// this is the guaranteed number of tiles that you teleport to.
    /// </summary>
    [DataField]
    public float TeleportationValue = 4f;

    /// <summary>
    /// How many attempts do you have to teleport into the wall without a fatal outcome
    /// </summary>
    [DataField]
    public int SaveAttempts = 1;

    /// <summary>
    /// The distance to which you will be teleported when you teleport into a wall
    /// </summary>
    [DataField]
    public int SaveDistance = 3;

    [DataField]
    public SoundSpecifier? TeleportSound;

    [DataField]
    public SoundSpecifier? AlarmSound = new SoundPathSpecifier("/Audio/GreyStation/Effects/beeps.ogg");

    /// <summary>
    /// The last user to activate this teleporter
    /// Used when saving them after being stuck in a wall.
    /// </summary>
    [DataField]
    public EntityUid? User;

    /// <summary>
    /// The number of seconds the player stays in the wall. (just so that he would realize that he almost died)
    /// </summary>
    [DataField]
    public TimeSpan SaveDelay = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// When the user was last saved from a wall.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastSave = TimeSpan.Zero;

    [DataField]
    public bool InWall;
}
