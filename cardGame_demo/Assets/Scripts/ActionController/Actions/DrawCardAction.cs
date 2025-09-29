using System;

public class DrawCardAction : IGameAction
{
    readonly Actor actor;
    readonly PhaseKind phase;

    public DrawCardAction(Actor a, PhaseKind k) { actor = a; phase = k; }

    public void Execute(CombatContext ctx)
    {
        var deck = ctx.GetDeckFor(actor);
        if (deck == null)
        {
            ctx.OnLog?.Invoke($"[Draw] No deck for {actor}. Draw skipped.");
            return;
        }

        var acc    = ctx.GetAcc(actor, phase);
        var relics = RelicManager.Instance; // null-safe (sadece kancalar için)

        // Threshold artık başlangıçta/relik senkronunda Ctx'e yazılıyor
        int threshold = ctx.GetThreshold(actor, phase);

        // Stand/Bust ise sadece UI güncelle
        if (acc.IsStanding || acc.IsBusted)
        {
            ctx.OnProgress?.Invoke(actor, phase, acc.Total, threshold);
            ctx.OnLog?.Invoke($"[Draw] Standing/Busted. No draw. TH={threshold}");
            return;
        }

        int totalBefore = acc.Total;
        int deckBefore  = deck.Count;

        // Tek kart çek
        if (deck.Count == 0)
        {
            deck.RebuildAndShuffle();
            ctx.OnLog?.Invoke($"[Deck] Empty → Rebuilt+Shuffled for {actor}.");
            relics?.OnShuffle();

            if (deck.Count == 0)
            {
                ctx.OnLog?.Invoke($"[Deck] Still empty after rebuild for {actor}. Stop drawing.");
                return;
            }
        }

        int before = acc.Total;

        acc.Hit(deck, threshold);

        var lastCard = acc.Cards.Count > 0 ? acc.Cards[^1] : default;
        relics?.OnCardDrawn(lastCard);

        ctx.OnCardDrawn?.Invoke(actor, phase, lastCard);
        ctx.OnProgress?.Invoke(actor, phase, acc.Total, threshold);

        ctx.OnLog?.Invoke(
            $"[{actor}:{phase}] {before} → {acc.Total} (Deck {deckBefore}->{deck.Count}) TH={threshold}"
        );

        // Özet log (tek kart olduğu için sade)
        ctx.OnLog?.Invoke(
            $"[Draw] {actor}:{phase} drew 1 card (Total {totalBefore}->{acc.Total}, Deck {deckBefore}->{deck.Count}) TH={threshold}"
        );
    }

    public string Describe() => $"Draw({actor},{phase})";
}
