﻿using Ship;
using Upgrade;
using System.Collections.Generic;
using BoardTools;

namespace UpgradesList.SecondEdition
{
    public class Fearless : GenericUpgrade
    {
        public Fearless() : base()
        {
            UpgradeInfo = new UpgradeCardInfo(
                "Fearless",
                UpgradeType.Talent,
                cost: 3,
                abilityType: typeof(Abilities.SecondEdition.FearlessAbility),
                restriction: new FactionRestriction(Faction.Scum),
                seImageNumber: 6
            );
        }        
    }
}

namespace Abilities.SecondEdition
{
    public class FearlessAbility : Abilities.FirstEdition.FearlessnessAbility
    {
        protected override void FearlessnessAddDiceModification(GenericShip host)
        {
            ActionsList.GenericAction newAction = new ActionsList.FearlessAction()
            {
                ImageUrl = HostUpgrade.ImageUrl,
                HostShip = HostShip
            };
            HostShip.AddAvailableDiceModificationOwn(newAction);
        }
    }
}

namespace ActionsList
{
    public class FearlessAction : GenericAction
    {
        public FearlessAction()
        {
            Name = DiceModificationName = "Fearless";
        }

        public override bool IsDiceModificationAvailable()
        {
            bool result = true;

            if (Combat.AttackStep != CombatStep.Attack) return false;

            if (Combat.ChosenWeapon.WeaponType != WeaponTypes.PrimaryWeapon) return false;

            if (!Combat.ChosenWeapon.WeaponInfo.ArcRestrictions.Contains(Arcs.ArcType.Front)) return false;

            if (Combat.ShotInfo.Range != 1) return false;

            if (!Combat.Defender.SectorsInfo.IsShipInSector(Combat.Attacker, Arcs.ArcType.Front)) return false;

            return result;
        }

        public override int GetDiceModificationPriority()
        {
            if (Combat.DiceRollAttack.WorstResult == DieSide.Blank || Combat.DiceRollAttack.WorstResult == DieSide.Focus) return 100;
            return 0;
        }

        public override void ActionEffect(System.Action callBack)
        {
            Combat.DiceRollAttack.ChangeOne(Combat.DiceRollAttack.WorstResult, DieSide.Success);
            callBack();
        }
    }
}

namespace Abilities.FirstEdition
{
    public class FearlessnessAbility : GenericAbility
    {

        public override void ActivateAbility()
        {
            HostShip.OnGenerateDiceModifications += FearlessnessAddDiceModification;
        }

        public override void DeactivateAbility()
        {
            HostShip.OnGenerateDiceModifications -= FearlessnessAddDiceModification;
        }

        protected virtual void FearlessnessAddDiceModification(GenericShip host)
        {
            ActionsList.GenericAction newAction = new ActionsList.FearlessnessAction()
            {
                ImageUrl = HostUpgrade.ImageUrl,
                HostShip = HostShip
            };
            HostShip.AddAvailableDiceModificationOwn(newAction);
        }

    }
}

namespace ActionsList
{

    public class FearlessnessAction : GenericAction
    {

        public FearlessnessAction()
        {
            Name = DiceModificationName = "Fearlessness";
        }

        public override bool IsDiceModificationAvailable()
        {
            bool result = true;

            if (Combat.AttackStep != CombatStep.Attack) return false;

            if (!Combat.ShotInfo.InArc) return false;

            ShotInfo reverseShotInfo = new ShotInfo(Combat.Defender, Combat.Attacker, Combat.Defender.PrimaryWeapons);
            if (!reverseShotInfo.InArc || reverseShotInfo.Range != 1) return false;

            return result;
        }

        public override int GetDiceModificationPriority()
        {
            return 110;
        }

        public override void ActionEffect(System.Action callBack)
        {
            Combat.CurrentDiceRoll.AddDiceAndShow(DieSide.Success);
            callBack();
        }

    }

}