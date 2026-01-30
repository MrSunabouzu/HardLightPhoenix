using Robust.Shared.GameStates;

namespace Content.Shared._Phoenix.Traits.Diets;

/// <summary>
/// Checks the protoytype, grabs the stomach from there.
/// </summary>

[RegisterComponent]
public sealed partial class DietSwapComponent : Component
{
    [DataField(required: true)]
    public string NewStomachPrototype = "OrganHumanStomach";

    public EntityUid? OriginalStomach { get; set; }
    public EntityUid? SwappedStomach { get; set; }
}
