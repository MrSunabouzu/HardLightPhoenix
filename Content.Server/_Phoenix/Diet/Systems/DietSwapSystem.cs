using Content.Server._Phoenix.Diet.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Server._Phoenix.Diet.Systems;
//this is the _Funkystation stomach swap mutation with the serial numbers filed off. It's dirty but it should get the job done.
public sealed class DietSwapSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DietSwapComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DietSwapComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<DietSwapComponent> ent, ref ComponentStartup args)
    {
        var comp = ent.Comp;

        // Find current stomach
        if (!TryGetStomachOrgan(ent.Owner, out var originalStomachNullable) || originalStomachNullable is not { } originalStomach)
        {
            RemComp<DietSwapComponent>(ent.Owner);
            return;
        }

        comp.OriginalStomach = originalStomach;

        // Spawn new stomach
        var newStomach = Spawn(comp.NewStomachPrototype, Transform(ent.Owner).Coordinates);
        comp.SwappedStomach = newStomach;

        if (!TryGetStomachSlot(ent.Owner, out var stomachSlot) || stomachSlot is null)
        {
            Del(newStomach);
            RemComp<DietSwapComponent>(ent.Owner);
            return;
        }

        _container.Insert(newStomach, stomachSlot);
    }
        private void OnShutdown(Entity<DietSwapComponent> ent, ref ComponentShutdown args)
    {
        var comp = ent.Comp;

        if (comp.OriginalStomach is not { Valid: true } original ||
            comp.SwappedStomach is not { Valid: true } swapped)
            return;

        if (!TryGetStomachSlot(ent.Owner, out var stomachSlot) || stomachSlot is null)
            return;

        if (stomachSlot.ContainedEntity is { } current)
        {
            _container.Remove(current, stomachSlot);
            Del(current);
        }

        comp.OriginalStomach = null;
        comp.SwappedStomach = null;
    }
    private bool TryGetStomachOrgan(EntityUid body, out EntityUid? stomach)
    {
        stomach = null;

        foreach (var (organUid, _) in _body.GetBodyOrgans(body))
        {
            if (HasComp<StomachComponent>(organUid))
            {
                stomach = organUid;
                return true;
            }
        }

        return false;
    }
        private bool TryGetStomachSlot(EntityUid body, out ContainerSlot? slot)
    {
        slot = null;
        foreach (var part in _body.GetBodyChildren(body))
        {
            if (_container.TryGetContainer(part.Id, "body_organ_slot_stomach", out var container) && container is ContainerSlot organSlot)
            {
                slot = organSlot;
                return true;
            }
        }
        return false;
    }

}
