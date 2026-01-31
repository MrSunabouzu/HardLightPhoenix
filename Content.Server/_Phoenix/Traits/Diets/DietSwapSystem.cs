using Content.Shared._Phoenix.Traits.Diets;
using Content.Server._Funkystation.Genetics.Mutations.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;

using Robust.Shared.Containers;

namespace Content.Server._phoenix.Traits.Diets;

//this is the _Funkystation stomach swap mutation with the serial numbers filed off. It's dirty but it should get the job done.
public sealed class DietSwapSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    private const string HiddenStorageContainerId = "Trait_hidden_stomach_storage";
   public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DietSwapComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<DietSwapComponent> ent, ref ComponentStartup args)
    {
        var comp = ent.Comp;
        var hiddenContainer = _container.EnsureContainer<ContainerSlot>(ent.Owner, HiddenStorageContainerId);

        // Find current stomach
        if (!TryGetStomachOrgan(ent.Owner, out var originalStomachNullable) || originalStomachNullable is not { } originalStomach)
        {
            RemComp<DietSwapComponent>(ent.Owner);
            return;
        }

        comp.OriginalStomach = originalStomach;
        _container.Insert(originalStomach, hiddenContainer);

        // Spawn new stomach
        var newStomach = Spawn(comp.NewStomachPrototype, Transform(ent.Owner).Coordinates);
        comp.SwappedStomach = newStomach;

        // Transfer solutions (stomach, food, organ)
        TransferOrganSolutions(originalStomach, newStomach);

        if (!TryGetStomachSlot(ent.Owner, out var stomachSlot) || stomachSlot is null)
        {
            Del(newStomach);
            RemComp<DietSwapComponent>(ent.Owner);
            return;
        }

        _container.Insert(newStomach, stomachSlot);
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

    private void TransferOrganSolutions(EntityUid from, EntityUid to)
    {
        var solutionNames = new[] { "stomach", "food", "organ" };

        foreach (var name in solutionNames)
        {
            // Get source solution
            if (!_solution.TryGetSolution(from, name, out var fromSolEntNullable, out var fromSol) ||
                fromSolEntNullable is not { } fromSolEnt)
                continue;

            // Get target solution
            if (!_solution.TryGetSolution(to, name, out var toSolEntNullable, out var toSol) ||
                toSolEntNullable is not { } toSolEnt)
                continue;

            if (fromSol.Volume > 0)
            {
                var drained = _solution.SplitSolution(fromSolEnt, fromSol.Volume);
                _solution.TryAddSolution(toSolEnt, drained);
            }
        }
    }
}
