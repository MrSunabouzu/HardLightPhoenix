namespace Content.Server._Phoenix.Diet.Components;

[RegisterComponent]
public sealed partial class DietSwapComponent : Component
{
    [DataField(required: true)]
    public string NewStomachPrototype = default!;

    public EntityUid? OriginalStomach { get; set; }
    public EntityUid? SwappedStomach { get; set; }
}
