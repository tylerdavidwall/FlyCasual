﻿using Ship;
using Upgrade;
using UnityEngine;
using SubPhases;
using System;
using System.Collections.Generic;
using System.Linq;
using Conditions;

namespace UpgradesList.SecondEdition
{
    public class KyloRen : GenericUpgrade
    {
        public KyloRen() : base()
        {
            UpgradeInfo = new UpgradeCardInfo(
                "Kylo Ren",
                UpgradeType.Crew,
                cost: 9,
                isLimited: true,
                restriction: new FactionRestriction(Faction.FirstOrder),
                abilityType: typeof(Abilities.SecondEdition.KyloRenCrewAbility),
                addForce: 1
            );

            Avatar = new AvatarInfo(
                Faction.FirstOrder,
                new Vector2(286, 1)
            );

            ImageUrl = "https://squadbuilder.fantasyflightgames.com/card_images/en/f60322a1f5ace7e45f6c7e0fa0200705.png";
        }        
    }
}

namespace Abilities.SecondEdition
{
    public class KyloRenCrewAbility : GenericAbility
    {
        public GenericDamageCard AssignedDamageCard;
        public GenericShip ShipWithCondition;

        public override void ActivateAbility()
        {
            HostShip.OnGenerateActions += KyloRenCrewAddAction;
        }

        public override void DeactivateAbility()
        {
            HostShip.OnGenerateActions -= KyloRenCrewAddAction;
        }

        private void KyloRenCrewAddAction(GenericShip host)
        {
            ActionsList.GenericAction action = new ActionsList.KyloRenCrewAction()
            {
                ImageUrl = HostUpgrade.ImageUrl,
                HostShip = HostShip,
                DoAction = DoKyloRenAction
            };
            host.AddAvailableAction(action);
        }

        protected virtual bool IsActionAvailbale()
        {
            return HostShip.State.Force > 0;
        }

        private void DoKyloRenAction()
        {
            RegisterAbilityTrigger(TriggerTypes.OnAbilityDirect, SelectShip);

            Triggers.ResolveTriggers(TriggerTypes.OnAbilityDirect, Phases.CurrentSubPhase.CallBack);
        }

        private void SelectShip(object sender, EventArgs e)
        {
            // TODO: Skip/Wrong target - revert

            SelectTargetForAbility(
                AssignConditionToTarget,
                FilterTargets,
                GetAiPriority,
                HostShip.Owner.PlayerNo,
                HostUpgrade.UpgradeInfo.Name,
                "Choose a ship to assign\n\"I'll Show You The Dark Side\" Condition",
                HostUpgrade
            );
        }

        private void AssignConditionToTarget()
        {
            Sounds.PlayShipSound("Ill-Show-You-The-Dark-Side");

            if (AssignedDamageCard == null)
            {
                // If condition is not in play - select card to assign
                SelectShipSubPhase.FinishSelectionNoCallback();
                ShowPilotCrits();
            }
            else
            {
                // If condition is in play - reassing only
                RemoveConditions(ShipWithCondition);
                AssignConditions(TargetShip);
                SelectShipSubPhase.FinishSelection();
            }
        }

        private bool FilterTargets(GenericShip ship)
        {
            return FilterByTargetType(ship, new List<TargetTypes>() { TargetTypes.Enemy }) && FilterTargetsByRange(ship, 1, 3);
        }

        private int GetAiPriority(GenericShip ship)
        {
            return ship.PilotInfo.Cost + ship.UpgradeBar.GetUpgradesOnlyFaceup().Sum(n => n.UpgradeInfo.Cost);
        }

        private void ShowPilotCrits()
        {
            SelectPilotCritDecision selectPilotCritSubphase = (SelectPilotCritDecision)Phases.StartTemporarySubPhaseNew(
                "Select Damage Card",
                typeof(SelectPilotCritDecision),
                Triggers.FinishTrigger
            );

            List<GenericDamageCard> opponentDeck = DamageDecks.GetDamageDeck(Roster.AnotherPlayer(HostShip.Owner.PlayerNo)).Deck;
            foreach (var card in opponentDeck.Where(n => n.Type == CriticalCardType.Pilot))
            {
                Decision existingDecision = selectPilotCritSubphase.GetDecisions().Find(n => n.Name == card.Name);
                if (existingDecision == null)
                {
                    selectPilotCritSubphase.AddDecision(card.Name, delegate { SelectDamageCard(card); }, card.ImageUrl, 1);
                }
                else
                {
                    existingDecision.SetCount(existingDecision.Count + 1);
                }
            }

            selectPilotCritSubphase.DecisionViewType = DecisionViewTypes.ImagesDamageCard;

            selectPilotCritSubphase.DefaultDecisionName = selectPilotCritSubphase.GetDecisions().First().Name;

            selectPilotCritSubphase.DescriptionShort = "Kylo Ren";
            selectPilotCritSubphase.DescriptionLong = "Select a Damage Card to assign";
            selectPilotCritSubphase.ImageSource = HostUpgrade;

            selectPilotCritSubphase.RequiredPlayer = HostShip.Owner.PlayerNo;

            selectPilotCritSubphase.Start();
        }

        private void SelectDamageCard(GenericDamageCard damageCard)
        {
            DecisionSubPhase.ConfirmDecisionNoCallback();

            Messages.ShowInfo("Kylo Ren selected  " + damageCard.Name);

            AssignedDamageCard = damageCard;
            AssignedDamageCard.IsFaceup = true;
            DamageDeck opponentDamageDeck = DamageDecks.GetDamageDeck(TargetShip.Owner.PlayerNo);
            opponentDamageDeck.RemoveFromDamageDeck(damageCard);
            opponentDamageDeck.ReShuffleDeck();
            AssignConditions(TargetShip);

            SpendExtra(Triggers.FinishTrigger);
        }

        protected virtual void SpendExtra(Action callback)
        {
            HostShip.State.SpendForce(1, callback);
        }

        private void AssignConditions(GenericShip ship)
        {
            ShipWithCondition = ship;

            ship.Tokens.AssignCondition(typeof(IllShowYouTheDarkSide));
            ship.Tokens.AssignCondition(new IllShowYouTheDarkSideDamageCard(ship) { Tooltip = AssignedDamageCard.ImageUrl });

            ship.OnSufferCriticalDamage += SufferAssignedCardInstead;
            ship.OnShipIsDestroyed += RemoveConditionsOnDestroyed;
        }

        private void RemoveConditions(GenericShip ship)
        {
            ShipWithCondition = null;
            ship.Tokens.RemoveCondition(typeof(IllShowYouTheDarkSide));
            ship.Tokens.RemoveCondition(typeof(IllShowYouTheDarkSideDamageCard));

            ship.OnSufferCriticalDamage -= SufferAssignedCardInstead;
            ship.OnShipIsDestroyed -= RemoveConditionsOnDestroyed;
        }

        private void SufferAssignedCardInstead(object sender, EventArgs e, ref bool isSkipSufferDamage)
        {
            if ((e as DamageSourceEventArgs).DamageType == DamageTypes.ShipAttack)
            {

                isSkipSufferDamage = true;

                GenericShip ship = ShipWithCondition;
                Messages.ShowInfo("Kylo Ren's vison of the Dark Side came true: " + ship.PilotInfo.PilotName + " suffers " + AssignedDamageCard.Name);
                Combat.CurrentCriticalHitCard = AssignedDamageCard;

                AssignedDamageCard = null;
                RemoveConditions(ship);

                ship.ProcessDrawnDamageCard(e, Triggers.FinishTrigger);
            }
        }

        private void RemoveConditionsOnDestroyed(GenericShip ship, bool isFled)
        {
            AssignedDamageCard = null;
            RemoveConditions(ship);
        }

        private class SelectPilotCritDecision : DecisionSubPhase { };
    }
}

namespace ActionsList
{
    public class KyloRenCrewAction : GenericAction
    {
        public KyloRenCrewAction()
        {
            Name = DiceModificationName = "Kylo Ren: Assign condition";
        }

        public override int GetActionPriority()
        {
            return 0;
        }
    }
}