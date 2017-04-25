﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using CoreAPI = Hearthstone_Deck_Tracker.API.Core;
using System.Collections.ObjectModel;
using HearthDb.Enums;
using System.ComponentModel;
using System.Windows;
using System.Text.RegularExpressions;
using HDT.Plugins.Custom.Models;
using HDT.Plugins.Custom.ViewModels;
using Hearthstone_Deck_Tracker.Utility.Logging;

namespace HDT.Plugins.Custom
{
    public class MetaDataController
    {
        #region Properties and Variables

        internal MetaDataView _dispView;
        internal WindowViewModel MainWindowViewModel { get; set; }
        CardInfoViewModel CardInfoVM = new CardInfoViewModel();

        IEnumerable<Entity> EntitiesInHand
        {
            get
            {
                var eih = from e in CoreAPI.Game.Player.Hand where e.Info.CardMark != CardMark.Coin orderby e.GetTag(GameTag.ZONE_POSITION) select e;

                if (eih.All(e => e.HasTag(GameTag.ZONE_POSITION)))
                    eih = eih.OrderBy(e => e.GetTag(GameTag.ZONE_POSITION));

                return eih;
            }
        }

        enum ComparisonType { LessThan, LessThanEqual, Equal, GreaterThanEqual, GreaterThan };

        int DeckCardCount => CoreAPI.Game.Player.PlayerCardList.Select(c => c.Count).Sum();

        bool ShouldHide { get { return Config.Instance.HideInMenu && CoreAPI.Game.IsInMenu; } }

        List<Card> PlayerCardList => CoreAPI.Game.Player.PlayerCardList;

        IDictionary<int, int> DeckCardCountByCost
        {
            get
            {
                var ccbc = new SortedDictionary<int, int>();

                foreach (Card c in PlayerCardList)
                {
                    if (c.Count == 0)
                        continue;

                    if (ccbc.ContainsKey(c.Cost))
                        ccbc[c.Cost] += c.Count;
                    else
                        ccbc.Add(c.Cost, c.Count);
                }

                return ccbc;
            }
        }

        #endregion 

        public MetaDataController(MetaDataView displayView)
        {
            _dispView = displayView;
            MainWindowViewModel = new WindowViewModel();
            _dispView.DataContext = MainWindowViewModel;

            if (ShouldHide)
                _dispView?.Hide();
            else
                _dispView?.Show();
        }



        public void GameStart()
        {
            _dispView?.Show();
            UpdateCardInformation();
        }

        internal void TurnStart(ActivePlayer player)
        {
            UpdateCardInformation();
        }

        internal void Update()
        {
            UpdateCardInformation();
        }

        internal void PlayerDraw(Card c)
        {
            UpdateCardInformation();
        }

        internal void GameEnd()
        {
            _dispView?.Hide();
        }

        internal void PlayerMulligan(Card c)
        {
            UpdateCardInformation();
        }

        internal void PlayerPlay(Card c)
        {

        }

        internal void OpponentPlay(Card c)
        {

        }

        Entity _localPlayerQuestEntity { get; set; }
        Entity _opponentQuestEntity { get; set; }

        internal void CheckForAndUpdateQuest()
        {
            if (_localPlayerQuestEntity == null)
                _localPlayerQuestEntity = CoreAPI.Game.Player.RevealedEntities.Where(e => e.IsQuest && e.IsInZone(Zone.SECRET)).FirstOrDefault();

            if (_opponentQuestEntity == null)
                _opponentQuestEntity = CoreAPI.Game.Opponent.RevealedEntities.Where(e => e.IsQuest && e.IsInZone(Zone.SECRET)).FirstOrDefault();

            if (_localPlayerQuestEntity != null)
            {
                var e = _localPlayerQuestEntity;
                MainWindowViewModel.LocalPlayerQuestProgress.Set(e.LocalizedName, e.GetTag(GameTag.QUEST_PROGRESS), e.GetTag(GameTag.QUEST_PROGRESS_TOTAL));
            }

            if (_opponentQuestEntity != null)
            {
                var e = _opponentQuestEntity;
                MainWindowViewModel.OpponentQuestProgress.Set(e.LocalizedName, e.GetTag(GameTag.QUEST_PROGRESS), e.GetTag(GameTag.QUEST_PROGRESS_TOTAL));
            }
        }

        private void UpdateCardMetaData()
        {
            int spellCount = 0;
            int mininionCount = 0;

            foreach (Card c in CoreAPI.Game.Player.PlayerCardList)
            {
                if (c.Type == "Spell")
                {
                    spellCount += c.Count;
                }

                if (c.Type == "Minion")
                {
                    mininionCount += c.Count;
                }
            }

            var spellModel = new CardTypeCountModel("Spells", spellCount, DeckCardCount);
            var minionModel = new CardTypeCountModel("Minions", mininionCount, DeckCardCount);

            MainWindowViewModel.CardTypeCount.Add(new ViewModels.CardTypeCountViewModel(spellModel));
            MainWindowViewModel.CardTypeCount.Add(new ViewModels.CardTypeCountViewModel(minionModel));
        }

     

        public void UpdateCardInformation()
        {
            MainWindowViewModel.Clear();
            CardInfoVM.CardInfo.Clear();

            double runningTotal = 0;
            foreach (KeyValuePair<int, int> kv in DeckCardCountByCost)
            {
                var equalOdds = DeckCostStats(kv.Key, ComparisonType.Equal, false) / DeckCardCount;
                runningTotal += equalOdds;

                var cm = new CardInfoModel(kv.Key, Helpers.ToPercentString(equalOdds), Helpers.ToPercentString(runningTotal));
                CardInfoVM.CardInfo.Add(cm);

            }

            if (CoreAPI.Game.IsMulliganDone == false)
            {
                UpdateMulliganData();
            }

            UpdateCardMetaData();
            UpdateHandDamageCounter();
            CheckForAndUpdateQuest();
        }

        void UpdateMulliganData()
        {
            foreach (Entity e in EntitiesInHand)
            {
                var c = e.Card;

                int cardsMulliganed = 1;
                var cardsAfterReshuffle = ((double)DeckCardCount + cardsMulliganed);

                var lowerOdds = DeckCostStats(c.Cost, ComparisonType.LessThan) / cardsAfterReshuffle;
                var equalOdds = DeckCostStats(c.Cost, ComparisonType.Equal, true) / cardsAfterReshuffle;
                var higherOdds = DeckCostStats(c.Cost, ComparisonType.GreaterThan) / cardsAfterReshuffle;
                var lowerEqualOdds = DeckCostStats(c.Cost, ComparisonType.LessThanEqual) / cardsAfterReshuffle;
                var higherEqualOdds = DeckCostStats(c.Cost, ComparisonType.GreaterThanEqual) / cardsAfterReshuffle;

                var mom = new MulliganOddsModel(lowerOdds, equalOdds, higherOdds);
                MainWindowViewModel.MulliganCardOdds.Add(new ViewModels.MulliganOddsViewModel(mom));
            }
        }

        void UpdateHandDamageCounter()
        {
            if (CoreAPI.Game.Player.IsLocalPlayer == false)
                return;

            int sp = 0;
            foreach (Entity e in CoreAPI.Game.Player.Board)
            {
                if (e.HasTag(GameTag.SPELLPOWER) == false)
                    continue;

                sp += e.GetTag(GameTag.SPELLPOWER);
            }

            int primaryDamage = 0;
            int secondaryDamage = 0;
            int cardCount = 0;
            foreach (Entity e in EntitiesInHand)
            {
                if (_damageCards.Contains(e.CardId) == false)
                    continue;

                var c = e.Card;
                var spellDamageInfo = GetCardDirectDamage(e);

                if (spellDamageInfo == null)
                    continue;
                cardCount++;
                primaryDamage += spellDamageInfo.PrimaryDamage + sp;
                secondaryDamage += spellDamageInfo.SecondaryDamage + sp;
            }

            MainWindowViewModel.ExtraInfo.Add("Spell Dmg: " + primaryDamage + " (" + secondaryDamage + ") (C" + cardCount + ")");
        }

        static string[] _damageCards = new string[]
        {
            HearthDb.CardIds.Collectible.Rogue.SinisterStrike,
            HearthDb.CardIds.Collectible.Rogue.Eviscerate,
            HearthDb.CardIds.Collectible.Rogue.JadeShuriken,
            HearthDb.CardIds.Collectible.Rogue.Shiv
        };

        class SpellDamageInfo
        {

            public int PrimaryDamage { get; set; }
            public int SecondaryDamage { get; set; }

            public SpellDamageInfo(int damage, int altDamage)
            {
                PrimaryDamage = damage;
                SecondaryDamage = altDamage;
            }

            public SpellDamageInfo(int damage)
            {
                PrimaryDamage = SecondaryDamage = damage;
            }
        }

        SpellDamageInfo GetCardDirectDamage(Entity e)
        {
            if (e.CardId == HearthDb.CardIds.Collectible.Rogue.SinisterStrike)
                return new SpellDamageInfo(3, 3);
            else if (e.CardId == HearthDb.CardIds.Collectible.Rogue.Eviscerate)
                return new SpellDamageInfo(2, 4);
            else if (e.CardId == HearthDb.CardIds.Collectible.Rogue.JadeShuriken)
                return new SpellDamageInfo(2);
            else if (e.CardId == HearthDb.CardIds.Collectible.Rogue.Shiv)
                return new SpellDamageInfo(1);
            else if (e.CardId == HearthDb.CardIds.Collectible.Mage.Fireball)
                return new SpellDamageInfo(6);
            else if (e.CardId == HearthDb.CardIds.Collectible.Mage.Pyroblast)
                return new SpellDamageInfo(10);
            else if (e.CardId == HearthDb.CardIds.Collectible.Mage.Frostbolt)
                return new SpellDamageInfo(3);
            return null;
        }

        private double DeckCostStats(int cost, ComparisonType comparisonType, bool countAsAddedBack = false)
        {
            double retValue = 0;

            if (comparisonType == ComparisonType.LessThan)
                retValue = (from d in DeckCardCountByCost where d.Key < cost select d.Value).Sum();
            else if (comparisonType == ComparisonType.GreaterThan)
                retValue = (from d in DeckCardCountByCost where d.Key > cost select d.Value).Sum();
            else if (comparisonType == ComparisonType.Equal)
                retValue = (from d in DeckCardCountByCost where d.Key == cost select d.Value).Sum() + (countAsAddedBack ? 1 : 0);
            else if (comparisonType == ComparisonType.LessThanEqual)
                retValue = (from d in DeckCardCountByCost where d.Key <= cost select d.Value).Sum();
            else if (comparisonType == ComparisonType.GreaterThanEqual)
                retValue = (from d in DeckCardCountByCost where d.Key >= cost select d.Value).Sum();
            else if (comparisonType == ComparisonType.GreaterThan)
                retValue = (from d in DeckCardCountByCost where d.Key < cost select d.Value).Sum();


            return retValue;
        }

    }
}
