using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.TheQueen;

/// <summary>
/// 入场券：所选牌由打出者手牌经 <see cref="CardModel.Owner"/> 转移给女王（原版 <see cref="CardModel.Owner"/> 不允许直接改换主人，须先置 <c>null</c> 再赋女王）。
/// 联机选牌由 <see cref="CardSelectCmd.FromHand"/> 同步，各端对同一 <c>selected</c> 执行相同转移。
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class AdmissionTicket : QueenCardModel
{
	private const int energyCost = 0;
	private const CardType type = CardType.Skill;
	private const CardRarity rarity = CardRarity.Common;
	private const TargetType targetType = TargetType.Self;
	private const bool shouldShowInCardLibrary = false;

	private ulong _queenGiverNetId;

	private static readonly string[] GiftNotifyQueenLocKeys =
	[
		"QUEEN.ADMISSION_TICKET.giftNotifyQueen_1",
		"QUEEN.ADMISSION_TICKET.giftNotifyQueen_2",
		"QUEEN.ADMISSION_TICKET.giftNotifyQueen_3",
		"QUEEN.ADMISSION_TICKET.giftNotifyQueen_4",
		"QUEEN.ADMISSION_TICKET.giftNotifyQueen_5",
		"QUEEN.ADMISSION_TICKET.giftNotifyQueen_6",
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StringVar("Applier"),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public AdmissionTicket()
		: base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
	{
	}

	internal void SetGiftFromQueen(Player queen)
	{
		_queenGiverNetId = queen.NetId;
		((StringVar)base.DynamicVars["Applier"]).StringValue = PlatformUtil.GetPlayerName(
			RunManager.Instance.NetService.Platform,
			queen.NetId);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		_ = cardPlay;
		if (base.Owner.Creature.CombatState is not { } combatState)
		{
			return;
		}

		Player? queen = combatState.RunState.GetPlayer(_queenGiverNetId);
		if (queen == null || !queen.Creature.IsAlive)
		{
			return;
		}

		// source 不能传 this：否则会推迟到本牌 ExecutionFinished 才 OnSelectModeSourceFinished，
		// 把选中牌的 NCard 又 Add 回递交者手牌，而模型已转给女王 → 幽灵牌、无法打出。
		CardModel? selected = (await CardSelectCmd.FromHand(
			choiceContext,
			base.Owner,
			new CardSelectorPrefs(new LocString("cards", "STS2_COMICCHESS_THEQUEEN_CARD_ADMISSION_TICKET.selectionPrompt"), 1),
			static _ => true,
			this
		)).FirstOrDefault();

		if (selected == null)
		{
			return;
		}

		selected.AssertMutable();
		string cardTitleFormatted = selected.Title;
		string giverName = PlatformUtil.GetPlayerName(
			RunManager.Instance.NetService.Platform,
			base.Owner.NetId);

		// CreateClone 的实例往往已带 Owner，不能再赋女王；改为转移同一张牌：先离手，再 Owner=null → 女王，再入手牌。
		selected.RemoveFromCurrentPile();
		selected.Owner = null!; // 原版 setter 仅允许 null 作为中间态以更换 Owner
		selected.Owner = queen;
		await CardPileCmd.Add(selected, PileType.Hand);

		// 不要用 CombatCardSelection：选牌/奖励等会大量消耗该流，联机各端计数易与「递交」不同步或产生偏相关。
		// Niche 为独立计数器，Run 存档里单独持久化，各端仍一致。
		string notifyKey = queen.RunState.Rng.Niche.NextItem(GiftNotifyQueenLocKeys)
			?? GiftNotifyQueenLocKeys[0];
		LocString notify = new LocString("monsters", notifyKey);
		notify.Add("Giver", giverName);
		notify.Add("CardTitle", cardTitleFormatted);
		ThinkCmd.Play(notify, queen.Creature, 2.5);
	}

	protected override void OnUpgrade()
	{
		base.AddKeyword(CardKeyword.Retain);
	}
}
